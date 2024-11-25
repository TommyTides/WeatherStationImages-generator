using System.Text.Json.Serialization;

namespace WeatherStationImages.Models;

public class ImageGenerationRequest
{
    [JsonPropertyName("jobId")]
    public string JobId { get; set; } = string.Empty;

    [JsonPropertyName("weatherData")]
    public WeatherData WeatherData { get; set; } = null!;

    [JsonPropertyName("totalImages")]
    public int TotalImages { get; set; }
}