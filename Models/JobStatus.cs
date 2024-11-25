namespace WeatherStationImages.Models;

public class JobStatus
{
    public string JobId { get; set; } = Guid.NewGuid().ToString();
    public List<string> ImageUrls { get; set; } = new();
    public bool IsComplete { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public int TotalImages { get; set; }
    public int CompletedImages { get; set; }
}