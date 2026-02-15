namespace CreditSystem.Api.Controllers;

using CreditSystem.Application.DTOs.Tasks;
using CreditSystem.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

[ApiController]
[Route("api/[controller]")]
public class TasksController : ControllerBase
{
    private readonly ITaskService _taskService;
    private readonly ITaskExecutionService _taskExecutionService;

    public TasksController(ITaskService taskService, ITaskExecutionService taskExecutionService)
    {
        _taskService = taskService;
        _taskExecutionService = taskExecutionService;
    }

    /// <summary>
    /// Create a new task for the authenticated user
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Newly created task information</returns>
    [HttpPost]
    [Authorize]
    [ProducesResponseType(typeof(CreateTaskResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> CreateTask(CancellationToken cancellationToken)
    {
        var userIdClaim = User.FindFirst(JwtRegisteredClaimNames.Sub) 
            ?? User.FindFirst(ClaimTypes.NameIdentifier);

        if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
        {
            return Unauthorized(new { message = "Invalid or missing user ID in token" });
        }

        var taskResponse = await _taskService.CreateTaskAsync(userId, cancellationToken);

        return CreatedAtAction(nameof(GetUserTasks), null, taskResponse);
    }

    /// <summary>
    /// Get all tasks for the authenticated user
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of user's tasks ordered by creation date descending</returns>
    [HttpGet]
    [Authorize]
    [ProducesResponseType(typeof(IReadOnlyList<TaskListItemResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetUserTasks(CancellationToken cancellationToken)
    {
        var userIdClaim = User.FindFirst(JwtRegisteredClaimNames.Sub) 
            ?? User.FindFirst(ClaimTypes.NameIdentifier);

        if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
        {
            return Unauthorized(new { message = "Invalid or missing user ID in token" });
        }

        var tasksList = await _taskService.GetUserTasksAsync(userId, cancellationToken);

        return Ok(tasksList);
    }

    /// <summary>
    /// Execute a task for the authenticated user
    /// </summary>
    /// <param name="id">Task ID to execute</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Task execution information with status and cost</returns>
    [HttpPost("{id}/execute")]
    [Authorize]
    [ProducesResponseType(typeof(ExecuteTaskResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ExecuteTask(Guid id, CancellationToken cancellationToken)
    {
        var userIdClaim = User.FindFirst(JwtRegisteredClaimNames.Sub) 
            ?? User.FindFirst(ClaimTypes.NameIdentifier);

        if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
        {
            return Unauthorized(new { message = "Invalid or missing user ID in token" });
        }

        try
        {
            var executeResponse = await _taskExecutionService.ExecuteTaskAsync(id, userId, cancellationToken);
            return Ok(executeResponse);
        }
        catch (InvalidOperationException ex)
        {
            if (ex.Message.Contains("not found"))
                return NotFound(new { message = ex.Message });

            if (ex.Message.Contains("not authorized"))
                return Forbid();

            return BadRequest(new { message = ex.Message });
        }
    }
}
