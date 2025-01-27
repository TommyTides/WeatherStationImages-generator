using System.Text.Json.Serialization;

namespace WeatherStationImages.Models;

public class JobStatus
{
    [JsonPropertyName("jobId")]
    public string JobId { get; set; } = Guid.NewGuid().ToString();

    [JsonPropertyName("imageUrls")]
    public List<string> ImageUrls { get; set; } = new();

    [JsonPropertyName("totalImages")]
    public int TotalImages { get; set; }
}