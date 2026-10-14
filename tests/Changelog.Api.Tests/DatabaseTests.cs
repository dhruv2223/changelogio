using System.Net;
using System.Net.Http.Json;
using Changelog.Core;

namespace Changelog.Api.Tests;

/// <summary>End-to-end tests against a real SQL Server; skipped unless CHANGELOG_TEST_SQL is set.</summary>
public sealed class DatabaseTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    private static async Task<T> DataAsync<T>(HttpResponseMessage response)
    {
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<DataResponse<T>>();
        return body!.Data;
    }

    [SqlServerFact]
    public async Task Register_sign_in_and_duplicate_usernames()
    {
        var client = factory.CreateClient();
        var credentials = new AuthRequest { Username = $"user-{Guid.NewGuid():N}", Password = "correct-horse" };

        Assert.Equal(HttpStatusCode.OK, (await client.PostAsJsonAsync("/user", credentials)).StatusCode);

        var duplicate = await client.PostAsJsonAsync("/user", credentials);
        Assert.Equal(HttpStatusCode.BadRequest, duplicate.StatusCode);
        Assert.Equal("username is already taken", (await duplicate.Content.ReadFromJsonAsync<MessageResponse>())!.Message);

        var signIn = await client.PostAsJsonAsync("/signin", credentials);
        var token = (await signIn.Content.ReadFromJsonAsync<TokenResponse>())!.Token;
        Assert.Equal(HttpStatusCode.OK, (await factory.CreateClient(token).GetAsync("/api/product")).StatusCode);

        var wrongPassword = await client.PostAsJsonAsync("/signin", new AuthRequest { Username = credentials.Username, Password = "wrong-password" });
        Assert.Equal(HttpStatusCode.Unauthorized, wrongPassword.StatusCode);
    }

    [SqlServerFact]
    public async Task Product_update_and_update_point_lifecycle()
    {
        var client = await factory.CreateUserClientAsync();

        var product = await DataAsync<Product>(await client.PostAsJsonAsync("/api/product", new { name = "  My App  " }));
        Assert.Equal("My App", product.Name);
        product = await DataAsync<Product>(await client.PutAsJsonAsync($"/api/product/{product.Id}", new { name = "Renamed" }));
        Assert.Equal("Renamed", product.Name);
        Assert.Single(await DataAsync<List<Product>>(await client.GetAsync("/api/product")));

        var update = await DataAsync<Update>(await client.PostAsJsonAsync("/api/update",
            new { productId = product.Id, title = "v1", body = "First release", version = "1.0.0" }));
        Assert.Equal(UpdateStatus.InProgress, update.Status);
        Assert.Equal("1.0.0", update.Version);

        // Partial edit: omitted fields are kept, an empty version clears it.
        update = await DataAsync<Update>(await client.PutAsJsonAsync($"/api/update/{update.Id}",
            new { status = UpdateStatus.Shipped, version = "" }));
        Assert.Equal(UpdateStatus.Shipped, update.Status);
        Assert.Equal("v1", update.Title);
        Assert.Null(update.Version);

        var point = await DataAsync<UpdatePoint>(await client.PostAsJsonAsync("/api/updatepoints",
            new { updateId = update.Id, name = "Dark mode", description = "Added a dark theme" }));
        point = await DataAsync<UpdatePoint>(await client.PutAsJsonAsync($"/api/updatepoints/{point.Id}", new { description = "Now with OLED black" }));
        Assert.Equal("Dark mode", point.Name);
        Assert.Equal("Now with OLED black", point.Description);
        Assert.Single(await DataAsync<List<UpdatePoint>>(await client.GetAsync($"/api/updatepoints?updateId={update.Id}")));

        // Deleting the product cascades to its updates and their points.
        await DataAsync<Product>(await client.DeleteAsync($"/api/product/{product.Id}"));
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync($"/api/update/{update.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync($"/api/updatepoints/{point.Id}")).StatusCode);
    }

    [SqlServerFact]
    public async Task Users_cannot_touch_each_others_data()
    {
        var owner = await factory.CreateUserClientAsync();
        var intruder = await factory.CreateUserClientAsync();

        var product = await DataAsync<Product>(await owner.PostAsJsonAsync("/api/product", new { name = "Private" }));
        var update = await DataAsync<Update>(await owner.PostAsJsonAsync("/api/update",
            new { productId = product.Id, title = "Secret", body = "..." }));

        Assert.Empty(await DataAsync<List<Product>>(await intruder.GetAsync("/api/product")));
        Assert.Empty(await DataAsync<List<Update>>(await intruder.GetAsync("/api/update")));
        Assert.Equal(HttpStatusCode.NotFound, (await intruder.GetAsync($"/api/product/{product.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await intruder.PutAsJsonAsync($"/api/product/{product.Id}", new { name = "Mine" })).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await intruder.DeleteAsync($"/api/update/{update.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await intruder.PostAsJsonAsync("/api/update",
            new { productId = product.Id, title = "Injected", body = "..." })).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await intruder.PostAsJsonAsync("/api/updatepoints",
            new { updateId = update.Id, name = "Injected", description = "..." })).StatusCode);

        // The owner's data is untouched.
        Assert.Equal("Private", (await DataAsync<Product>(await owner.GetAsync($"/api/product/{product.Id}"))).Name);
        Assert.Single(await DataAsync<List<Update>>(await owner.GetAsync($"/api/update?productId={product.Id}")));
    }
}
