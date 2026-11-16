namespace car_dealership.Models;

// Durable checkpoints make a stopped import resumable and failed calls visible.
public class CatalogImportRequest
{
    public string Id { get; set; } = ""; // API request path; also the unique key.
    public int Attempts { get; set; } // Number of import runs that processed this request.
    public DateTime LastAttemptAt { get; set; }
    public DateTime? CompletedAt { get; set; } // Null means this request needs retrying.
    public string? LastError { get; set; }
    public string? ResponseJson { get; set; } // Original successful API payload.
}
