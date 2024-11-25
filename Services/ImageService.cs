using Azure.Storage.Blobs;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.Fonts;
using WeatherStationImages.Models;
using SixLabors.ImageSharp.Processing;

namespace WeatherStationImages.Services;

public class ImageService : IImageService
{
    private readonly BlobContainerClient _containerClient;
    private readonly HttpClient _httpClient;

    public ImageService(string connectionString)
    {
        _containerClient = new BlobContainerClient(connectionString, "weather-images");
        _containerClient.CreateIfNotExists();
        _httpClient = new HttpClient();
    }

    public async Task<string> GenerateAndUploadImageAsync(WeatherData weatherData, string jobId)
    {
        // Download base image from Picsum
        var imageBytes = await _httpClient.GetByteArrayAsync("https://picsum.photos/600/800");

        using var image = Image.Load(imageBytes);
        using var outputStream = new MemoryStream();

        // Add weather information to the image
        var font = SystemFonts.CreateFont("Arial", 30);

        image.Mutate(x => x
            .DrawText($"Station: {weatherData.StationName}", font, Color.White, new PointF(20, 20))
            .DrawText($"Temperature: {weatherData.Temperature:F1}°C", font, Color.White, new PointF(20, 60))
            .DrawText($"Humidity: {weatherData.Humidity:F1}%", font, Color.White, new PointF(20, 100))
            .DrawText($"Weather: {weatherData.WeatherDescription}", font, Color.White, new PointF(20, 140))
            .DrawText($"Region: {weatherData.Region}", font, Color.White, new PointF(20, 180)));

        image.SaveAsJpeg(outputStream);
        outputStream.Position = 0;

        // Upload to blob storage
        var blobName = $"{jobId}/{weatherData.StationName.ToLower().Replace(" ", "-")}.jpg";
        var blobClient = _containerClient.GetBlobClient(blobName);
        await blobClient.UploadAsync(outputStream, overwrite: true);

        return blobClient.Uri.ToString();
    }
}