using Changelog.Api.Auth;
using Changelog.Api.Data;
using Changelog.Core;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Changelog.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/product")]
public sealed class ProductController(ProductRepository products) : ControllerBase
{
    private static readonly MessageResponse NotFoundMessage = new() { Message = "product not found" };

    [HttpGet]
    public async Task<DataResponse<IReadOnlyList<Product>>> List(CancellationToken ct) =>
        new() { Data = await products.ListAsync(User.GetUserId(), ct) };

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<DataResponse<Product>>> Get(Guid id, CancellationToken ct) =>
        await products.GetAsync(id, User.GetUserId(), ct) is { } product
            ? new DataResponse<Product> { Data = product }
            : NotFound(NotFoundMessage);

    [HttpPost]
    public async Task<DataResponse<Product>> Create(ProductRequest request, CancellationToken ct) =>
        new() { Data = await products.CreateAsync(User.GetUserId(), request.Name.Trim(), ct) };

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<DataResponse<Product>>> Rename(Guid id, ProductRequest request, CancellationToken ct) =>
        await products.RenameAsync(id, User.GetUserId(), request.Name.Trim(), ct) is { } product
            ? new DataResponse<Product> { Data = product }
            : NotFound(NotFoundMessage);

    [HttpDelete("{id:guid}")]
    public async Task<ActionResult<DataResponse<Product>>> Delete(Guid id, CancellationToken ct) =>
        await products.DeleteAsync(id, User.GetUserId(), ct) is { } product
            ? new DataResponse<Product> { Data = product }
            : NotFound(NotFoundMessage);
}
