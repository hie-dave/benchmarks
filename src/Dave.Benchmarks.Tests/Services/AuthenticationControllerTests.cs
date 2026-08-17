using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Text;
using System.Text.Json;
using Dave.Benchmarks.Web.Configuration;
using Dave.Benchmarks.Web.Controllers;
using Dave.Benchmarks.Web.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Moq;

namespace Dave.Benchmarks.Tests.Services;

public class AuthenticationControllerTests
{
    [Fact]
    public async Task Exchange_WhenUserIsMaintainer_IssuesCuratorToken()
    {
        AuthenticationController controller = CreateController(40);

        ActionResult<object> result = await controller.Exchange(
            new ExchangeGitLabTokenRequest { AccessToken = "opaque-gitlab-token" }, CancellationToken.None);

        OkObjectResult ok = Assert.IsType<OkObjectResult>(result.Result);
        string json = JsonSerializer.Serialize(ok.Value);
        string token = JsonDocument.Parse(json).RootElement.GetProperty("accessToken").GetString()!;
        JwtSecurityToken jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);
        Assert.Equal("https://benchmarks.example.test", jwt.Issuer);
        Assert.Equal("observation_curator", jwt.Claims.Single(c => c.Type == "role").Value);
        Assert.Equal("12345", jwt.Claims.Single(c => c.Type == "project_id").Value);
    }

    [Fact]
    public async Task Exchange_WhenUserIsDeveloper_ReturnsForbidden()
    {
        AuthenticationController controller = CreateController(30);

        ActionResult<object> result = await controller.Exchange(
            new ExchangeGitLabTokenRequest { AccessToken = "opaque-gitlab-token" }, CancellationToken.None);

        Assert.IsType<ForbidResult>(result.Result);
    }

    private static AuthenticationController CreateController(int accessLevel)
    {
        HttpClient client = new(new GitLabHandler(accessLevel))
        {
            BaseAddress = new Uri("https://gitlab.example.test/")
        };
        Mock<IHttpClientFactory> clients = new();
        clients.Setup(f => f.CreateClient("GitLabOAuth")).Returns(client);
        return new AuthenticationController(
            clients.Object,
            Options.Create(new GitLabOAuthSettings
            {
                TokenIssuer = "https://benchmarks.example.test",
                SigningKey = Convert.ToBase64String(Enumerable.Repeat((byte)42, 32).ToArray()),
                TokenLifetimeMinutes = 60
            }),
            Options.Create(new AuthenticationSettings
            {
                Authority = "https://gitlab.example.test",
                ValidAudiences = ["https://benchmarks.example.test"]
            }),
            Options.Create(new AuthorisationSettings { AllowedGitlabProjectIds = ["12345"] }));
    }

    private sealed class GitLabHandler(int accessLevel) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Assert.Equal("Bearer", request.Headers.Authorization?.Scheme);
            Assert.Equal("opaque-gitlab-token", request.Headers.Authorization?.Parameter);
            string json = request.RequestUri!.AbsolutePath switch
            {
                "/api/v4/user" => "{\"id\":7,\"username\":\"curator\"}",
                "/api/v4/projects/12345/members/all/7" => $"{{\"access_level\":{accessLevel}}}",
                _ => throw new InvalidOperationException($"Unexpected path {request.RequestUri.AbsolutePath}")
            };
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            });
        }
    }
}
