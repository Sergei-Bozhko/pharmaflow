using System.Data;

using FluentValidation;

using PharmaFlow.Domain.Sites;

namespace PharmaFlow.Application.Modules.Sites.CreateSite.Internal;

internal sealed class CreateSiteValidator : AbstractValidator<CreateSiteCommand>
{
    public CreateSiteValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(Site.MaxNameLength);

        RuleFor(x => x.SiteNumber)
            .NotEmpty()
            .MaximumLength(Site.MaxSiteNumberLength);

        RuleFor(x => x.Country)
            .NotEmpty()
            .Length(Site.CountryCodeLength);

        RuleFor(x => x.PrincipalInvestigatorUserId)
            .NotEmpty();

        RuleFor(x => x.StudyId)
            .NotEmpty();
    }
}