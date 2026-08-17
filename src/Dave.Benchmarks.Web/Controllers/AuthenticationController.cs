using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text.Json.Serialization;
using Dave.Benchmarks.Web.Configuration;
using Dave.Benchmarks.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Dave.Benchmarks.Web.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthenticationController : ControllerBase
{
    private readonly IHttpClientFactory clients;
    private readonly GitLabOAuthSettings oauth;
    private readonly AuthenticationSettings authentication;
    private readonly AuthorisationSettings authorisation;

    public AuthenticationController(
        IHttpClientFactory clients,
        IOptions<GitLabOAuthSettings> oauth,
        IOptions<AuthenticationSettings> authentication,
        IOptions<AuthorisationSettings> authorisation)
    {
        this.clients = clients;
        this.oauth = oauth.Value;
        this.authentication = authentication.Value;
        this.authorisation = authorisation.Value;
    }

    [AllowAnonymous]
    [HttpPost("gitlab/exchange")]
    public async Task<ActionResult<object>> Exchange(
        ExchangeGitLabTokenRequest request,
        CancellationToken cancellationToken)
    {
        HttpClient gitlab = clients.CreateClient("GitLabOAuth");
        gitlab.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", request.AccessToken);

        GitLabUser? user = await GetAsync<GitLabUser>(gitlab, "api/v4/user", cancellationToken);
        if (user == null) return Unauthorized("GitLab rejected the OAuth token");

        int highestAccess = 0;
        string? trustedProject = null;
        foreach (string projectId in authorisation.AllowedGitlabProjectIds)
        {
            GitLabMember? member = await GetAsync<GitLabMember>(
                gitlab, $"api/v4/projects/{Uri.EscapeDataString(projectId)}/members/all/{user.Id}", cancellationToken);
            if (member != null && member.AccessLevel > highestAccess)
            {
                highestAccess = member.AccessLevel;
                trustedProject = projectId;
            }
        }

        if (highestAccess < 40 || trustedProject == null) return Forbid();

        DateTime expires = DateTime.UtcNow.AddMinutes(oauth.TokenLifetimeMinutes);
        Claim[] claims =
        [
            new(JwtRegisteredClaimNames.Sub, $"gitlab-user:{user.Id}"),
            new("gitlab_user_id", user.Id.ToString()),
            new("gitlab_username", user.Username),
            new("project_id", trustedProject),
            new("role", "observation_curator")
        ];
        JwtSecurityToken token = new(
            issuer: oauth.TokenIssuer,
            audience: authentication.ValidAudiences[0],
            claims: claims,
            notBefore: DateTime.UtcNow,
            expires: expires,
            signingCredentials: new SigningCredentials(
                new SymmetricSecurityKey(Convert.FromBase64String(oauth.SigningKey)),
                SecurityAlgorithms.HmacSha256));

        return Ok(new
        {
            accessToken = new JwtSecurityTokenHandler().WriteToken(token),
            expiresAt = expires
        });
    }

    private static async Task<T?> GetAsync<T>(HttpClient client, string path, CancellationToken cancellationToken)
    {
        using HttpResponseMessage response = await client.GetAsync(path, cancellationToken);
        if (response.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden or HttpStatusCode.NotFound)
            return default;
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<T>(cancellationToken: cancellationToken);
    }

    private sealed class GitLabUser
    {
        public int Id { get; set; }
        public string Username { get; set; } = string.Empty;
    }

    private sealed class GitLabMember
    {
        [JsonPropertyName("access_level")] public int AccessLevel { get; set; }
    }
}
