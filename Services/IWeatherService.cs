using WeatherStationImages.Models;

namespace WeatherStationImages.Services;

public interface IWeatherService
{
    List<WeatherData> ParseWeatherData(string jsonData);
}