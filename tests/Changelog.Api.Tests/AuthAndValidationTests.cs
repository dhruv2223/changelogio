using System.Net;
using System.Net.Http.Json;
using Changelog.Api.Auth;
using Changelog.Api.Data;
using Changelog.Core;
using Microsoft.Extensions.DependencyInjection;

namespace Changelog.Api.Tests;

/// <summary>Requests that are answered before any query runs, so these pass without a database.</summary>
public sealed class AuthAndValidationTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    private HttpClient AuthenticatedClient() =>
        factory.CreateClient(factory.Services.GetRequiredService<TokenService>()
            .CreateToken(new UserRecord { Id = Guid.NewGuid(), Username = "someone" }));

    [Fact]
    public async Task Root_says_hello()
    {
        var body = await factory.CreateClient().GetFromJsonAsync<MessageResponse>("/");
        Assert.Equal("hello", body!.Message);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("not-a-jwt")]
    [InlineData("eyJhbGciOiJIUzI1NiJ9.eyJzdWIiOiIxMjMifQ.c2lnbmF0dXJl")]
    public async Task Api_rejects_missing_or_invalid_tokens(string? token)
    {
        var client = token is null ? factory.CreateClient() : factory.CreateClient(token);

        var response = await client.GetAsync("/api/product");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<MessageResponse>();
        Assert.Equal("user not authorized", body!.Message);
    }

    [Theory]
    [InlineData("ab", "long-enough")]
    [InlineData("valid-name", "short")]
    [InlineData("", "")]
    public async Task Register_rejects_invalid_credentials(string username, string password)
    {
        var response = await factory.CreateClient().PostAsJsonAsync("/user", new { username, password });
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Create_update_rejects_unknown_status()
    {
        var response = await AuthenticatedClient().PostAsJsonAsync("/api/update",
            new { productId = Guid.NewGuid(), title = "t", body = "b", status = "DONE" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("Status must be one of", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Create_update_requires_product_id()
    {
        var response = await AuthenticatedClient().PostAsJsonAsync("/api/update", new { title = "t", body = "b" });
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Create_update_point_requires_update_id()
    {
        var response = await AuthenticatedClient().PostAsJsonAsync("/api/updatepoints", new { name = "n", description = "d" });
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Non_guid_ids_do_not_match_a_route()
    {
        var response = await AuthenticatedClient().GetAsync("/api/product/not-a-guid");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
