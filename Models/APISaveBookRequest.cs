using System.ComponentModel.DataAnnotations.Schema;

namespace EBookDashboard.Models
{
    [Table("apisavebookrequest")]
    public class APISaveBookRequest
    {
        public string? UserId { get; set; }
        public string? BookId { get; set; }
        public string ApiRaw { get; set; } = string.Empty;

    }
}
