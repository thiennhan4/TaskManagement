using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using TaskHub.Application.DTOs;

namespace TaskHub.API.Controllers;

// Transitional array aliases retain their shape, but never an unbounded query.
// Supported clients use the versioned PagedResult body instead.
internal static class LegacyPaging
{
    public static IActionResult LegacyPage<T>(this ControllerBase controller, PagedResult<T> page)
    {
        controller.Response.Headers["Deprecation"] = "true";
        controller.Response.Headers["X-Pagination"] = JsonSerializer.Serialize(new { page.Page,page.PageSize,page.TotalItems,page.TotalPages });
        return controller.Ok(ApiResponse<List<T>>.Ok(page.Items));
    }
}
