using FluentValidation;

using PharmaFlow.Domain.Studies;

namespace PharmaFlow.Application.Studies.Commands.CreateStudy;

public sealed class CreateStudyValidator : AbstractValidator<CreateStudyCommand>
{
    public CreateStudyValidator()
    {
        RuleFor(x => x.ProtocolNumber)
            .NotEmpty()
            .MaximumLength(Study.MaxProtocolNumberLength);

        RuleFor(x => x.Title)
            .NotEmpty()
            .MaximumLength(Study.MaxTitleLength);

        RuleFor(x => x.Phase)
            .IsInEnum();

        RuleFor(x => x.TherapeuticArea)
            .NotEmpty()
            .MaximumLength(Study.MaxTherapeuticAreaLength);

        RuleFor(x => x.SponsorOrganization)
            .NotEmpty()
            .MaximumLength(Study.MaxSponsorOrganizationLength);

        RuleFor(x => x.PlannedEnrolment)
            .GreaterThanOrEqualTo(Study.MinPlannedEnrolment);

        RuleFor(x => x.PlannedEndDate)
            .GreaterThan(x => x.PlannedStartDate);
    }
}