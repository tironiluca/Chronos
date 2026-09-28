using FluentValidation;

namespace Chronos.Application.Resources.Queries.GetResourceAvailability;

public class GetResourceAvailabilityQueryValidator : AbstractValidator<GetResourceAvailabilityQuery>
{
    public GetResourceAvailabilityQueryValidator()
    {
        RuleFor(x => x.CallerOrganizationId).NotEmpty();
        RuleFor(x => x.To).GreaterThanOrEqualTo(x => x.From).WithMessage("End date cannot precede start date.");
    }
}
