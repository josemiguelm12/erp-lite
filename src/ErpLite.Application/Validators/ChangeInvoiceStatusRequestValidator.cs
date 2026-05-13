using ErpLite.Application.DTOs;
using FluentValidation;

namespace ErpLite.Application.Validators;

public sealed class ChangeInvoiceStatusRequestValidator : AbstractValidator<ChangeInvoiceStatusRequest>
{
    public ChangeInvoiceStatusRequestValidator()
    {
        RuleFor(x => x.Status).IsInEnum();
    }
}
