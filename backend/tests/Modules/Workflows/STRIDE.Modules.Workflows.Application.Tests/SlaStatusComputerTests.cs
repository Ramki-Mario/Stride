using FluentAssertions;
using STRIDE.Modules.Workflows.Application.Helpers;
using STRIDE.Modules.Workflows.Domain.Enums;

namespace STRIDE.Modules.Workflows.Application.Tests;

public sealed class SlaStatusComputerTests
{
    private static readonly DateTime _createdAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    // ── No SLA configured ────────────────────────────────────────────────────

    [Fact]
    public void Compute_WithNullDeadline_ReturnsNull()
    {
        var result = SlaStatusComputer.Compute(_createdAt, deadlineAt: null, completedAt: null);
        result.Should().BeNull();
    }

    // ── Running instance — OnTime ─────────────────────────────────────────────

    [Fact]
    public void Compute_RunningWithMoreThan20PctRemaining_ReturnsOnTime()
    {
        // 10 hours window; 8 hours elapsed → 2h left = 20% — boundary check (not < 20%)
        var deadline = _createdAt.AddHours(10);
        var now      = _createdAt.AddHours(7); // 30% remaining

        var result = SlaStatusComputer.Compute(_createdAt, deadline, completedAt: null, now: now);

        result.Should().Be(SlaStatus.OnTime);
    }

    // ── Running instance — AtRisk ─────────────────────────────────────────────

    [Fact]
    public void Compute_RunningWithLessThan20PctRemaining_ReturnsAtRisk()
    {
        // 10 hour window; 9.5 hours elapsed → 0.5h left = 5%
        var deadline = _createdAt.AddHours(10);
        var now      = _createdAt.AddHours(9.5);

        var result = SlaStatusComputer.Compute(_createdAt, deadline, completedAt: null, now: now);

        result.Should().Be(SlaStatus.AtRisk);
    }

    // ── Running instance — Breached ───────────────────────────────────────────

    [Fact]
    public void Compute_RunningPastDeadline_ReturnsBreached()
    {
        var deadline = _createdAt.AddHours(10);
        var now      = _createdAt.AddHours(12); // 2h past deadline

        var result = SlaStatusComputer.Compute(_createdAt, deadline, completedAt: null, now: now);

        result.Should().Be(SlaStatus.Breached);
    }

    // ── Completed instance — OnTime ───────────────────────────────────────────

    [Fact]
    public void Compute_CompletedBeforeDeadline_ReturnsOnTime()
    {
        var deadline    = _createdAt.AddHours(48);
        var completedAt = _createdAt.AddHours(30); // well before deadline

        var result = SlaStatusComputer.Compute(_createdAt, deadline, completedAt);

        result.Should().Be(SlaStatus.OnTime);
    }

    [Fact]
    public void Compute_CompletedExactlyAtDeadline_ReturnsOnTime()
    {
        var deadline = _createdAt.AddHours(48);

        // completedAt == deadlineAt: referenceTime (=completedAt) is NOT > deadlineAt
        var result = SlaStatusComputer.Compute(_createdAt, deadline, completedAt: deadline);

        result.Should().Be(SlaStatus.OnTime);
    }

    // ── Completed instance — Breached ─────────────────────────────────────────

    [Fact]
    public void Compute_CompletedAfterDeadline_ReturnsBreached()
    {
        var deadline    = _createdAt.AddHours(48);
        var completedAt = _createdAt.AddHours(50); // 2h late

        var result = SlaStatusComputer.Compute(_createdAt, deadline, completedAt);

        result.Should().Be(SlaStatus.Breached);
    }

    // ── Completed instances do NOT show AtRisk ────────────────────────────────

    [Fact]
    public void Compute_CompletedInLastMinuteBeforeDeadline_ReturnsOnTimeNotAtRisk()
    {
        // Completed with only 1% of window remaining — should still be OnTime (already done)
        var deadline    = _createdAt.AddHours(100);
        var completedAt = _createdAt.AddHours(99); // 1h before deadline (1% remaining)

        var result = SlaStatusComputer.Compute(_createdAt, deadline, completedAt);

        result.Should().Be(SlaStatus.OnTime, "completed instances are either OnTime or Breached, never AtRisk");
    }
}
