using System.Text.Json.Serialization;

namespace WeatherStationImages.Models;

public class WeatherFetchRequest
{
    [JsonPropertyName("jobId")]
    public string JobId { get; set; } = string.Empty;
}

public class WeatherProcessingResult
{
    public string JobId { get; set; } = string.Empty;
    public List<WeatherData> WeatherDataList { get; set; } = new();
    public int TotalStations { get; set; }
}

public class ImageProcessRequest
{
    [JsonPropertyName("jobId")]
    public string JobId { get; set; } = string.Empty;

    [JsonPropertyName("weatherData")]
    public WeatherData WeatherData { get; set; } = null!;

    [JsonPropertyName("totalImages")]
    public int TotalImages { get; set; }

    [JsonPropertyName("imageIndex")]
    public int ImageIndex { get; set; }
}