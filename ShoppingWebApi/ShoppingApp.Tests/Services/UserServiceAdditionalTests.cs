using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using ShoppingWebApi.Contexts;
using ShoppingWebApi.Exceptions;
using ShoppingWebApi.Interfaces;
using ShoppingWebApi.Models;
using ShoppingWebApi.Models.DTOs.Users;
using ShoppingWebApi.Services;

namespace ShoppingApp.Tests.Services;

public class UserServiceAdditionalTests
{
    private readonly Mock<IRepository<int, User>> _userRepoMock = new();
    private readonly Mock<IRepository<int, UserDetails>> _detailsRepoMock = new();
    private readonly Mock<ILogger<UserService>> _loggerMock = new();

    private AppDbContext CreateCtx(Action<AppDbContext>? seed = null)
    {
        var opts = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var ctx = new AppDbContext(opts);
        seed?.Invoke(ctx);
        ctx.SaveChanges();
        return ctx;
    }

    private UserService BuildSut(AppDbContext ctx)
    {
        _userRepoMock.Setup(r => r.GetQueryable()).Returns(() => ctx.Users);
        return new UserService(_userRepoMock.Object, _detailsRepoMock.Object, ctx, _loggerMock.Object);
    }

    // ── GetProfileAsync ───────────────────────────────────────────────────────

    [Fact]
    public async Task GetProfile_Found_ReturnsDto()
    {
        using var ctx = CreateCtx(c =>
            c.Users.Add(new User { Id = 1, Email = "a@b.com", Role = "User", PasswordHash = "x" }));
        var sut = BuildSut(ctx);

        var result = await sut.GetProfileAsync(1);

        Assert.NotNull(result);
        Assert.Equal("a@b.com", result!.Email);
    }

    [Fact]
    public async Task GetProfile_NotFound_ReturnsNull()
    {
        using var ctx = CreateCtx();
        var sut = BuildSut(ctx);

        var result = await sut.GetProfileAsync(99);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetProfile_WithDetails_ReturnsFullProfile()
    {
        using var ctx = CreateCtx(c =>
        {
            c.Users.Add(new User { Id = 1, Email = "a@b.com", Role = "User", PasswordHash = "x" });
            c.SaveChanges();
            c.UserDetails.Add(new UserDetails { UserId = 1, FirstName = "John", LastName = "Doe", Phone = "9999" });
        });
        var sut = BuildSut(ctx);

        var result = await sut.GetProfileAsync(1);

        Assert.Equal("John", result!.FirstName);
        Assert.Equal("Doe", result.LastName);
        Assert.Equal("9999", result.Phone);
    }

    // ── UpdateProfileAsync — no existing details ──────────────────────────────

    [Fact]
    public async Task UpdateProfile_NoExistingDetails_CreatesDetails()
    {
        using var ctx = CreateCtx(c =>
            c.Users.Add(new User { Id = 1, Email = "a@b.com", Role = "User", PasswordHash = "x" }));
        var sut = BuildSut(ctx);

        var newDetails = new UserDetails { UserId = 1, FirstName = "Jane", LastName = "Smith" };
        _detailsRepoMock.Setup(r => r.Add(It.IsAny<UserDetails>())).ReturnsAsync(newDetails);
        _userRepoMock.Setup(r => r.Update(1, It.IsAny<User>()))
            .ReturnsAsync(ctx.Users.First());

        var dto = new UpdateUserProfileDto { FirstName = "Jane", LastName = "Smith" };
        var result = await sut.UpdateProfileAsync(1, dto);

        Assert.Equal("Jane", result.FirstName);
        _detailsRepoMock.Verify(r => r.Add(It.IsAny<UserDetails>()), Times.Once);
    }

    [Fact]
    public async Task UpdateProfile_UserNotFound_ThrowsNotFoundException()
    {
        using var ctx = CreateCtx();
        var sut = BuildSut(ctx);

        var dto = new UpdateUserProfileDto { FirstName = "Jane", LastName = "Smith" };

        await Assert.ThrowsAsync<NotFoundException>(() => sut.UpdateProfileAsync(99, dto));
    }

    // ── UpdateProfileAsync — existing details ─────────────────────────────────

    [Fact]
    public async Task UpdateProfile_ExistingDetails_UpdatesFields()
    {
        using var ctx = CreateCtx(c =>
        {
            c.Users.Add(new User { Id = 1, Email = "a@b.com", Role = "User", PasswordHash = "x" });
            c.SaveChanges();
            c.UserDetails.Add(new UserDetails { UserId = 1, FirstName = "Old", LastName = "Name" });
        });
        var sut = BuildSut(ctx);
        var details = ctx.UserDetails.First();
        _detailsRepoMock.Setup(r => r.Update(1, It.IsAny<UserDetails>())).ReturnsAsync(details);
        _userRepoMock.Setup(r => r.Update(1, It.IsAny<User>())).ReturnsAsync(ctx.Users.First());

        var dto = new UpdateUserProfileDto { FirstName = "New", LastName = "Name" };
        var result = await sut.UpdateProfileAsync(1, dto);

        Assert.Equal("New", result.FirstName);
    }

    [Fact]
    public async Task UpdateProfile_OnlyPhone_UpdatesPhone()
    {
        using var ctx = CreateCtx(c =>
        {
            c.Users.Add(new User { Id = 1, Email = "a@b.com", Role = "User", PasswordHash = "x" });
            c.SaveChanges();
            c.UserDetails.Add(new UserDetails { UserId = 1, FirstName = "John", LastName = "Doe" });
        });
        var sut = BuildSut(ctx);
        var details = ctx.UserDetails.First();
        _detailsRepoMock.Setup(r => r.Update(1, It.IsAny<UserDetails>())).ReturnsAsync(details);
        _userRepoMock.Setup(r => r.Update(1, It.IsAny<User>())).ReturnsAsync(ctx.Users.First());

        var dto = new UpdateUserProfileDto { Phone = "9876543210" };
        var result = await sut.UpdateProfileAsync(1, dto);

        Assert.Equal("John", result.FirstName); // unchanged
        Assert.Equal("9876543210", result.Phone);
    }

    [Fact]
    public async Task UpdateProfile_DateOfBirth_IsUpdated()
    {
        using var ctx = CreateCtx(c =>
        {
            c.Users.Add(new User { Id = 1, Email = "a@b.com", Role = "User", PasswordHash = "x" });
            c.SaveChanges();
            c.UserDetails.Add(new UserDetails { UserId = 1, FirstName = "John", LastName = "Doe" });
        });
        var sut = BuildSut(ctx);
        var details = ctx.UserDetails.First();
        _detailsRepoMock.Setup(r => r.Update(1, It.IsAny<UserDetails>())).ReturnsAsync(details);
        _userRepoMock.Setup(r => r.Update(1, It.IsAny<User>())).ReturnsAsync(ctx.Users.First());

        var dob = new DateTime(1990, 5, 15);
        var dto = new UpdateUserProfileDto { DateOfBirth = dob };
        var result = await sut.UpdateProfileAsync(1, dto);

        Assert.Equal(dob, result.DateOfBirth);
    }

    // ── GetPagedAsync — sort variants ─────────────────────────────────────────

    [Fact]
    public async Task GetPaged_SortByEmail_Works()
    {
        using var ctx = CreateCtx(c => c.Users.AddRange(
            new User { Email = "z@test.com", Role = "User", PasswordHash = "x" },
            new User { Email = "a@test.com", Role = "User", PasswordHash = "x" }));
        var sut = BuildSut(ctx);

        var result = await sut.GetPagedAsync(null, null, null, "email", false, 1, 10);

        Assert.Equal("a@test.com", result.Items[0].Email);
    }

    [Fact]
    public async Task GetPaged_SortByEmailDesc_Works()
    {
        using var ctx = CreateCtx(c => c.Users.AddRange(
            new User { Email = "a@test.com", Role = "User", PasswordHash = "x" },
            new User { Email = "z@test.com", Role = "User", PasswordHash = "x" }));
        var sut = BuildSut(ctx);

        var result = await sut.GetPagedAsync(null, null, null, "email", true, 1, 10);

        Assert.Equal("z@test.com", result.Items[0].Email);
    }

    [Fact]
    public async Task GetPaged_SortByRole_Works()
    {
        using var ctx = CreateCtx(c => c.Users.AddRange(
            new User { Email = "a@test.com", Role = "User", PasswordHash = "x" },
            new User { Email = "b@test.com", Role = "Admin", PasswordHash = "x" }));
        var sut = BuildSut(ctx);

        var result = await sut.GetPagedAsync(null, null, null, "role", false, 1, 10);

        Assert.Equal(2, result.TotalCount);
    }

    [Fact]
    public async Task GetPaged_SortByName_Works()
    {
        using var ctx = CreateCtx(c =>
        {
            c.Users.AddRange(
                new User { Id = 1, Email = "a@test.com", Role = "User", PasswordHash = "x" },
                new User { Id = 2, Email = "b@test.com", Role = "User", PasswordHash = "x" });
            c.SaveChanges();
            c.UserDetails.AddRange(
                new UserDetails { UserId = 1, FirstName = "Zara", LastName = "Ali" },
                new UserDetails { UserId = 2, FirstName = "Anna", LastName = "Bee" });
        });
        var sut = BuildSut(ctx);

        var result = await sut.GetPagedAsync(null, null, null, "name", false, 1, 10);

        Assert.Equal(2, result.TotalCount);
    }

    [Fact]
    public async Task GetPaged_NameFilter_Works()
    {
        using var ctx = CreateCtx(c =>
        {
            c.Users.AddRange(
                new User { Id = 1, Email = "a@test.com", Role = "User", PasswordHash = "x" },
                new User { Id = 2, Email = "b@test.com", Role = "User", PasswordHash = "x" });
            c.SaveChanges();
            c.UserDetails.AddRange(
                new UserDetails { UserId = 1, FirstName = "Alice", LastName = "Smith" },
                new UserDetails { UserId = 2, FirstName = "Bob", LastName = "Jones" });
        });
        var sut = BuildSut(ctx);

        var result = await sut.GetPagedAsync(null, null, "Alice", null, false, 1, 10);

        Assert.Single(result.Items);
    }

    [Fact]
    public async Task GetPaged_PageSizeZero_DefaultsTo10()
    {
        using var ctx = CreateCtx(c => c.Users.Add(
            new User { Email = "a@test.com", Role = "User", PasswordHash = "x" }));
        var sut = BuildSut(ctx);

        var result = await sut.GetPagedAsync(null, null, null, null, false, 0, 0);

        Assert.Equal(1, result.TotalCount);
    }

    [Fact]
    public async Task GetPaged_Pagination_Works()
    {
        using var ctx = CreateCtx(c =>
        {
            for (int i = 1; i <= 5; i++)
                c.Users.Add(new User { Email = $"user{i}@test.com", Role = "User", PasswordHash = "x" });
        });
        var sut = BuildSut(ctx);

        var result = await sut.GetPagedAsync(null, null, null, null, false, 2, 2);

        Assert.Equal(2, result.Items.Count);
        Assert.Equal(5, result.TotalCount);
    }
}
