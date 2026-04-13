using AdminMembers.Data;
using AdminMembers.Models;
using Microsoft.EntityFrameworkCore;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace AdminMembers.Services
{
    public class FeatureRequestService
    {
        private readonly ApplicationDbContext _context;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<FeatureRequestService> _logger;

        public FeatureRequestService(ApplicationDbContext context, IHttpClientFactory httpClientFactory, ILogger<FeatureRequestService> logger)
        {
            _context = context;
            _httpClientFactory = httpClientFactory;
            _logger = logger;
        }

        public async Task<bool> TriggerAnalysisAsync(FeatureRequest request)
        {
            var settings = await _context.AiSettings.FirstOrDefaultAsync();

            if (settings == null || string.IsNullOrEmpty(settings.GitHubToken)
                || string.IsNullOrEmpty(settings.GitHubOwner)
                || string.IsNullOrEmpty(settings.GitHubRepo))
            {
                _logger.LogWarning("GitHub AI settings not configured. Cannot trigger analysis for request {Id}.", request.Id);
                return false;
            }

            try
            {
                var url = $"https://api.github.com/repos/{settings.GitHubOwner}/{settings.GitHubRepo}/actions/workflows/analyze-feature-request.yml/dispatches";

                var payload = new
                {
                    @ref = "feature/ai-feature-requests",
                    inputs = new
                    {
                        request_id = request.Id.ToString(),
                        title = request.Title,
                        description = request.Description,
                        submitted_by = string.IsNullOrWhiteSpace(request.SubmittedByUsername) ? "Unknown" : request.SubmittedByUsername
                    }
                };

                var json = JsonSerializer.Serialize(payload);

                using var client = _httpClientFactory.CreateClient();
                using var httpRequest = new HttpRequestMessage(HttpMethod.Post, url);
                httpRequest.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));
                httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", settings.GitHubToken);
                httpRequest.Headers.Add("X-GitHub-Api-Version", "2022-11-28");
                httpRequest.Headers.UserAgent.ParseAdd("AdminMembers/1.0");
                httpRequest.Content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await client.SendAsync(httpRequest);

                if (response.IsSuccessStatusCode)
                {
                    request.Status = "Processing";
                    await _context.SaveChangesAsync();
                    _logger.LogInformation("GitHub Action triggered for feature request {Id}.", request.Id);
                    return true;
                }

                var body = await response.Content.ReadAsStringAsync();
                _logger.LogError("GitHub API returned {StatusCode}: {Body}", (int)response.StatusCode, body);
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to trigger GitHub Action for feature request {Id}.", request.Id);
                return false;
            }
        }
    }
}
