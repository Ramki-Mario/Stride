using STRIDE.Modules.Workflows.Application.Commands.RejectStep;

namespace STRIDE.Modules.Workflows.Application.Tests;

/// <summary>
/// US-172 mandate: a rejection must always carry an explanatory comment.
/// The validator is the enforcement point — the UI modal mirrors these rules.
/// </summary>
public sealed class RejectStepCommandValidatorTests
{
    private readonly RejectStepCommandValidator _sut = new();

    private static RejectStepCommand BuildCommand(string? comment) =>
        new(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), comment);

    [Fact]
    public void Validate_WithValidComment_Passes()
    {
        var result = _sut.Validate(BuildCommand("The figures in section 2 are wrong."));

        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_WithMissingComment_Fails(string? comment)
    {
        var result = _sut.Validate(BuildCommand(comment));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(RejectStepCommand.Comment));
    }

    [Fact]
    public void Validate_WithCommentShorterThanTenChars_Fails()
    {
        var result = _sut.Validate(BuildCommand("too short"));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e =>
            e.PropertyName == nameof(RejectStepCommand.Comment) &&
            e.ErrorMessage.Contains("10"));
    }

    [Fact]
    public void Validate_WithCommentOverThousandChars_Fails()
    {
        var result = _sut.Validate(BuildCommand(new string('x', 1001)));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(RejectStepCommand.Comment));
    }

    [Fact]
    public void Validate_WithEmptyIds_Fails()
    {
        var result = _sut.Validate(new RejectStepCommand(
            Guid.Empty, Guid.Empty, Guid.Empty, "A perfectly valid comment."));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(RejectStepCommand.WorkflowInstanceId));
        result.Errors.Should().Contain(e => e.PropertyName == nameof(RejectStepCommand.StepInstanceId));
        result.Errors.Should().Contain(e => e.PropertyName == nameof(RejectStepCommand.RejectedBy));
    }
}
