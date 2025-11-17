namespace EBookDashboard.Models
{
    public class CartUpdateResult
    {
        public PaymentSummaryViewModel Summary { get; set; } = new PaymentSummaryViewModel();
        public int ItemCount { get; set; }
        public bool IsNewItem { get; set; }
        public string Message { get; set; } = string.Empty;
        public bool Success { get; set; }
    }
}


