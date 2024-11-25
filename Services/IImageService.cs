using WeatherStationImages.Models;

namespace WeatherStationImages.Services;

public interface IImageService
{
    Task<string> GenerateAndUploadImageAsync(WeatherData weatherData, string jobId);
}