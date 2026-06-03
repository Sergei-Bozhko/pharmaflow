using System.Text.RegularExpressions;

using PharmaFlow.Domain.Common;
using PharmaFlow.Domain.Common.Ids;
using PharmaFlow.Domain.Sites.Events;

namespace PharmaFlow.Domain.Sites;

public sealed partial class Site : Entity<SiteId>
{
    public StudyId StudyId { get; private set; }
    public string SiteNumber { get; private set; } = default!;
    public string Name { get; private set; } = default!;
    public string Country { get; private set; } = default!;
    public UserId PrincipalInvestigatorUserId { get; private set; }
    public DateTimeOffset? ActivationDate { get; private set; }
    public SiteStatus Status { get; private set; }

    public const int MaxSiteNumberLength = 20;
    public const int MaxNameLength = 200;
    public const int CountryCodeLength = 2;


    private Site() { }

    private Site(
        SiteId id,
        StudyId studyId,
        string siteNumber,
        string name,
        string country,
        UserId principalInvestigatorUserId
    ) : base(id)
    {
        StudyId = studyId;
        SiteNumber = siteNumber;
        Name = name;
        Country = country;
        PrincipalInvestigatorUserId = principalInvestigatorUserId;
        ActivationDate = null;
        Status = SiteStatus.Selected;
    }

    public static Result<Site> Create(
        SiteId id,
        StudyId studyId,
        string siteNumber,
        string name,
        string country,
        UserId principalInvestigatorUserId,
        IClock clock
        )
    {
        if (string.IsNullOrWhiteSpace(siteNumber) || siteNumber.Length > MaxSiteNumberLength)
        {
            return Error.Validation(
                "site.number.invalid",
                "Site number must be non-empty and ≤ 20 characters."
            );
        }

        {
            if (string.IsNullOrWhiteSpace(name) || name.Length > MaxNameLength)
            {
                return Error.Validation(
                    "site.name.invalid",
                    "Site name must be non-empty and ≤ 200 characters.");
            }
        }

        if (!(country.Length == CountryCodeLength && country.All(char.IsAsciiLetterUpper)))
        {
            return Error.Validation(
                "site.country.invalid",
                $"Invalid Country code: {country}."
            );
        }

        if (studyId == StudyId.Empty)
        {
            return Error.Validation(
                "site.study_id.required",
                "StudyId is required."
            );
        }

        if (principalInvestigatorUserId == UserId.Empty)
        {
            return Error.Validation(
                "site.pi_user_id.required",
                "Principal investigator is required."
            );
        }

        var site = new Site(
            id,
            studyId,
            siteNumber,
            name,
            country,
            principalInvestigatorUserId
        );
        site.Raise(new SiteCreated(id, studyId, clock.UtcNow));
        return site;
    }

    public Result Qualify()
    {
        if (Status != SiteStatus.Selected)
        {
            return Error.Conflict(
                "site.transition.invalid",
                $"Cannot qualify a Site with status {Status}."
            );
        }

        Status = SiteStatus.Qualified;
        return Result.Success();
    }

    public Result Initiate()
    {
        if (Status != SiteStatus.Qualified)
        {
            return Error.Conflict(
                "site.transition.invalid",
                $"Cannot initiate a Site with status {Status}."
            );
        }

        Status = SiteStatus.Initiated;
        return Result.Success();
    }

    public Result Activate(
        SignatureMeta sponsorSignature,
        SignatureMeta investigatorSignature,
        IClock clock)
    {
        if (Status != SiteStatus.Initiated)
        {
            return Error.Conflict(
                "site.transition.invalid",
                $"Cannot activate a Site with status {Status}."
            );
        }

        if (sponsorSignature is null || investigatorSignature is null
            || string.IsNullOrWhiteSpace(sponsorSignature.Reason)
            || string.IsNullOrWhiteSpace(investigatorSignature.Reason))
        {
            return Error.Validation(
                "site.activate.signature_reason_required",
                "Both activation signatures must include a non-empty reason."
            );
        }

        Status = SiteStatus.Active;
        ActivationDate = clock.UtcNow;
        Raise(new SiteActivated(Id, sponsorSignature, investigatorSignature, clock.UtcNow));
        return Result.Success();
    }

    public Result Close(
        SignatureMeta signature,
        string reason,
        IClock clock)
    {
        if (Status != SiteStatus.Active)
        {
            return Error.Conflict(
                "site.transition.invalid",
                $"Cannot close a Site with status {Status}."
            );
        }

        if (signature is null || string.IsNullOrWhiteSpace(signature.Reason))
        {
            return Error.Validation(
                "site.close.signature_reason_required",
                "Closing signature must include a non-empty reason."
            );
        }

        if (string.IsNullOrWhiteSpace(reason))
        {
            return Error.Validation(
                "site.close.reason_required",
                "Reason must be non-empty string."
            );
        }

        Status = SiteStatus.Closed;
        Raise(new SiteClosed(Id, reason, signature, clock.UtcNow));
        return Result.Success();
    }
}