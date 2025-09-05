using System;
using System.ComponentModel.DataAnnotations;
using XTHomeManager.API.Models;

public class MilkEntry
{
    public int Id { get; set; }
    public int RecordId { get; set; } // Foreign key, should not be null
    public DateTime Date { get; set; }
    public decimal QuantityLiters { get; set; }
    public string Status { get; set; }
    public decimal TotalCost { get; set; }
    public string AdminId { get; set; } // No [Required]
    public Record? Record { get; set; } // Nullable navigation property
}