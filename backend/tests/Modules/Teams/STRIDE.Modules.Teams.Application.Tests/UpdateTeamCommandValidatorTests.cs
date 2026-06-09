using STRIDE.Modules.Teams.Application.Commands.UpdateTeam;
using STRIDE.Modules.Teams.Domain.Entities;

namespace STRIDE.Modules.Teams.Application.Tests;

public sealed class UpdateTeamCommandValidatorTests
{
    private static UpdateTeamCommandValidator Sut() => new();

    private static UpdateTeamCommand Valid() =>
        new(Guid.NewGuid(), Guid.NewGuid(), "Engineering", null, null, Guid.NewGuid());

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
        var cmd    = Valid() with { Name = new string('B', Team.NameMaxLength + 1) };
        var result = Sut().Validate(cmd);
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validate_DescriptionTooLong_Fails()
    {
        var cmd    = Valid() with { Description = new string('y', Team.DescriptionMaxLength + 1) };
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
    public void Validate_EmptyTeamId_Fails()
    {
        var cmd    = Valid() with { TeamId = Guid.Empty };
        var result = Sut().Validate(cmd);
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validate_EmptyUpdatedBy_Fails()
    {
        var cmd    = Valid() with { UpdatedBy = Guid.Empty };
        var result = Sut().Validate(cmd);
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validate_WithParentTeamId_Passes()
    {
        var cmd    = Valid() with { ParentTeamId = Guid.NewGuid() };
        var result = Sut().Validate(cmd);
        result.IsValid.Should().BeTrue();
    }
}
