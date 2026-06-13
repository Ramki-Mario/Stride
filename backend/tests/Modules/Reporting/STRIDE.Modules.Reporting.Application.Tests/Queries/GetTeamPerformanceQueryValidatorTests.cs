using STRIDE.Modules.Reporting.Application.Queries.GetTeamPerformance;

namespace STRIDE.Modules.Reporting.Application.Tests.Queries;

public sealed class GetTeamPerformanceQueryValidatorTests
{
    private readonly GetTeamPerformanceQueryValidator _sut = new();

    private static readonly Guid     TenantId = Guid.NewGuid();
    private static readonly DateTime From     = new(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime To       = new(2026, 3, 31, 0, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task Validate_ValidQuery_PassesValidation()
    {
        var result = await _sut.ValidateAsync(new GetTeamPerformanceQuery(TenantId, From, To));

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task Validate_EmptyTenantId_Fails()
    {
        var result = await _sut.ValidateAsync(new GetTeamPerformanceQuery(Guid.Empty, From, To));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "TenantId");
    }

    [Fact]
    public async Task Validate_ToDateBeforeFromDate_Fails()
    {
        var result = await _sut.ValidateAsync(new GetTeamPerformanceQuery(TenantId, To, From));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "ToDate");
    }

    [Fact]
    public async Task Validate_RangeExceeds366Days_Fails()
    {
        var longFrom = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var longTo   = longFrom.AddDays(367);

        var result = await _sut.ValidateAsync(new GetTeamPerformanceQuery(TenantId, longFrom, longTo));

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task Validate_RangeExactly366Days_Passes()
    {
        var from = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var to   = from.AddDays(366);

        var result = await _sut.ValidateAsync(new GetTeamPerformanceQuery(TenantId, from, to));

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task Validate_WithOptionalRoleId_Passes()
    {
        var result = await _sut.ValidateAsync(
            new GetTeamPerformanceQuery(TenantId, From, To, RoleId: Guid.NewGuid()));

        result.IsValid.Should().BeTrue();
    }
}
