using FluentValidation.Results;

using PharmaFlow.Application.Studies.Commands.CreateStudy;
using PharmaFlow.Domain.Studies;

namespace PharmaFlow.Tests.Unit.Studies;

public class CreateStudyValidatorTests
{
    private readonly CreateStudyValidator _validator = new();

    [Fact]
    public void Happy_path_has_no_failures()
    {
        var result = _validator.Validate(ValidCommand());
        Assert.True(result.IsValid);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Empty_protocol_number_fails(string protocolNumber)
    {
        var result = _validator.Validate(ValidCommand() with { ProtocolNumber = protocolNumber });
        AssertFailsOn(result, nameof(CreateStudyCommand.ProtocolNumber));
    }

    [Fact]
    public void Long_protocol_number_fails()
    {
        var tooLong = new string('x', Study.MaxProtocolNumberLength + 1);
        var result = _validator.Validate(ValidCommand() with { ProtocolNumber = tooLong });
        AssertFailsOn(result, nameof(CreateStudyCommand.ProtocolNumber));
    }

    [Fact]
    public void Empty_title_fails()
    {
        var result = _validator.Validate(ValidCommand() with { Title = "" });
        AssertFailsOn(result, nameof(CreateStudyCommand.Title));
    }

    [Fact]
    public void Long_title_fails()
    {
        var tooLong = new string('x', Study.MaxTitleLength + 1);
        var result = _validator.Validate(ValidCommand() with { Title = tooLong });
        AssertFailsOn(result, nameof(CreateStudyCommand.Title));
    }

    [Fact]
    public void Undefined_phase_fails()
    {
        var result = _validator.Validate(ValidCommand() with { Phase = (StudyPhase)999 });
        AssertFailsOn(result, nameof(CreateStudyCommand.Phase));
    }

    [Fact]
    public void Empty_therapeutic_area_fails()
    {
        var result = _validator.Validate(ValidCommand() with { TherapeuticArea = "" });
        AssertFailsOn(result, nameof(CreateStudyCommand.TherapeuticArea));
    }

    [Fact]
    public void Long_therapeutic_area_fails()
    {
        var tooLong = new string('x', Study.MaxTherapeuticAreaLength + 1);
        var result = _validator.Validate(ValidCommand() with { TherapeuticArea = tooLong });
        AssertFailsOn(result, nameof(CreateStudyCommand.TherapeuticArea));
    }

    [Fact]
    public void Empty_sponsor_organization_fails()
    {
        var result = _validator.Validate(ValidCommand() with { SponsorOrganization = "" });
        AssertFailsOn(result, nameof(CreateStudyCommand.SponsorOrganization));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Non_positive_planned_enrolment_fails(int enrolment)
    {
        var result = _validator.Validate(ValidCommand() with { PlannedEnrolment = enrolment });
        AssertFailsOn(result, nameof(CreateStudyCommand.PlannedEnrolment));
    }

    [Fact]
    public void End_date_equal_to_start_fails()
    {
        var start = new DateOnly(2026, 6, 1);
        var result = _validator.Validate(ValidCommand() with { PlannedStartDate = start, PlannedEndDate = start });
        AssertFailsOn(result, nameof(CreateStudyCommand.PlannedEndDate));
    }

    [Fact]
    public void End_date_before_start_fails()
    {
        var result = _validator.Validate(ValidCommand() with
        {
            PlannedStartDate = new DateOnly(2026, 6, 1),
            PlannedEndDate = new DateOnly(2026, 5, 31),
        });
        AssertFailsOn(result, nameof(CreateStudyCommand.PlannedEndDate));
    }

    private static CreateStudyCommand ValidCommand() => new(
        ProtocolNumber: "PROTO-001",
        Title: "Phase II Cardiovascular Study",
        Phase: StudyPhase.PhaseII,
        TherapeuticArea: "Cardiology",
        SponsorOrganization: "Acme Pharma Inc",
        PlannedEnrolment: 200,
        PlannedStartDate: new DateOnly(2026, 6, 1),
        PlannedEndDate: new DateOnly(2027, 5, 31));

    private static void AssertFailsOn(ValidationResult result, string propertyName)
    {
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == propertyName);
    }
}
