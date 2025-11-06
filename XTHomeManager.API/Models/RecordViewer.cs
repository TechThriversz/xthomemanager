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


    // Models.cs
    public class RecordDetailsInvitedDto
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Type { get; set; }
        public string CreatedBy { get; set; }
        public List<EntryDto> Entries { get; set; }
    }

    public class EntryDto
    {
        public DateTime Date { get; set; }
        public decimal QuantityLiters { get; set; } // Milk
        public string Status { get; set; } // Milk, Rent
        public decimal TotalCost { get; set; } // Milk
        public string Month { get; set; } // Rent, Bill
        public decimal Amount { get; set; } // Rent, Bill
        public string ReferenceNumber { get; set; } // Bill
        public string FilePath { get; set; } // Bill
    }

    public class SharedRecordDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public string OwnerName { get; set; } = string.Empty;
        public bool IsAccepted { get; set; }
    }
}
