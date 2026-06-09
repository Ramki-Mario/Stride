using STRIDE.Modules.Teams.Application.Commands.CreateTeam;
using STRIDE.Modules.Teams.Domain.Entities;

namespace STRIDE.Modules.Teams.Application.Tests;

public sealed class CreateTeamCommandValidatorTests
{
    private static CreateTeamCommandValidator Sut() => new();

    private static CreateTeamCommand Valid() =>
        new(Guid.NewGuid(), "Engineering", null, null, Guid.NewGuid());

    [Fact]
    public void Validate_ValidCommand_Passes()
    {
        var result = Sut().Validate(Valid());
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_EmptyName_Fails()
    {
        var cmd    = Valid() with { Name = "" };
        var result = Sut().Validate(cmd);
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validate_NameTooLong_Fails()
    {
        var cmd    = Valid() with { Name = new string('A', Team.NameMaxLength + 1) };
        var result = Sut().Validate(cmd);
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validate_DescriptionTooLong_Fails()
    {
        var cmd    = Valid() with { Description = new string('x', Team.DescriptionMaxLength + 1) };
        var result = Sut().Validate(cmd);
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validate_EmptyTenantId_Fails()
    {
        var cmd    = Valid() with { TenantId = Guid.Empty };
        var result = Sut().Validate(cmd);
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validate_EmptyCreatedBy_Fails()
    {
        var cmd    = Valid() with { CreatedBy = Guid.Empty };
        var result = Sut().Validate(cmd);
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validate_NullDescription_Passes()
    {
        var cmd    = Valid() with { Description = null };
        var result = Sut().Validate(cmd);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_WithParentTeamId_Passes()
    {
        var cmd    = Valid() with { ParentTeamId = Guid.NewGuid() };
        var result = Sut().Validate(cmd);
        result.IsValid.Should().BeTrue();
    }
}
