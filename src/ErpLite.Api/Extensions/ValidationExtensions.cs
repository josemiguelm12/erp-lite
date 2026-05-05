using FluentValidation;
using Microsoft.AspNetCore.Mvc;

namespace ErpLite.Api.Extensions;

public static class ValidationExtensions
{
    public static async Task<ActionResult?> ToBadRequestAsync<T>(this IValidator<T> validator, T request, CancellationToken cancellationToken)
    {
        var validation = await validator.ValidateAsync(request, cancellationToken);
        if (validation.IsValid)
        {
            return null;
        }

        return new BadRequestObjectResult(new
        {
            errors = validation.Errors.Select(x => new { x.PropertyName, x.ErrorMessage })
        });
    }
}
