using System.Text.Json;
using WeatherStationImages.Models;

namespace WeatherStationImages.Services;

public class WeatherService : IWeatherService
{
    public List<WeatherData> ParseWeatherData(string jsonData)
    {
        try
        {
            using var document = JsonDocument.Parse(jsonData);
            var stations = document.RootElement
                .GetProperty("actual")
                .GetProperty("stationmeasurements")
                .EnumerateArray();

            var weatherData = new List<WeatherData>();

            foreach (var station in stations)
            {
                // Skip stations without complete data
                if (!station.TryGetProperty("temperature", out _) ||
                    !station.TryGetProperty("humidity", out _))
                    continue;

                weatherData.Add(new WeatherData
                {
                    StationName = station.GetProperty("stationname").GetString() ?? "",
                    Temperature = station.GetProperty("temperature").GetDouble(),
                    Humidity = station.GetProperty("humidity").GetDouble(),
                    WeatherDescription = station.GetProperty("weatherdescription").GetString() ?? "",
                    Region = station.GetProperty("regio").GetString() ?? ""
                });
            }

            return weatherData;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Failed to parse weather data", ex);
        }
    }
}