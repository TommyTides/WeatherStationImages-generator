using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using System.Net;
using WeatherStationImages.Authentication;
using WeatherStationImages.Models;

namespace WeatherStationImages.Functions;

public class GetImagesFunction
{
    private readonly ILogger<GetImagesFunction> _logger;
    private static readonly Dictionary<string, List<string>> _jobImages = new();

    public GetImagesFunction(ILoggerFactory loggerFactory)
    {
        _logger = loggerFactory.CreateLogger<GetImagesFunction>();
    }

    [Function("GetImages")]
    public async Task<HttpResponseData> GetImagesAsync(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "images/{jobId}")] HttpRequestData req,
        string jobId)
    {
        _logger.LogInformation("Fetching images for job: {JobId}", jobId);

        // Validate API Key
        var (isValid, errorResponse) = await ApiKeyAuthHandler.ValidateApiKeyAsync(req);
        if (!isValid)
        {
            return errorResponse!;
        }

        List<string>? imageUrls;
        lock (_jobImages)
        {
            _jobImages.TryGetValue(jobId, out imageUrls);
        }

        var response = req.CreateResponse(HttpStatusCode.OK);
        await response.WriteAsJsonAsync(new
        {
            jobId,
            imagesProcessed = imageUrls?.Count ?? 0,
            images = imageUrls ?? new List<string>(),
            message = imageUrls == null
                ? "No images processed yet"
                : $"Found {imageUrls.Count} processed images"
        });

        return response;
    }

    [Function("CollectImageUrl")]
    public void CollectImageUrl([QueueTrigger("image-complete")] JobStatus newStatus)
    {
        if (newStatus?.ImageUrls == null || !newStatus.ImageUrls.Any())
        {
            _logger.LogWarning("No image URLs in status update for job {JobId}", newStatus?.JobId);
            return;
        }

        lock (_jobImages)
        {
            if (!_jobImages.ContainsKey(newStatus.JobId))
            {
                _jobImages[newStatus.JobId] = new List<string>();
            }
            _jobImages[newStatus.JobId].AddRange(newStatus.ImageUrls);
            _logger.LogInformation("Added {Count} images for job {JobId}. Total images so far: {Total}",
                newStatus.ImageUrls.Count,
                newStatus.JobId,
                _jobImages[newStatus.JobId].Count);
        }
    }
}