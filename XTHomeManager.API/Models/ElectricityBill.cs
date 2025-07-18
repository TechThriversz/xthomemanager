namespace XTHomeManager.API.Models
{
    public class ElectricityBill
    {
        public int Id { get; set; }
        public string Month { get; set; }
        public decimal Amount { get; set; }
        public string ReferenceNumber { get; set; }
        public string? FilePath { get; set; }
        public string AdminId { get; set; }
        public bool AllowViewerAccess { get; set; }
    }
}