using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using System.Net;
using System.Text.Json;
using WeatherStationImages.Models;
using WeatherStationImages.Authentication;
using Microsoft.Extensions.Logging;

namespace WeatherStationImages.Functions;

public class GetJobStatusFunction
{
    private static readonly Dictionary<string, JobStatus> _jobStatuses = new();
    private readonly ILogger<GetJobStatusFunction> _logger;

    public GetJobStatusFunction(ILoggerFactory loggerFactory)
    {
        _logger = loggerFactory.CreateLogger<GetJobStatusFunction>();
    }

    [Function("GetJobStatus")]
    public async Task<HttpResponseData> GetStatusAsync(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "status/{jobId}")] HttpRequestData req,
        string jobId)
    {
        // Validate API Key
        var (isValid, errorResponse) = await ApiKeyAuthHandler.ValidateApiKeyAsync(req);
        if (!isValid)
        {
            return errorResponse!;
        }

        _logger.LogInformation("Checking status for job: {JobId}", jobId);

        JobStatus? status;
        lock (_jobStatuses)
        {
            _jobStatuses.TryGetValue(jobId, out status);
        }

        if (status == null)
        {
            _logger.LogWarning("Job not found: {JobId}", jobId);
            var notFoundResponse = req.CreateResponse(HttpStatusCode.NotFound);
            await notFoundResponse.WriteStringAsync($"Job with ID {jobId} not found");
            return notFoundResponse;
        }

        var response = req.CreateResponse(HttpStatusCode.OK);
        var statusResponse = new
        {
            jobId = status.JobId,
            status = status.IsComplete ? "Completed" : "In Progress",
            progress = $"{status.CompletedImages}/{status.TotalImages}",
            percentComplete = ((double)status.CompletedImages / status.TotalImages * 100).ToString("F1") + "%",
            createdAt = status.CreatedAt,
            lastUpdated = status.LastUpdated,
            imageUrls = status.ImageUrls,
            isComplete = status.IsComplete
        };

        await response.WriteAsJsonAsync(statusResponse);
        return response;
    }

    [Function("UpdateJobStatus")]
    public void UpdateStatus([QueueTrigger("image-complete")] string messageContent)
    {
        try
        {
            var newStatus = JsonSerializer.Deserialize<JobStatus>(messageContent);
            if (newStatus == null)
            {
                _logger.LogError("Failed to deserialize status update message");
                return;
            }

            lock (_jobStatuses)
            {
                if (!_jobStatuses.TryGetValue(newStatus.JobId, out var existingStatus))
                {
                    // First status update for this job
                    newStatus.LastUpdated = DateTime.UtcNow;
                    _jobStatuses[newStatus.JobId] = newStatus;
                    _logger.LogInformation("Created new status entry for job {JobId}", newStatus.JobId);
                }
                else
                {
                    // Update existing status
                    existingStatus.ImageUrls.AddRange(newStatus.ImageUrls);
                    existingStatus.CompletedImages += newStatus.CompletedImages;
                    existingStatus.LastUpdated = DateTime.UtcNow;
                    existingStatus.IsComplete = existingStatus.CompletedImages >= existingStatus.TotalImages;

                    _logger.LogInformation(
                        "Updated status for job {JobId}: {Completed}/{Total} images",
                        existingStatus.JobId,
                        existingStatus.CompletedImages,
                        existingStatus.TotalImages);

                    if (existingStatus.IsComplete)
                    {
                        _logger.LogInformation("Job {JobId} completed successfully", existingStatus.JobId);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing status update: {MessageContent}", messageContent);
            throw;
        }
    }
}