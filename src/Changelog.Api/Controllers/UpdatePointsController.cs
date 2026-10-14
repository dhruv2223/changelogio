using Changelog.Api.Auth;
using Changelog.Api.Data;
using Changelog.Core;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Changelog.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/updatepoints")]
public sealed class UpdatePointsController(UpdatePointRepository points) : ControllerBase
{
    private static readonly MessageResponse NotFoundMessage = new() { Message = "update point not found" };

    /// <summary>All of the user's update points, or only one update's with ?updateId=.</summary>
    [HttpGet]
    public async Task<DataResponse<IReadOnlyList<UpdatePoint>>> List([FromQuery] Guid? updateId, CancellationToken ct) =>
        new() { Data = await points.ListAsync(User.GetUserId(), updateId, ct) };

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<DataResponse<UpdatePoint>>> Get(Guid id, CancellationToken ct) =>
        await points.GetAsync(id, User.GetUserId(), ct) is { } point
            ? new DataResponse<UpdatePoint> { Data = point }
            : NotFound(NotFoundMessage);

    [HttpPost]
    public async Task<ActionResult<DataResponse<UpdatePoint>>> Create(CreateUpdatePointRequest request, CancellationToken ct) =>
        await points.CreateAsync(User.GetUserId(), request, ct) is { } point
            ? new DataResponse<UpdatePoint> { Data = point }
            : NotFound(new MessageResponse { Message = "update not found" });

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<DataResponse<UpdatePoint>>> Edit(Guid id, EditUpdatePointRequest request, CancellationToken ct) =>
        await points.EditAsync(id, User.GetUserId(), request, ct) is { } point
            ? new DataResponse<UpdatePoint> { Data = point }
            : NotFound(NotFoundMessage);

    [HttpDelete("{id:guid}")]
    public async Task<ActionResult<DataResponse<UpdatePoint>>> Delete(Guid id, CancellationToken ct) =>
        await points.DeleteAsync(id, User.GetUserId(), ct) is { } point
            ? new DataResponse<UpdatePoint> { Data = point }
            : NotFound(NotFoundMessage);
}
