using ErpLite.Application.DTOs;
using FluentValidation;

namespace ErpLite.Application.Validators;

public sealed class AssignPermissionToRoleRequestValidator : AbstractValidator<AssignPermissionToRoleRequest>
{
    public AssignPermissionToRoleRequestValidator()
    {
        RuleFor(x => x.PermissionName).NotEmpty().MaximumLength(150);
    }
}
