using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using System.Net;
using WeatherStationImages.Models;

namespace WeatherStationImages.Functions;

public class GetJobStatusFunction
{
    private static readonly Dictionary<string, JobStatus> _jobStatuses = new();

    [Function("GetJobStatus")]
    public async Task<HttpResponseData> RunAsync(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "status/{jobId}")] HttpRequestData req,
        string jobId)
    {
        var response = req.CreateResponse();

        if (!_jobStatuses.ContainsKey(jobId))
        {
            response.StatusCode = HttpStatusCode.NotFound;
            await response.WriteStringAsync($"Job with ID {jobId} not found");
            return response;
        }

        var status = _jobStatuses[jobId];
        response.StatusCode = HttpStatusCode.OK;
        await response.WriteAsJsonAsync(status);
        return response;
    }

    [Function("UpdateJobStatus")]
    public void Run([QueueTrigger("image-complete")] JobStatus newStatus)
    {
        lock (_jobStatuses)
        {
            if (!_jobStatuses.ContainsKey(newStatus.JobId))
            {
                _jobStatuses[newStatus.JobId] = newStatus;
            }
            else
            {
                var existingStatus = _jobStatuses[newStatus.JobId];
                existingStatus.ImageUrls.AddRange(newStatus.ImageUrls);
                existingStatus.CompletedImages += newStatus.CompletedImages;
                existingStatus.IsComplete = existingStatus.CompletedImages >= existingStatus.TotalImages;
            }
        }
    }
}