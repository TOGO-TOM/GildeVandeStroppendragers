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

        /// <summary>
        /// Triggers the GitHub Actions workflow for AI analysis.
        /// Returns (success, errorMessage) so the caller can surface a meaningful error.
        /// </summary>
        public async Task<(bool Success, string? Error)> TriggerAnalysisAsync(FeatureRequest request)
        {
            var settings = await _context.AiSettings.FirstOrDefaultAsync();

            if (settings == null)
            {
                _logger.LogWarning("AI settings row missing. Cannot trigger analysis for request {Id}.", request.Id);
                return (false, "AI settings not configured. Go to Settings to add GitHub owner, repo, and token.");
            }

            if (string.IsNullOrEmpty(settings.GitHubOwner) || string.IsNullOrEmpty(settings.GitHubRepo))
            {
                _logger.LogWarning("GitHub owner/repo not set. Cannot trigger analysis for request {Id}.", request.Id);
                return (false, "GitHub owner or repository not configured in AI settings.");
            }

            if (string.IsNullOrEmpty(settings.GitHubToken))
            {
                _logger.LogWarning("GitHub token not set. Cannot trigger analysis for request {Id}.", request.Id);
                return (false, "GitHub token not configured in AI settings.");
            }

            try
            {
                var url = $"https://api.github.com/repos/{settings.GitHubOwner}/{settings.GitHubRepo}/actions/workflows/analyze-feature-request.yml/dispatches";

                var payload = new
                {
                    @ref = "main",
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

                _logger.LogInformation("Dispatching workflow to {Url} for request {Id}...", url, request.Id);
                var response = await client.SendAsync(httpRequest);

                if (response.IsSuccessStatusCode)
                {
                    request.Status = "Processing";
                    await _context.SaveChangesAsync();
                    _logger.LogInformation("GitHub Action triggered for feature request {Id}.", request.Id);
                    return (true, null);
                }

                var body = await response.Content.ReadAsStringAsync();
                _logger.LogError("GitHub API returned {StatusCode}: {Body}", (int)response.StatusCode, body);

                var errorMsg = (int)response.StatusCode switch
                {
                    401 => "GitHub token is invalid or expired. Update it in AI settings.",
                    403 => "GitHub token lacks 'workflow' permission. Create a token with the 'repo' and 'workflow' scopes.",
                    404 => $"Workflow not found at {settings.GitHubOwner}/{settings.GitHubRepo}. Check owner/repo in AI settings.",
                    422 => "GitHub rejected the request. Ensure the 'main' branch and workflow file exist.",
                    _ => $"GitHub API error {(int)response.StatusCode}: {body}"
                };

                return (false, errorMsg);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to trigger GitHub Action for feature request {Id}.", request.Id);
                return (false, $"Network error: {ex.Message}");
            }
        }
    }
}
