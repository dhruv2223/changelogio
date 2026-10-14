using Changelog.Api.Auth;
using Changelog.Api.Data;
using Changelog.Core;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Changelog.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/update")]
public sealed class UpdateController(UpdateRepository updates) : ControllerBase
{
    private static readonly MessageResponse NotFoundMessage = new() { Message = "update not found" };

    /// <summary>All of the user's updates, or only one product's with ?productId=.</summary>
    [HttpGet]
    public async Task<DataResponse<IReadOnlyList<Update>>> List([FromQuery] Guid? productId, CancellationToken ct) =>
        new() { Data = await updates.ListAsync(User.GetUserId(), productId, ct) };

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<DataResponse<Update>>> Get(Guid id, CancellationToken ct) =>
        await updates.GetAsync(id, User.GetUserId(), ct) is { } update
            ? new DataResponse<Update> { Data = update }
            : NotFound(NotFoundMessage);

    [HttpPost]
    public async Task<ActionResult<DataResponse<Update>>> Create(CreateUpdateRequest request, CancellationToken ct) =>
        await updates.CreateAsync(User.GetUserId(), request, ct) is { } update
            ? new DataResponse<Update> { Data = update }
            : NotFound(new MessageResponse { Message = "product not found" });

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<DataResponse<Update>>> Edit(Guid id, EditUpdateRequest request, CancellationToken ct) =>
        await updates.EditAsync(id, User.GetUserId(), request, ct) is { } update
            ? new DataResponse<Update> { Data = update }
            : NotFound(NotFoundMessage);

    [HttpDelete("{id:guid}")]
    public async Task<ActionResult<DataResponse<Update>>> Delete(Guid id, CancellationToken ct) =>
        await updates.DeleteAsync(id, User.GetUserId(), ct) is { } update
            ? new DataResponse<Update> { Data = update }
            : NotFound(NotFoundMessage);
}
