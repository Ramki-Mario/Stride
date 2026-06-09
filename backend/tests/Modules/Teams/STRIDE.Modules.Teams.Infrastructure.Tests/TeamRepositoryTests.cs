namespace STRIDE.Modules.Teams.Infrastructure.Tests;

/// <summary>
/// Integration-style tests for <see cref="TeamRepository"/> using the EF Core
/// in-memory provider.  Each test gets its own isolated database instance so
/// that state cannot leak between tests.
///
/// These tests also exercise <see cref="TeamsDbContext"/> and
/// <see cref="Configurations.TeamConfiguration"/> (applied via
/// <c>ApplyConfigurationsFromAssembly</c>).
/// </summary>
public sealed class TeamRepositoryTests : IDisposable
{
    private readonly TeamsDbContext _db;
    private readonly TeamRepository _repo;

    public TeamRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<TeamsDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _db   = new TeamsDbContext(options);
        _repo = new TeamRepository(_db);
    }

    public void Dispose() => _db.Dispose();

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static Team MakeTeam(Guid tenantId, string name = "Engineering")
        => Team.Create(new NewTeam(tenantId, name, "A test team.", null, Guid.NewGuid()));

    // ── AddAsync / SaveChangesAsync ──────────────────────────────────────────

    [Fact]
    public async Task AddAsync_ThenSave_PersistsTeamInContext()
    {
        var tenantId = Guid.NewGuid();
        var team     = MakeTeam(tenantId);

        await _repo.AddAsync(team);
        await _repo.SaveChangesAsync();

        var inDb = await _db.Teams.FirstOrDefaultAsync(t => t.Id == team.Id);
        inDb.Should().NotBeNull();
        inDb!.Name.Should().Be("Engineering");
    }

    // ── GetByIdAsync ─────────────────────────────────────────────────────────

    [Fact]
    public async Task GetByIdAsync_ExistingTeam_ReturnsTeam()
    {
        var tenantId = Guid.NewGuid();
        var team     = MakeTeam(tenantId);
        await _repo.AddAsync(team);
        await _repo.SaveChangesAsync();

        var found = await _repo.GetByIdAsync(tenantId, team.Id);

        found.Should().NotBeNull();
        found!.Id.Should().Be(team.Id);
        found.Name.Should().Be("Engineering");
    }

    [Fact]
    public async Task GetByIdAsync_WrongTenantId_ReturnsNull()
    {
        var tenantId = Guid.NewGuid();
        var team     = MakeTeam(tenantId);
        await _repo.AddAsync(team);
        await _repo.SaveChangesAsync();

        var found = await _repo.GetByIdAsync(Guid.NewGuid(), team.Id);

        found.Should().BeNull();
    }

    [Fact]
    public async Task GetByIdAsync_NonExistentId_ReturnsNull()
    {
        var found = await _repo.GetByIdAsync(Guid.NewGuid(), Guid.NewGuid());

        found.Should().BeNull();
    }

    // ── ExistsByNameAsync ────────────────────────────────────────────────────

    [Fact]
    public async Task ExistsByNameAsync_MatchingNameAndTenant_ReturnsTrue()
    {
        var tenantId = Guid.NewGuid();
        await _repo.AddAsync(MakeTeam(tenantId, "Backend"));
        await _repo.SaveChangesAsync();

        var exists = await _repo.ExistsByNameAsync(tenantId, "Backend");

        exists.Should().BeTrue();
    }

    [Fact]
    public async Task ExistsByNameAsync_DifferentTenant_ReturnsFalse()
    {
        var tenantId = Guid.NewGuid();
        await _repo.AddAsync(MakeTeam(tenantId, "Backend"));
        await _repo.SaveChangesAsync();

        var exists = await _repo.ExistsByNameAsync(Guid.NewGuid(), "Backend");

        exists.Should().BeFalse();
    }

    [Fact]
    public async Task ExistsByNameAsync_NameDoesNotExist_ReturnsFalse()
    {
        var tenantId = Guid.NewGuid();

        var exists = await _repo.ExistsByNameAsync(tenantId, "Nonexistent");

        exists.Should().BeFalse();
    }

    [Fact]
    public async Task ExistsByNameAsync_WithExcludeId_ExcludesMatchingTeam()
    {
        var tenantId = Guid.NewGuid();
        var team     = MakeTeam(tenantId, "Frontend");
        await _repo.AddAsync(team);
        await _repo.SaveChangesAsync();

        // Exclude the very team that matches — should look like name is free.
        var exists = await _repo.ExistsByNameAsync(tenantId, "Frontend", excludeId: team.Id);

        exists.Should().BeFalse();
    }

    [Fact]
    public async Task ExistsByNameAsync_WithExcludeId_StillFindsOtherTeam()
    {
        var tenantId = Guid.NewGuid();
        var team1    = MakeTeam(tenantId, "Frontend");
        var team2    = MakeTeam(tenantId, "Backend");
        await _repo.AddAsync(team1);
        await _repo.AddAsync(team2);
        await _repo.SaveChangesAsync();

        // Exclude team1 but "Frontend" also matches team1 only — so false.
        // Test that "Backend" is still found even when excluding a different id.
        var exists = await _repo.ExistsByNameAsync(tenantId, "Backend", excludeId: team1.Id);

        exists.Should().BeTrue();
    }
}
