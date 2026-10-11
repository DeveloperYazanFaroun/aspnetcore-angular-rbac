using Microsoft.AspNetCore.Mvc;

namespace Rbac.Api.Common;

/// <summary>
/// Converts <see cref="Result"/> objects to HTTP responses with RFC 9457 problem details.
/// </summary>
public static class ResultExtensions
{
    public static IActionResult ToActionResult(this Result result, ControllerBase controller) =>
        result.IsSuccess ? controller.NoContent() : controller.ToProblem(result.Error!);

    public static ActionResult<T> ToActionResult<T>(this Result<T> result, ControllerBase controller) =>
        result.IsSuccess ? controller.Ok(result.Value) : controller.ToProblem(result.Error!);

    public static ObjectResult ToProblem(this ControllerBase controller, Error error) =>
        controller.Problem(
            detail: error.Message,
            statusCode: (int)error.Type,
            title: TitleFor(error.Type));

    private static string TitleFor(ErrorType type) => type switch
    {
        ErrorType.Validation => "Validation failed",
        ErrorType.Unauthorized => "Unauthorized",
        ErrorType.Forbidden => "Forbidden",
        ErrorType.NotFound => "Not found",
        ErrorType.Conflict => "Conflict",
        _ => "Error",
    };
}
