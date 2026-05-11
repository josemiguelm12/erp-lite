using ErpLite.Application.Common;
using Microsoft.AspNetCore.Mvc;

namespace ErpLite.Api.Extensions;

public static class OperationResultExtensions
{
    public static ActionResult<T> ToActionResult<T>(this OperationResult<T> result)
    {
        return result.Status switch
        {
            OperationStatus.Success => result.Value!,
            OperationStatus.Forbidden => new ForbidResult(),
            OperationStatus.NotFound => new NotFoundObjectResult(new { error = result.Error }),
            OperationStatus.ValidationError => new BadRequestObjectResult(new { error = result.Error }),
            _ => new BadRequestObjectResult(new { error = result.Error })
        };
    }

    public static IActionResult ToActionResult(this OperationResult result)
    {
        return result.Status switch
        {
            OperationStatus.Success => new NoContentResult(),
            OperationStatus.Forbidden => new ForbidResult(),
            OperationStatus.NotFound => new NotFoundObjectResult(new { error = result.Error }),
            OperationStatus.ValidationError => new BadRequestObjectResult(new { error = result.Error }),
            _ => new BadRequestObjectResult(new { error = result.Error })
        };
    }
}
