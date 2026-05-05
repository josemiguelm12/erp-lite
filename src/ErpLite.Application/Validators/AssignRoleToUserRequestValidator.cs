using ErpLite.Application.DTOs;
using FluentValidation;

namespace ErpLite.Application.Validators;

public sealed class AssignRoleToUserRequestValidator : AbstractValidator<AssignRoleToUserRequest>
{
    public AssignRoleToUserRequestValidator()
    {
        RuleFor(x => x.RoleId).NotEmpty();
    }
}
