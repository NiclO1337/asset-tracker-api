using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;

namespace AssetTracker.Api.Tests;

public sealed class SmokeTests(WebApplicationFactory<Program> factory)
    : IClassFixture<WebApplicationFactory<Program>>
{
    [Fact]
    public async Task UnknownRoute_Returns404()
    {
        using var client = factory.CreateClient();

        using var response = await client.GetAsync("/does-not-exist");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
