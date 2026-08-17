using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Dave.Benchmarks.CLI.Configuration;
using Microsoft.Extensions.Logging;

namespace Dave.Benchmarks.CLI.Services;

public class GitLabCuratorAuthenticator
{
    private readonly IHttpClientFactory clients;
    private readonly ApiSettings settings;
    private readonly ILogger<GitLabCuratorAuthenticator> logger;

    public GitLabCuratorAuthenticator(
        IHttpClientFactory clients,
        ApiSettings settings,
        ILogger<GitLabCuratorAuthenticator> logger)
    {
        this.clients = clients;
        this.settings = settings;
        this.logger = logger;
    }

    public async Task<string> AuthenticateAsync(CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(settings.GitLabUrl) || string.IsNullOrWhiteSpace(settings.GitLabOAuthClientId))
            throw new InvalidOperationException("GitLabUrl and GitLabOAuthClientId are required for curator login");

        using HttpClient gitlab = clients.CreateClient();
        gitlab.BaseAddress = new Uri(settings.GitLabUrl.TrimEnd('/') + "/");
        using HttpResponseMessage deviceResponse = await gitlab.PostAsync(
            "oauth/authorize_device",
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["client_id"] = settings.GitLabOAuthClientId,
                ["scope"] = "read_api"
            }),
            cancellationToken);
        deviceResponse.EnsureSuccessStatusCode();
        DeviceAuthorization device = await deviceResponse.Content.ReadFromJsonAsync<DeviceAuthorization>(cancellationToken)
            ?? throw new InvalidOperationException("GitLab returned an empty device authorization response");

        logger.LogInformation("Open {VerificationUri} and enter code {UserCode}", device.VerificationUri, device.UserCode);
        string gitlabToken = await PollAsync(gitlab, device, cancellationToken);

        using HttpClient benchmark = clients.CreateClient();
        benchmark.BaseAddress = new Uri(settings.WebApiUrl.TrimEnd('/') + "/");
        using HttpResponseMessage exchange = await benchmark.PostAsJsonAsync(
            "api/auth/gitlab/exchange",
            new { accessToken = gitlabToken },
            cancellationToken);
        exchange.EnsureSuccessStatusCode();
        ExchangeResponse result = await exchange.Content.ReadFromJsonAsync<ExchangeResponse>(cancellationToken)
            ?? throw new InvalidOperationException("Benchmark server returned an empty token response");
        return result.AccessToken;
    }

    private async Task<string> PollAsync(
        HttpClient gitlab,
        DeviceAuthorization device,
        CancellationToken cancellationToken)
    {
        DateTime deadline = DateTime.UtcNow.AddSeconds(device.ExpiresIn);
        int interval = Math.Max(1, device.Interval);
        while (DateTime.UtcNow < deadline)
        {
            await Task.Delay(TimeSpan.FromSeconds(interval), cancellationToken);
            using HttpResponseMessage response = await gitlab.PostAsync(
                "oauth/token",
                new FormUrlEncodedContent(new Dictionary<string, string>
                {
                    ["grant_type"] = "urn:ietf:params:oauth:grant-type:device_code",
                    ["device_code"] = device.DeviceCode,
                    ["client_id"] = settings.GitLabOAuthClientId
                }),
                cancellationToken);
            DeviceTokenResponse? token = await response.Content.ReadFromJsonAsync<DeviceTokenResponse>(cancellationToken);
            if (response.IsSuccessStatusCode && !string.IsNullOrWhiteSpace(token?.AccessToken)) return token.AccessToken;
            if (token?.Error == "slow_down") { interval += 5; continue; }
            if (token?.Error == "authorization_pending") continue;
            throw new InvalidOperationException($"GitLab device authorization failed: {token?.Error ?? response.StatusCode.ToString()}");
        }
        throw new TimeoutException("GitLab device authorization expired");
    }

    private sealed class DeviceAuthorization
    {
        [JsonPropertyName("device_code")] public string DeviceCode { get; set; } = string.Empty;
        [JsonPropertyName("user_code")] public string UserCode { get; set; } = string.Empty;
        [JsonPropertyName("verification_uri")] public string VerificationUri { get; set; } = string.Empty;
        [JsonPropertyName("expires_in")] public int ExpiresIn { get; set; }
        [JsonPropertyName("interval")] public int Interval { get; set; }
    }

    private sealed class DeviceTokenResponse
    {
        [JsonPropertyName("access_token")] public string? AccessToken { get; set; }
        [JsonPropertyName("error")] public string? Error { get; set; }
    }

    private sealed class ExchangeResponse
    {
        public string AccessToken { get; set; } = string.Empty;
    }
}
