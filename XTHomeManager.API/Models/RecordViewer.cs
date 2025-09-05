namespace XTHomeManager.API.Models
{
    // Models/RecordViewer.cs
    public class RecordViewer
    {
        public int RecordId { get; set; }
        public string UserId { get; set; }
        public bool AllowViewerAccess { get; set; }
        public bool IsAccepted { get; set; }

        public Record Record { get; set; }
        public User User { get; set; }
    }
}
