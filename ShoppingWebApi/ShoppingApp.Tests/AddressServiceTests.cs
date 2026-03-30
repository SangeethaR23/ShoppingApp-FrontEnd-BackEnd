using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using ShoppingWebApi.Contexts;
using ShoppingWebApi.Exceptions;
using ShoppingWebApi.Interfaces;
using ShoppingWebApi.Models;
using ShoppingWebApi.Models.DTOs.Address;
using ShoppingWebApi.Services;

namespace ShoppingApp.Tests.Services
{
    public class AddressServiceTests
    {
        private readonly Mock<IRepository<int, Address>> _addressRepoMock;
        private readonly Mock<IRepository<int, User>> _userRepoMock;
        private readonly Mock<ILogger<AddressService>> _loggerMock;
        private readonly AddressService _sut;

        private List<Address> _addresses;

        public AddressServiceTests()
        {
            _addressRepoMock = new Mock<IRepository<int, Address>>();
            _userRepoMock = new Mock<IRepository<int, User>>();
            _loggerMock = new Mock<ILogger<AddressService>>();

            _addresses = new List<Address>();
            _addressRepoMock.Setup(r => r.GetQueryable()).Returns(() => _addresses.AsQueryable());

            _sut = new AddressService(_addressRepoMock.Object, _userRepoMock.Object, _loggerMock.Object);
        }

        // Helper: create an in-memory DbContext seeded with the given addresses
        private static AppDbContext CreateInMemoryContext(params Address[] addresses)
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            var ctx = new AppDbContext(options);
            ctx.Addresses.AddRange(addresses);
            ctx.SaveChanges();
            return ctx;
        }

        // Helper: build an AddressService wired to an in-memory DbContext
        private AddressService BuildServiceWithContext(AppDbContext ctx)
        {
            _addressRepoMock.Setup(r => r.GetQueryable()).Returns(() => ctx.Addresses);
            return new AddressService(_addressRepoMock.Object, _userRepoMock.Object, _loggerMock.Object);
        }

        private static AddressCreateDto MakeCreateDto(int userId = 1) => new AddressCreateDto
        {
            UserId = userId,
            Label = "Home",
            FullName = "John Doe",
            Phone = "9999999999",
            Line1 = "123 Main St",
            City = "Chennai",
            State = "TN",
            PostalCode = "600001",
            Country = "India"
        };

        // ──────────────────────────────────────────────
        // CREATE
        // ──────────────────────────────────────────────

        [Fact]
        public async Task Create_UserNotFound_ThrowsNotFoundException()
        {
            _userRepoMock.Setup(r => r.Get(99)).ReturnsAsync((User?)null);

            await Assert.ThrowsAsync<NotFoundException>(() => _sut.CreateAsync(MakeCreateDto(99)));
        }

        [Fact]
        public async Task Create_Success_ReturnsDto()
        {
            _userRepoMock.Setup(r => r.Get(1)).ReturnsAsync(new User { Id = 1, Email = "a@b.com" });
            var added = new Address
            {
                Id = 10,
                UserId = 1,
                Label = "Home",
                FullName = "John Doe",
                Line1 = "123 Main St",
                City = "Chennai",
                State = "TN",
                PostalCode = "600001",
                Country = "India"
            };
            _addressRepoMock.Setup(r => r.Add(It.IsAny<Address>())).ReturnsAsync(added);

            var result = await _sut.CreateAsync(MakeCreateDto());

            Assert.Equal(10, result.Id);
            Assert.Equal("Home", result.Label);
        }

        // ──────────────────────────────────────────────
        // GET BY ID
        // ──────────────────────────────────────────────


        [Fact]
        public async Task GetById_Found_ReturnsDto()
        {
            var address = new Address
            {
                Id = 1,
                UserId = 1,
                Label = "Office",
                FullName = "Jane",
                Line1 = "456 Work Ave",
                City = "Bangalore",
                State = "KA",
                PostalCode = "560001",
                Country = "India"
            };

            using var ctx = CreateInMemoryContext(address);
            var sut = BuildServiceWithContext(ctx);

            var result = await sut.GetByIdAsync(1);

            Assert.NotNull(result);
            Assert.Equal("Office", result!.Label);
        }

        [Fact]
        public async Task GetById_NotFound_ReturnsNull()
        {
            using var ctx = CreateInMemoryContext(); // empty
            var sut = BuildServiceWithContext(ctx);

            var result = await sut.GetByIdAsync(999);

            Assert.Null(result);
        }

        // ──────────────────────────────────────────────
        // GET BY USER (paged)
        // ──────────────────────────────────────────────

        [Fact]
        public async Task GetByUser_ReturnsOnlyUserAddresses()
        {
            _userRepoMock.Setup(r => r.Get(1)).ReturnsAsync(new User { Id = 1 });

            using var ctx = CreateInMemoryContext(
                new Address { Id = 1, UserId = 1, Label = "Home", FullName = "A", Line1 = "L1", City = "C", State = "S", PostalCode = "P", Country = "India" },
                new Address { Id = 2, UserId = 1, Label = "Work", FullName = "A", Line1 = "L2", City = "C", State = "S", PostalCode = "P", Country = "India" },
                new Address { Id = 3, UserId = 2, Label = "Home", FullName = "B", Line1 = "L3", City = "C", State = "S", PostalCode = "P", Country = "India" }
            );
            var sut = BuildServiceWithContext(ctx);

            var result = await sut.GetByUserAsync(1, 1, 10);

            Assert.Equal(2, result.TotalCount);
        }

        [Fact]
        public async Task GetByUser_Paging()
        {
            _userRepoMock.Setup(r => r.Get(1)).ReturnsAsync(new User { Id = 1 });

            var addresses = Enumerable.Range(1, 5)
                .Select(i => new Address { Id = i, UserId = 1, Label = "H", FullName = "A", Line1 = "L", City = "C", State = "S", PostalCode = "P", Country = "India" })
                .ToArray();

            using var ctx = CreateInMemoryContext(addresses);
            var sut = BuildServiceWithContext(ctx);

            var result = await sut.GetByUserAsync(1, 2, 2);

            Assert.Equal(2, result.Items.Count);
            Assert.Equal(2, result.PageNumber);
        }

        // ──────────────────────────────────────────────
        // UPDATE
        // ──────────────────────────────────────────────

        [Fact]
        public async Task Update_NotFound_ThrowsNotFoundException()
        {
            _addressRepoMock.Setup(r => r.Get(99)).ReturnsAsync((Address?)null);

            await Assert.ThrowsAsync<NotFoundException>(() =>
                _sut.UpdateAsync(99, 1, new AddressUpdateDto { Label = "X", FullName = "X", Line1 = "X", City = "X", State = "X", PostalCode = "X", Country = "X" }));
        }

        [Fact]
        public async Task Update_WrongUser_ThrowsForbidden()
        {
            _addressRepoMock.Setup(r => r.Get(1)).ReturnsAsync(new Address { Id = 1, UserId = 2, Label = "Home", FullName = "A", Line1 = "L", City = "C", State = "S", PostalCode = "P", Country = "India" });

            await Assert.ThrowsAsync<ForbiddenException>(() =>
                _sut.UpdateAsync(1, 99, new AddressUpdateDto { Label = "X", FullName = "X", Line1 = "X", City = "X", State = "X", PostalCode = "X", Country = "X" }));
        }

        [Fact]
        public async Task Update_Success_ReturnsTrue()
        {
            var address = new Address { Id = 1, UserId = 1, Label = "Old", FullName = "A", Line1 = "L", City = "C", State = "S", PostalCode = "P", Country = "India" };
            _addressRepoMock.Setup(r => r.Get(1)).ReturnsAsync(address);
            _addressRepoMock.Setup(r => r.Update(1, It.IsAny<Address>())).ReturnsAsync(address);

            var result = await _sut.UpdateAsync(1, 1, new AddressUpdateDto
            { Label = "New", FullName = "B", Line1 = "L2", City = "C2", State = "S2", PostalCode = "P2", Country = "India" });

            Assert.True(result);
        }

        // ──────────────────────────────────────────────
        // DELETE
        // ──────────────────────────────────────────────

        [Fact]
        public async Task Delete_NotFound_ReturnsFalse()
        {
            _addressRepoMock.Setup(r => r.Get(99)).ReturnsAsync((Address?)null);

            var result = await _sut.DeleteAsync(99, 1);

            Assert.False(result);
        }

        [Fact]
        public async Task Delete_WrongUser_ThrowsForbidden()
        {
            _addressRepoMock.Setup(r => r.Get(1)).ReturnsAsync(new Address { Id = 1, UserId = 2, Label = "H", FullName = "A", Line1 = "L", City = "C", State = "S", PostalCode = "P", Country = "India" });

            await Assert.ThrowsAsync<ForbiddenException>(() => _sut.DeleteAsync(1, 99));
        }

        [Fact]
        public async Task Delete_Success_ReturnsTrue()
        {
            var address = new Address { Id = 1, UserId = 1, Label = "H", FullName = "A", Line1 = "L", City = "C", State = "S", PostalCode = "P", Country = "India" };
            _addressRepoMock.Setup(r => r.Get(1)).ReturnsAsync(address);
            _addressRepoMock.Setup(r => r.Delete(1)).ReturnsAsync(address);

            var result = await _sut.DeleteAsync(1, 1);

            Assert.True(result);
        }
    }
}
