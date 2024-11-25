using System.Text.Json.Serialization;

namespace WeatherStationImages.Models;

public class WeatherData
{
    [JsonPropertyName("stationname")]
    public string StationName { get; set; } = string.Empty;

    [JsonPropertyName("temperature")]
    public double Temperature { get; set; }

    [JsonPropertyName("humidity")]
    public double Humidity { get; set; }

    [JsonPropertyName("weatherdescription")]
    public string WeatherDescription { get; set; } = string.Empty;

    [JsonPropertyName("regio")]
    public string Region { get; set; } = string.Empty;
}