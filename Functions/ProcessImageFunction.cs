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
    private readonly QueueClient _imageCompleteQueue;
    private readonly ILogger<ProcessImageFunction> _logger;

    public ProcessImageFunction(
        IImageService imageService,
        ILoggerFactory loggerFactory)
    {
        _imageService = imageService;
        _logger = loggerFactory.CreateLogger<ProcessImageFunction>();

        var connectionString = Environment.GetEnvironmentVariable("AzureWebJobsStorage");
        _imageCompleteQueue = new QueueClient(connectionString, "image-complete");
        _imageCompleteQueue.CreateIfNotExists();
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

            _logger.LogInformation("Processing station: {Station} for job: {JobId}",
                request.WeatherData.StationName,
                request.JobId);

            var imageUrl = await _imageService.GenerateAndUploadImageAsync(request.WeatherData, request.JobId);

            var jobStatus = new JobStatus
            {
                JobId = request.JobId,
                ImageUrls = new List<string> { imageUrl },
                TotalImages = request.TotalImages
            };

            var statusMessage = JsonSerializer.Serialize(jobStatus);
            await _imageCompleteQueue.SendMessageAsync(Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(statusMessage)));

            _logger.LogInformation("Successfully processed image for station: {StationName}",
                request.WeatherData.StationName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing image request: {MessageContent}", messageContent);
            throw;
        }
    }
}