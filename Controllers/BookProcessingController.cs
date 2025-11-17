using EBookDashboard.Models;
using EBookDashboard.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Net.Http.Headers;

namespace EBookDashboard.Controllers
{
    [Route("BookProcessing")]
    public class BookProcessingController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly BookProcessingService _processingService;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;

        public BookProcessingController(ApplicationDbContext context, IHttpClientFactory httpClientFactory, IConfiguration configuration)
        {
            _context = context;
            _processingService = new BookProcessingService(_context);
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
        }

        [HttpGet]
        [Route("")]
        [Route("Index")]
        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        [Route("ProcessResponse")]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> ProcessResponse(int responseId)
        {
            try
            {
                var result = await _processingService.ProcessRawResponse(responseId);
                if (result)
                    return Ok(new { success = true, message = $"Response {responseId} processed successfully" });
                return BadRequest(new { success = false, message = $"Failed to process response {responseId}" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        [Route("ProcessAllResponses")]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> ProcessAllResponses()
        {
            try
            {
                var processedCount = await _processingService.ProcessAllUnprocessedResponses();
                return Ok(new { success = true, message = $"Processed {processedCount} responses" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        [HttpGet]
        [Route("GetUnprocessedResponses")]
        public async Task<IActionResult> GetUnprocessedResponses()
        {
            var unprocessed = await _context.APIRawResponse
                .Where(r => string.IsNullOrEmpty(r.ParsedBookId) && !string.IsNullOrEmpty(r.ResponseData))
                .Select(r => new
                {
                    r.ResponseId,
                    r.Endpoint,
                    r.CreatedAt,
                    UserId = r.UserId ?? 0
                })
                .ToListAsync();

            return Ok(unprocessed);
        }

        [HttpGet]
        [Route("DebugResponse")]
        public async Task<IActionResult> DebugResponse(int responseId)
        {
            try
            {
                var response = await _context.APIRawResponse
                    .FirstOrDefaultAsync(r => r.ResponseId == responseId);

                if (response == null)
                {
                    return NotFound($"Response {responseId} not found");
                }

                var debugInfo = new
                {
                    ResponseId = response.ResponseId,
                    Endpoint = response.Endpoint,
                    UserId = response.UserId,
                    BookId = response.BookId,
                    HasRequestData = !string.IsNullOrEmpty(response.RequestData),
                    HasResponseData = !string.IsNullOrEmpty(response.ResponseData),
                    ResponseDataLength = response.ResponseData?.Length ?? 0,
                    ResponseDataPreview = response.ResponseData?.Substring(0, Math.Min(200, response.ResponseData?.Length ?? 0)),
                    ParsedBookId = response.ParsedBookId,
                    CreatedAt = response.CreatedAt
                };

                return Ok(debugInfo);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message, stackTrace = ex.StackTrace });
            }
        }

        // Step 5: Chapter Finalization
        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Route("FinalizeChapter")]
        public async Task<IActionResult> FinalizeChapter([FromBody] FinalizeChapterRequest request)
        {
            try
            {
                var chapter = await _context.Chapters.FirstOrDefaultAsync(c => c.ChapterId == request.ChapterId && c.BookId == request.BookId);
                if (chapter == null)
                {
                    return Json(new { success = false, message = "Chapter not found" });
                }
                chapter.Status = "Finalized";
                chapter.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                // Optional: replicate to external DBs via APIs if configured
                _ = Task.Run(async () =>
                {
                    try
                    {
                        var hasanUrl = _configuration["ExternalSync:HasanFinalizeUrl"];
                        var atharUrl = _configuration["ExternalSync:AtharFinalizeUrl"];
                        var payload = new { request.UserId, request.BookId, request.ChapterId, status = "Finalized" };
                        var client = _httpClientFactory.CreateClient();
                        var content = new StringContent(System.Text.Json.JsonSerializer.Serialize(payload), System.Text.Encoding.UTF8, "application/json");
                        if (!string.IsNullOrEmpty(hasanUrl)) await client.PostAsync(hasanUrl, content);
                        if (!string.IsNullOrEmpty(atharUrl)) await client.PostAsync(atharUrl, content);
                    }
                    catch { }
                });

                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // Step 6: Chapter Creation via Audio (Hasan's Audio API → topic → AI chapter)
        [Authorize]
        [HttpPost]
        [RequestSizeLimit(52428800)]
        [Route("CreateChapterFromAudio")]
        public async Task<IActionResult> CreateChapterFromAudio(int bookId, string userId, IFormFile audio)
        {
            if (audio == null || audio.Length == 0)
            {
                return BadRequest(new { success = false, message = "Audio file is required" });
            }

            try
            {
                // 1) Transcribe audio → topic/headline
                var audioApiUrl = _configuration["AudioApi:TranscribeUrl"]; // e.g., https://.../transcribe
                string topic = "Chapter Topic";
                if (!string.IsNullOrEmpty(audioApiUrl))
                {
                    using var stream = audio.OpenReadStream();
                    var client = _httpClientFactory.CreateClient();
                    using var content = new MultipartFormDataContent();
                    var fileContent = new StreamContent(stream);
                    fileContent.Headers.ContentType = new MediaTypeHeaderValue(audio.ContentType);
                    content.Add(fileContent, "file", audio.FileName);
                    content.Add(new StringContent(userId ?? ""), "userId");
                    content.Add(new StringContent(bookId.ToString()), "bookId");
                    var resp = await client.PostAsync(audioApiUrl, content);
                    var txt = await resp.Content.ReadAsStringAsync();
                    if (resp.IsSuccessStatusCode && !string.IsNullOrWhiteSpace(txt))
                    {
                        topic = txt.Trim();
                    }
                }

                // 2) Create chapter via AI using topic (re-using existing AI book endpoint)
                var aiRequest = new AIBookRequest
                {
                    UserId = userId,
                    BookId = bookId.ToString(),
                    Chapter = 0,
                    UserInput = topic
                };

                // Reuse existing internal endpoint for AI generation
                var json = System.Text.Json.JsonSerializer.Serialize(aiRequest);
                var aiClient = _httpClientFactory.CreateClient();
                var baseUri = $"{Request.Scheme}://{Request.Host}";
                var aiResp = await aiClient.PostAsync(baseUri + "/Books/AIGenerateBook", new StringContent(json, System.Text.Encoding.UTF8, "application/json"));
                var aiData = await aiResp.Content.ReadAsStringAsync();

                return Content(aiData, "application/json");
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        public class FinalizeChapterRequest
        {
            public int BookId { get; set; }
            public int ChapterId { get; set; }
            public string UserId { get; set; } = string.Empty;
        }
    }
}
