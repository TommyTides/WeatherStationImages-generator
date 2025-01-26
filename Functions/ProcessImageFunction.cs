using Azure.Storage.Queues;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using WeatherStationImages.Models;
using WeatherStationImages.Services;

namespace WeatherStationImages.Functions;

public class ProcessImageFunction
{
    private readonly IImageService _imageService;
    private readonly QueueClient _completeQueueClient;
    private readonly ILogger<ProcessImageFunction> _logger;

    public ProcessImageFunction(
        IImageService imageService,
        ILoggerFactory loggerFactory)
    {
        _imageService = imageService;
        _logger = loggerFactory.CreateLogger<ProcessImageFunction>();

        var connectionString = Environment.GetEnvironmentVariable("AzureWebJobsStorage");
        _completeQueueClient = new QueueClient(connectionString, "image-complete");
        _completeQueueClient.CreateIfNotExists();
    }

    [Function("ProcessImage")]
    public async Task RunAsync([QueueTrigger("image-process")] string messageContent)
    {
        _logger.LogInformation("Received image processing request: {MessageContent}", messageContent);

        try
        {
            var request = JsonSerializer.Deserialize<ImageProcessRequest>(
                messageContent,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (request == null)
            {
                _logger.LogError("Failed to deserialize message. Content: {MessageContent}", messageContent);
                throw new InvalidOperationException("Message could not be deserialized");
            }

            _logger.LogInformation("Processing image {Index}/{Total} for station: {Station} (Job: {JobId})",
                request.ImageIndex,
                request.TotalImages,
                request.WeatherData.StationName,
                request.JobId);

            var imageUrl = await _imageService.GenerateAndUploadImageAsync(request.WeatherData, request.JobId);

            var status = new JobStatus
            {
                JobId = request.JobId,
                ImageUrls = new List<string> { imageUrl },
                IsComplete = false,
                TotalImages = request.TotalImages,
                CompletedImages = 1,
                CreatedAt = DateTime.UtcNow
            };

            var statusMessage = JsonSerializer.Serialize(status);
            await _completeQueueClient.SendMessageAsync(Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(statusMessage)));

            _logger.LogInformation(
                "Successfully processed image {Index}/{Total} for station: {Station} (Job: {JobId})",
                request.ImageIndex,
                request.TotalImages,
                request.WeatherData.StationName,
                request.JobId);
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "JSON Deserialization error. Message content: {MessageContent}", messageContent);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing image request. Message content: {MessageContent}", messageContent);
            throw;
        }
    }
}