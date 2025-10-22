using EBookDashboard.Models;
using EBookDashboard.Services;
using Newtonsoft.Json;

namespace EBookDashboard.Interfaces
{
    public interface IAPIRawResponseService
    {
        Task<int> SaveRawResponseAsync(AIBookRequest request, string responseData, string endpoint, string statusCode, string errorMessage = null);
        Task<Models.APIRawResponse> GetRawResponseAsync(int responseId);
    }
}
