namespace XTHomeManager.API.Models
{
    public class RentEntry
    {
        public int Id { get; set; }
        public string Month { get; set; }
        public decimal Amount { get; set; }
        public string PropertyName { get; set; }
        public string AdminId { get; set; }
        public bool AllowViewerAccess { get; set; }
    }
}