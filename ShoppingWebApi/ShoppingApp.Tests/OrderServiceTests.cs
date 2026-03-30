using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using ShoppingWebApi.Contexts;
using ShoppingWebApi.Exceptions;
using ShoppingWebApi.Interfaces;
using ShoppingWebApi.Models;
using ShoppingWebApi.Models.DTOs.Orders;
using ShoppingWebApi.Models.DTOs.Return;
using ShoppingWebApi.Models.enums;
using ShoppingWebApi.Services;

namespace ShoppingApp.Tests.Services
{
    public class OrderServiceTests
    {
        private readonly Mock<IRepository<int, Order>> _orderRepoMock = new();
        private readonly Mock<IRepository<int, OrderItem>> _orderItemRepoMock = new();
        private readonly Mock<IRepository<int, CartItem>> _cartItemRepoMock = new();
        private readonly Mock<IRepository<int, Payment>> _paymentRepoMock = new();
        private readonly Mock<IRepository<int, Refund>> _refundRepoMock = new();
        private readonly Mock<IRepository<int, ReturnRequest>> _returnRepoMock = new();
        private readonly Mock<IRepository<int, Inventory>> _inventoryRepoMock = new();
        private readonly Mock<IPromoService> _promoServiceMock = new();
        private readonly Mock<IWalletService> _walletServiceMock = new();
        private readonly Mock<ILogWriter> _logWriterMock = new();
        private readonly Mock<ILogger<OrderService>> _loggerMock = new();

        public OrderServiceTests()
        {
            _logWriterMock.Setup(l => l.InfoAsync(
                It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<int?>(), It.IsAny<string?>(), It.IsAny<string?>(),
                It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
            _logWriterMock.Setup(l => l.ErrorAsync(
                It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<Exception?>(), It.IsAny<int?>(), It.IsAny<string?>(), It.IsAny<string?>(),
                It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        }

        // ── helpers ──────────────────────────────────────────────────────────

        private AppDbContext CreateCtx()
        {
            var opts = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            return new AppDbContext(opts);
        }

        // Builds an Order with all required ShipTo fields filled (no Payment navigation — seed separately)
        private static Order MakeOrder(int userId, string orderNumber, OrderStatus status,
            decimal total = 0m, decimal walletUsed = 0m,
            List<OrderItem>? items = null) => new Order
            {
                UserId = userId,
                OrderNumber = orderNumber,
                Status = status,
                Total = total,
                WalletUsed = walletUsed,
                ShipToName = "John",
                ShipToLine1 = "L1",
                ShipToCity = "C",
                ShipToState = "S",
                ShipToPostalCode = "P",
                ShipToCountry = "I",
                Items = items ?? new List<OrderItem>()
            };

        // Seeds a Payment row for an order and returns it (with PaymentType set)
        private static Payment SeedPayment(AppDbContext ctx, int orderId, int userId, string paymentType)
        {
            var p = new Payment { OrderId = orderId, UserId = userId, PaymentType = paymentType, TotalAmount = 0m, CreatedAt = DateTime.UtcNow };
            ctx.Payments.Add(p);
            ctx.SaveChanges();
            return p;
        }

        private OrderService BuildSut(AppDbContext ctx)
        {
            _orderRepoMock.Setup(r => r.GetQueryable()).Returns(() => ctx.Orders);

            var userRepoMock = new Mock<IRepository<int, User>>();
            var addressRepoMock = new Mock<IRepository<int, Address>>();
            var cartRepoMock = new Mock<IRepository<int, Carts>>();
            var inventoryRepoMock = new Mock<IRepository<int, Inventory>>();
            var returnRepoMock = new Mock<IRepository<int, ReturnRequest>>();

            userRepoMock.Setup(r => r.GetQueryable()).Returns(() => ctx.Users);
            addressRepoMock.Setup(r => r.GetQueryable()).Returns(() => ctx.Addresses);
            cartRepoMock.Setup(r => r.GetQueryable()).Returns(() => ctx.Carts);
            inventoryRepoMock.Setup(r => r.GetQueryable()).Returns(() => ctx.Inventories);
            returnRepoMock.Setup(r => r.GetQueryable()).Returns(() => ctx.ReturnRequests);

            inventoryRepoMock.Setup(r => r.Update(It.IsAny<int>(), It.IsAny<Inventory>()))
                .Returns((int id, Inventory inv) => _inventoryRepoMock.Object.Update(id, inv));
            returnRepoMock.Setup(r => r.Add(It.IsAny<ReturnRequest>()))
                .Returns((ReturnRequest r) => _returnRepoMock.Object.Add(r));
            returnRepoMock.Setup(r => r.Get(It.IsAny<int>()))
                .Returns((int id) => _returnRepoMock.Object.Get(id));
            returnRepoMock.Setup(r => r.Update(It.IsAny<int>(), It.IsAny<ReturnRequest>()))
                .Returns((int id, ReturnRequest r) => _returnRepoMock.Object.Update(id, r));

            return new OrderService(
                userRepoMock.Object, addressRepoMock.Object,
                cartRepoMock.Object, _cartItemRepoMock.Object,
                _orderRepoMock.Object, _orderItemRepoMock.Object,
                inventoryRepoMock.Object, _paymentRepoMock.Object,
                _refundRepoMock.Object, returnRepoMock.Object,
                _promoServiceMock.Object, _walletServiceMock.Object,
                _logWriterMock.Object, _loggerMock.Object);
        }

        private PlaceOrderRequestDto MakeOrderRequest(int userId = 1, int addressId = 1)
            => new PlaceOrderRequestDto
            {
                UserId = userId,
                AddressId = addressId,
                PaymentType = "UPI",
                ShippingFee = 0m,
                Discount = 0m,
                WalletUseAmount = 0m
            };

        private void SeedValidOrderScenario(AppDbContext ctx, decimal price = 100m, int qty = 1, int stock = 10, string paymentType = "UPI")
        {
            ctx.Users.Add(new User { Email = "a@b.com", PasswordHash = "x" });
            ctx.SaveChanges();
            var user = ctx.Users.First();

            ctx.Addresses.Add(new Address
            {
                UserId = user.Id,
                FullName = "John",
                Phone = "999",
                Line1 = "L1",
                City = "C",
                State = "S",
                PostalCode = "P",
                Country = "India"
            });
            ctx.SaveChanges();
            var address = ctx.Addresses.First();

            var product = new Product { Name = "Widget", SKU = "W1", Price = price, IsActive = true, CategoryId = 1 };
            ctx.Products.Add(product);
            ctx.SaveChanges();

            var cart = new Carts { UserId = user.Id };
            ctx.Carts.Add(cart);
            ctx.SaveChanges();

            ctx.CartItems.Add(new CartItem { CartId = cart.Id, ProductId = product.Id, Quantity = qty, UnitPrice = price });
            ctx.Inventories.Add(new Inventory { ProductId = product.Id, Quantity = stock });
            ctx.SaveChanges();

            var addedOrder = new Order
            {
                Id = 100,
                UserId = user.Id,
                OrderNumber = "ORD-20260101-ABCD1234",
                Status = OrderStatus.Pending,
                PaymentStatus = PaymentStatus.Pending,
                SubTotal = price * qty,
                Total = price * qty,
                Items = new List<OrderItem>()
            };
            _orderRepoMock.Setup(r => r.Add(It.IsAny<Order>())).ReturnsAsync(addedOrder);
            _orderRepoMock.Setup(r => r.Update(It.IsAny<int>(), It.IsAny<Order>())).ReturnsAsync(addedOrder);
            _orderItemRepoMock.Setup(r => r.Add(It.IsAny<OrderItem>())).ReturnsAsync(new OrderItem());
            _inventoryRepoMock.Setup(r => r.Update(It.IsAny<int>(), It.IsAny<Inventory>())).ReturnsAsync(new Inventory());
            _cartItemRepoMock.Setup(r => r.Delete(It.IsAny<int>())).ReturnsAsync(new CartItem());
            _paymentRepoMock.Setup(r => r.Add(It.IsAny<Payment>())).ReturnsAsync(new Payment { PaymentType = paymentType });
        }

        // ═══════════════════════════════════════════════
        // PLACE ORDER
        // ═══════════════════════════════════════════════

        [Fact]
        public async Task PlaceOrder_UserNotFound_ThrowsNotFoundException()
        {
            using var ctx = CreateCtx();
            await Assert.ThrowsAsync<NotFoundException>(() => BuildSut(ctx).PlaceOrderAsync(MakeOrderRequest()));
        }

        [Fact]
        public async Task PlaceOrder_AddressNotFound_ThrowsBusinessValidation()
        {
            using var ctx = CreateCtx();
            ctx.Users.Add(new User { Email = "a@b.com", PasswordHash = "x" });
            ctx.SaveChanges();
            await Assert.ThrowsAsync<BusinessValidationException>(() => BuildSut(ctx).PlaceOrderAsync(MakeOrderRequest()));
        }

        [Fact]
        public async Task PlaceOrder_EmptyCart_ThrowsBusinessValidation()
        {
            using var ctx = CreateCtx();
            ctx.Users.Add(new User { Email = "a@b.com", PasswordHash = "x" });
            ctx.SaveChanges();
            ctx.Addresses.Add(new Address { UserId = ctx.Users.First().Id, FullName = "J", Line1 = "L", City = "C", State = "S", PostalCode = "P", Country = "I" });
            ctx.SaveChanges();
            await Assert.ThrowsAsync<BusinessValidationException>(() => BuildSut(ctx).PlaceOrderAsync(MakeOrderRequest()));
        }

        [Fact]
        public async Task PlaceOrder_CartWithNoItems_ThrowsBusinessValidation()
        {
            using var ctx = CreateCtx();
            ctx.Users.Add(new User { Email = "a@b.com", PasswordHash = "x" });
            ctx.SaveChanges();
            var uid = ctx.Users.First().Id;
            ctx.Addresses.Add(new Address { UserId = uid, FullName = "J", Line1 = "L", City = "C", State = "S", PostalCode = "P", Country = "I" });
            ctx.Carts.Add(new Carts { UserId = uid });
            ctx.SaveChanges();
            await Assert.ThrowsAsync<BusinessValidationException>(() => BuildSut(ctx).PlaceOrderAsync(MakeOrderRequest()));
        }

        [Fact]
        public async Task PlaceOrder_InsufficientInventory_ThrowsBusinessValidation()
        {
            using var ctx = CreateCtx();
            ctx.Users.Add(new User { Email = "a@b.com", PasswordHash = "x" });
            ctx.SaveChanges();
            var uid = ctx.Users.First().Id;
            ctx.Addresses.Add(new Address { UserId = uid, FullName = "J", Line1 = "L", City = "C", State = "S", PostalCode = "P", Country = "I" });
            ctx.SaveChanges();
            ctx.Products.Add(new Product { Name = "W", SKU = "W1", Price = 10m, IsActive = true, CategoryId = 1 });
            ctx.SaveChanges();
            var pid = ctx.Products.First().Id;
            ctx.Carts.Add(new Carts { UserId = uid });
            ctx.SaveChanges();
            var cid = ctx.Carts.First().Id;
            ctx.CartItems.Add(new CartItem { CartId = cid, ProductId = pid, Quantity = 100, UnitPrice = 10m });
            ctx.Inventories.Add(new Inventory { ProductId = pid, Quantity = 5 });
            ctx.SaveChanges();
            await Assert.ThrowsAsync<BusinessValidationException>(() => BuildSut(ctx).PlaceOrderAsync(MakeOrderRequest()));
        }

        [Fact]
        public async Task PlaceOrder_MissingInventory_ThrowsBusinessValidation()
        {
            using var ctx = CreateCtx();
            ctx.Users.Add(new User { Email = "a@b.com", PasswordHash = "x" });
            ctx.SaveChanges();
            var uid = ctx.Users.First().Id;
            ctx.Addresses.Add(new Address { UserId = uid, FullName = "J", Line1 = "L", City = "C", State = "S", PostalCode = "P", Country = "I" });
            ctx.SaveChanges();
            ctx.Products.Add(new Product { Name = "W", SKU = "W1", Price = 10m, IsActive = true, CategoryId = 1 });
            ctx.SaveChanges();
            var pid = ctx.Products.First().Id;
            ctx.Carts.Add(new Carts { UserId = uid });
            ctx.SaveChanges();
            ctx.CartItems.Add(new CartItem { CartId = ctx.Carts.First().Id, ProductId = pid, Quantity = 1, UnitPrice = 10m });
            ctx.SaveChanges();
            await Assert.ThrowsAsync<BusinessValidationException>(() => BuildSut(ctx).PlaceOrderAsync(MakeOrderRequest()));
        }

        [Fact]
        public async Task PlaceOrder_InvalidUnitPrice_ThrowsBusinessValidation()
        {
            using var ctx = CreateCtx();
            ctx.Users.Add(new User { Email = "a@b.com", PasswordHash = "x" });
            ctx.SaveChanges();
            var uid = ctx.Users.First().Id;
            ctx.Addresses.Add(new Address { UserId = uid, FullName = "J", Line1 = "L", City = "C", State = "S", PostalCode = "P", Country = "I" });
            ctx.SaveChanges();
            ctx.Products.Add(new Product { Name = "W", SKU = "W1", Price = 0m, IsActive = true, CategoryId = 1 });
            ctx.SaveChanges();
            var pid = ctx.Products.First().Id;
            ctx.Carts.Add(new Carts { UserId = uid });
            ctx.SaveChanges();
            ctx.CartItems.Add(new CartItem { CartId = ctx.Carts.First().Id, ProductId = pid, Quantity = 1, UnitPrice = 0m });
            ctx.Inventories.Add(new Inventory { ProductId = pid, Quantity = 10 });
            ctx.SaveChanges();
            await Assert.ThrowsAsync<BusinessValidationException>(() => BuildSut(ctx).PlaceOrderAsync(MakeOrderRequest()));
        }

        [Fact]
        public async Task PlaceOrder_Success_ReturnsOrderDto()
        {
            using var ctx = CreateCtx();
            SeedValidOrderScenario(ctx);
            var result = await BuildSut(ctx).PlaceOrderAsync(MakeOrderRequest());
            Assert.NotNull(result);
            Assert.Equal(100, result.Id);
            Assert.StartsWith("ORD-", result.OrderNumber);
        }

        [Fact]
        public async Task PlaceOrder_WithDiscount_TotalReduced()
        {
            using var ctx = CreateCtx();
            SeedValidOrderScenario(ctx, price: 200m);
            var req = MakeOrderRequest(); req.Discount = 50m;
            var result = await BuildSut(ctx).PlaceOrderAsync(req);
            Assert.NotNull(result);
        }

        [Fact]
        public async Task PlaceOrder_WithShippingFee_TotalIncreased()
        {
            using var ctx = CreateCtx();
            SeedValidOrderScenario(ctx);
            var req = MakeOrderRequest(); req.ShippingFee = 50m;
            Assert.NotNull(await BuildSut(ctx).PlaceOrderAsync(req));
        }

        [Fact]
        public async Task PlaceOrder_WithPromoCode_DiscountApplied()
        {
            using var ctx = CreateCtx();
            SeedValidOrderScenario(ctx, price: 500m);
            _promoServiceMock.Setup(p => p.GetValidPromoAsync(It.IsAny<string>(), It.IsAny<decimal>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new PromoCode { Code = "SAVE100", DiscountAmount = 100m });
            var req = MakeOrderRequest(); req.PromoCode = "SAVE100";
            Assert.NotNull(await BuildSut(ctx).PlaceOrderAsync(req));
        }

        [Fact]
        public async Task PlaceOrder_WithWallet_DeductsWalletAmount()
        {
            using var ctx = CreateCtx();
            SeedValidOrderScenario(ctx, price: 500m);
            _walletServiceMock.Setup(w => w.GetAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new Wallet { UserId = 1, Balance = 200m });
            _walletServiceMock.Setup(w => w.DebitAsync(It.IsAny<int>(), It.IsAny<decimal>(), It.IsAny<WalletTxnType>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((200m, 1));
            var req = MakeOrderRequest(); req.WalletUseAmount = 200m;
            Assert.NotNull(await BuildSut(ctx).PlaceOrderAsync(req));
        }

        [Fact]
        public async Task PlaceOrder_COD_PaymentStatusStaysPending()
        {
            using var ctx = CreateCtx();
            SeedValidOrderScenario(ctx, paymentType: "cod");
            var req = MakeOrderRequest(); req.PaymentType = "cod";
            var result = await BuildSut(ctx).PlaceOrderAsync(req);
            Assert.Equal("Pending", result.PaymentStatus);
        }

        [Fact]
        public async Task PlaceOrder_OnlinePayment_TotalZero_StatusPaid()
        {
            using var ctx = CreateCtx();
            SeedValidOrderScenario(ctx, price: 100m);
            _walletServiceMock.Setup(w => w.GetAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new Wallet { UserId = 1, Balance = 200m });
            _walletServiceMock.Setup(w => w.DebitAsync(It.IsAny<int>(), It.IsAny<decimal>(), It.IsAny<WalletTxnType>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((0m, 1));
            var req = MakeOrderRequest(); req.WalletUseAmount = 100m;
            Assert.NotNull(await BuildSut(ctx).PlaceOrderAsync(req));
        }

        [Fact]
        public async Task PlaceOrder_InventoryDecremented()
        {
            using var ctx = CreateCtx();
            SeedValidOrderScenario(ctx, qty: 3, stock: 10);
            await BuildSut(ctx).PlaceOrderAsync(MakeOrderRequest());
            _inventoryRepoMock.Verify(r => r.Update(It.IsAny<int>(), It.Is<Inventory>(inv => inv.Quantity == 7)), Times.Once);
        }

        [Fact]
        public async Task PlaceOrder_CartCleared()
        {
            using var ctx = CreateCtx();
            SeedValidOrderScenario(ctx);
            await BuildSut(ctx).PlaceOrderAsync(MakeOrderRequest());
            _cartItemRepoMock.Verify(r => r.Delete(It.IsAny<int>()), Times.AtLeastOnce);
        }

        // ═══════════════════════════════════════════════
        // GET BY ID
        // ═══════════════════════════════════════════════

        [Fact]
        public async Task GetById_Found_ReturnsOrderDto()
        {
            using var ctx = CreateCtx();
            ctx.Orders.Add(MakeOrder(1, "ORD-001", OrderStatus.Pending));
            ctx.SaveChanges();
            var result = await BuildSut(ctx).GetByIdAsync(ctx.Orders.First().Id);
            Assert.NotNull(result);
            Assert.Equal("ORD-001", result!.OrderNumber);
        }

        [Fact]
        public async Task GetById_NotFound_ReturnsNull()
        {
            using var ctx = CreateCtx();
            Assert.Null(await BuildSut(ctx).GetByIdAsync(999));
        }

        // ═══════════════════════════════════════════════
        // GET USER ORDERS
        // ═══════════════════════════════════════════════

        [Fact]
        public async Task GetUserOrders_ReturnsOnlyUserOrders()
        {
            using var ctx = CreateCtx();
            ctx.Orders.AddRange(
                MakeOrder(1, "ORD-1", OrderStatus.Pending),
                MakeOrder(1, "ORD-2", OrderStatus.Shipped),
                MakeOrder(2, "ORD-3", OrderStatus.Pending)
            );
            ctx.SaveChanges();
            var result = await BuildSut(ctx).GetUserOrdersAsync(1, 1, 10);
            Assert.Equal(2, result.TotalCount);
        }

        [Fact]
        public async Task GetUserOrders_FilterByStatus_Works()
        {
            using var ctx = CreateCtx();
            ctx.Orders.AddRange(
                MakeOrder(1, "O1", OrderStatus.Pending),
                MakeOrder(1, "O2", OrderStatus.Delivered)
            );
            ctx.SaveChanges();
            var result = await BuildSut(ctx).GetUserOrdersAsync(1, 1, 10, status: "Delivered");
            Assert.Single(result.Items);
        }

        [Fact]
        public async Task GetUserOrders_InvalidPage_DefaultsToOne()
        {
            using var ctx = CreateCtx();
            var result = await BuildSut(ctx).GetUserOrdersAsync(1, -1, -1);
            Assert.Equal(1, result.PageNumber);
        }

        // ═══════════════════════════════════════════════
        // CANCEL ORDER
        // ═══════════════════════════════════════════════

        [Fact]
        public async Task CancelOrder_NotFound_ThrowsNotFoundException()
        {
            using var ctx = CreateCtx();
            await Assert.ThrowsAsync<NotFoundException>(() => BuildSut(ctx).CancelOrderAsync(999, 1));
        }

        [Fact]
        public async Task CancelOrder_WrongUser_ThrowsForbidden()
        {
            using var ctx = CreateCtx();
            ctx.Orders.Add(MakeOrder(5, "O1", OrderStatus.Pending));
            ctx.SaveChanges();
            var oid = ctx.Orders.First().Id;
            await Assert.ThrowsAsync<ForbiddenException>(() => BuildSut(ctx).CancelOrderAsync(oid, userId: 99, isAdmin: false));
        }

        [Theory]
        [InlineData(OrderStatus.Shipped)]
        [InlineData(OrderStatus.Delivered)]
        public async Task CancelOrder_ShippedOrDelivered_ThrowsBusinessValidation(OrderStatus status)
        {
            using var ctx = CreateCtx();
            ctx.Orders.Add(MakeOrder(1, "O1", status));
            ctx.SaveChanges();
            var oid = ctx.Orders.First().Id;
            await Assert.ThrowsAsync<BusinessValidationException>(() => BuildSut(ctx).CancelOrderAsync(oid, userId: 1));
        }

        [Fact]
        public async Task CancelOrder_AlreadyCancelled_ReturnsAlreadyCancelledMessage()
        {
            using var ctx = CreateCtx();
            ctx.Orders.Add(MakeOrder(1, "O1", OrderStatus.Cancelled));
            ctx.SaveChanges();
            var oid = ctx.Orders.First().Id;
            var result = await BuildSut(ctx).CancelOrderAsync(oid, userId: 1);
            Assert.Equal("Order already cancelled.", result.Message);
        }

        [Fact]
        public async Task CancelOrder_PendingOrder_SuccessfullyCancelled()
        {
            using var ctx = CreateCtx();
            ctx.Inventories.Add(new Inventory { ProductId = 1, Quantity = 10 });
            ctx.SaveChanges();
            var inv = ctx.Inventories.First();

            ctx.Orders.Add(MakeOrder(1, "O1", OrderStatus.Pending, total: 100m,
                items: new List<OrderItem> { new OrderItem { ProductId = 1, Quantity = 2, ProductName = "P", SKU = "S", UnitPrice = 50m, LineTotal = 100m } }));
            ctx.SaveChanges();
            var order = ctx.Orders.First();
            SeedPayment(ctx, order.Id, order.UserId, "UPI");

            _inventoryRepoMock.Setup(r => r.Update(It.IsAny<int>(), It.IsAny<Inventory>())).ReturnsAsync(inv);
            _orderRepoMock.Setup(r => r.Update(It.IsAny<int>(), It.IsAny<Order>())).ReturnsAsync(order);
            _refundRepoMock.Setup(r => r.Add(It.IsAny<Refund>())).ReturnsAsync(new Refund());
            _walletServiceMock.Setup(w => w.CreditAsync(It.IsAny<int>(), It.IsAny<decimal>(), It.IsAny<WalletTxnType>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((200m, 1));

            var result = await BuildSut(ctx).CancelOrderAsync(order.Id, userId: 1);
            Assert.Equal("Cancelled", result.Status);
            Assert.Contains("cancelled", result.Message, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task CancelOrder_CODOrder_OnlyRefundsWalletUsed()
        {
            using var ctx = CreateCtx();
            ctx.Inventories.Add(new Inventory { ProductId = 1, Quantity = 10 });
            ctx.SaveChanges();
            var inv = ctx.Inventories.First();

            ctx.Orders.Add(MakeOrder(1, "O1", OrderStatus.Pending, total: 200m, walletUsed: 50m,
                items: new List<OrderItem> { new OrderItem { ProductId = 1, Quantity = 1, ProductName = "P", SKU = "S", UnitPrice = 200m, LineTotal = 200m } }));
            ctx.SaveChanges();
            var order = ctx.Orders.First();
            SeedPayment(ctx, order.Id, order.UserId, "cod");

            _inventoryRepoMock.Setup(r => r.Update(It.IsAny<int>(), It.IsAny<Inventory>())).ReturnsAsync(inv);
            _orderRepoMock.Setup(r => r.Update(It.IsAny<int>(), It.IsAny<Order>())).ReturnsAsync(order);
            _refundRepoMock.Setup(r => r.Add(It.IsAny<Refund>())).ReturnsAsync(new Refund());
            _walletServiceMock.Setup(w => w.CreditAsync(It.IsAny<int>(), It.IsAny<decimal>(), It.IsAny<WalletTxnType>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((50m, 1));

            await BuildSut(ctx).CancelOrderAsync(order.Id, userId: 1);
            _walletServiceMock.Verify(w => w.CreditAsync(order.UserId, 50m, WalletTxnType.CreditRefund, It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task CancelOrder_InventoryRestored()
        {
            using var ctx = CreateCtx();
            ctx.Inventories.Add(new Inventory { ProductId = 1, Quantity = 10 });
            ctx.SaveChanges();
            var inv = ctx.Inventories.First();

            ctx.Orders.Add(MakeOrder(1, "O1", OrderStatus.Pending, total: 100m,
                items: new List<OrderItem> { new OrderItem { ProductId = 1, Quantity = 3, ProductName = "P", SKU = "S", UnitPrice = 33m, LineTotal = 99m } }));
            ctx.SaveChanges();
            var order = ctx.Orders.First();
            SeedPayment(ctx, order.Id, order.UserId, "UPI");

            _inventoryRepoMock.Setup(r => r.Update(It.IsAny<int>(), It.IsAny<Inventory>())).ReturnsAsync(inv);
            _orderRepoMock.Setup(r => r.Update(It.IsAny<int>(), It.IsAny<Order>())).ReturnsAsync(order);
            _refundRepoMock.Setup(r => r.Add(It.IsAny<Refund>())).ReturnsAsync(new Refund());
            _walletServiceMock.Setup(w => w.CreditAsync(It.IsAny<int>(), It.IsAny<decimal>(), It.IsAny<WalletTxnType>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((100m, 1));

            await BuildSut(ctx).CancelOrderAsync(order.Id, userId: 1);
            _inventoryRepoMock.Verify(r => r.Update(inv.Id, It.Is<Inventory>(i => i.Quantity == 13)), Times.Once);
        }

        // ═══════════════════════════════════════════════
        // UPDATE STATUS
        // ═══════════════════════════════════════════════

        [Fact]
        public async Task UpdateStatus_InvalidStatus_ThrowsBusinessValidation()
        {
            using var ctx = CreateCtx();
            await Assert.ThrowsAsync<BusinessValidationException>(() => BuildSut(ctx).UpdateStatusAsync(1, "InvalidStatus"));
        }

        [Fact]
        public async Task UpdateStatus_OrderNotFound_ReturnsFalse()
        {
            using var ctx = CreateCtx();
            _orderRepoMock.Setup(r => r.Get(99)).ReturnsAsync((Order?)null);
            Assert.False(await BuildSut(ctx).UpdateStatusAsync(99, "Shipped"));
        }

        [Fact]
        public async Task UpdateStatus_CancelledOrder_ThrowsConflict()
        {
            using var ctx = CreateCtx();
            var order = new Order { Id = 1, Status = OrderStatus.Cancelled, UserId = 1, OrderNumber = "O1" };
            _orderRepoMock.Setup(r => r.Get(1)).ReturnsAsync(order);
            await Assert.ThrowsAsync<ConflictException>(() => BuildSut(ctx).UpdateStatusAsync(1, "Shipped"));
        }

        [Fact]
        public async Task UpdateStatus_ValidTransition_ReturnsTrue()
        {
            using var ctx = CreateCtx();
            var order = new Order { Id = 1, Status = OrderStatus.Pending, UserId = 1, OrderNumber = "O1" };
            _orderRepoMock.Setup(r => r.Get(1)).ReturnsAsync(order);
            _orderRepoMock.Setup(r => r.Update(1, It.IsAny<Order>())).ReturnsAsync(order);
            Assert.True(await BuildSut(ctx).UpdateStatusAsync(1, "Shipped"));
            Assert.Equal(OrderStatus.Shipped, order.Status);
        }

        // ═══════════════════════════════════════════════
        // REQUEST RETURN
        // ═══════════════════════════════════════════════

        [Fact]
        public async Task RequestReturn_OrderNotFound_ThrowsNotFoundException()
        {
            using var ctx = CreateCtx();
            await Assert.ThrowsAsync<NotFoundException>(() =>
                BuildSut(ctx).RequestReturnAsync(1, new ReturnRequestCreateDto { OrderId = 99, Reason = "Damaged" }));
        }

        [Fact]
        public async Task RequestReturn_NotDelivered_ThrowsBusinessValidation()
        {
            using var ctx = CreateCtx();
            ctx.Orders.Add(MakeOrder(1, "O1", OrderStatus.Shipped));
            ctx.SaveChanges();
            var oid = ctx.Orders.First().Id;
            await Assert.ThrowsAsync<BusinessValidationException>(() =>
                BuildSut(ctx).RequestReturnAsync(1, new ReturnRequestCreateDto { OrderId = oid, Reason = "Late" }));
        }

        [Fact]
        public async Task RequestReturn_AlreadyRequested_ThrowsBusinessValidation()
        {
            using var ctx = CreateCtx();
            ctx.Orders.Add(MakeOrder(1, "O1", OrderStatus.Delivered));
            ctx.SaveChanges();
            var oid = ctx.Orders.First().Id;
            ctx.ReturnRequests.Add(new ReturnRequest { OrderId = oid, Status = ReturnStatus.Requested, Reason = "Damaged" });
            ctx.SaveChanges();
            await Assert.ThrowsAsync<BusinessValidationException>(() =>
                BuildSut(ctx).RequestReturnAsync(1, new ReturnRequestCreateDto { OrderId = oid, Reason = "Damage" }));
        }

        [Fact]
        public async Task RequestReturn_Success_CreatesReturnAndUpdatesOrder()
        {
            using var ctx = CreateCtx();
            ctx.Orders.Add(MakeOrder(1, "O1", OrderStatus.Delivered));
            ctx.SaveChanges();
            var order = ctx.Orders.First();
            _returnRepoMock.Setup(r => r.Add(It.IsAny<ReturnRequest>())).ReturnsAsync(new ReturnRequest());
            _orderRepoMock.Setup(r => r.Update(order.Id, It.IsAny<Order>())).ReturnsAsync(order);
            Assert.True(await BuildSut(ctx).RequestReturnAsync(1, new ReturnRequestCreateDto { OrderId = order.Id, Reason = "Damaged" }));
            _returnRepoMock.Verify(r => r.Add(It.IsAny<ReturnRequest>()), Times.Once);
        }

        // ═══════════════════════════════════════════════
        // REVIEW RETURN
        // ═══════════════════════════════════════════════

        [Fact]
        public async Task ReviewReturn_ReturnNotFound_ThrowsNotFoundException()
        {
            using var ctx = CreateCtx();
            _returnRepoMock.Setup(r => r.Get(99)).ReturnsAsync((ReturnRequest?)null);
            await Assert.ThrowsAsync<NotFoundException>(() =>
                BuildSut(ctx).ReviewReturnAsync(99, new ReturnRequestUpdateDto { Action = "approve" }));
        }

        [Fact]
        public async Task ReviewReturn_OrderNotFound_ThrowsNotFoundException()
        {
            using var ctx = CreateCtx();
            _returnRepoMock.Setup(r => r.Get(1)).ReturnsAsync(new ReturnRequest { Id = 1, OrderId = 99 });
            await Assert.ThrowsAsync<NotFoundException>(() =>
                BuildSut(ctx).ReviewReturnAsync(1, new ReturnRequestUpdateDto { Action = "approve" }));
        }

        [Fact]
        public async Task ReviewReturn_Reject_UpdatesStatusToRejected()
        {
            using var ctx = CreateCtx();
            ctx.Orders.Add(MakeOrder(1, "O1", OrderStatus.ReturnRequested));
            ctx.SaveChanges();
            var order = ctx.Orders.First();
            SeedPayment(ctx, order.Id, order.UserId, "UPI");

            var returnReq = new ReturnRequest { Id = 1, OrderId = order.Id, Status = ReturnStatus.Requested };
            _returnRepoMock.Setup(r => r.Get(1)).ReturnsAsync(returnReq);
            _returnRepoMock.Setup(r => r.Update(1, It.IsAny<ReturnRequest>())).ReturnsAsync(returnReq);
            _orderRepoMock.Setup(r => r.Update(order.Id, It.IsAny<Order>())).ReturnsAsync(order);

            var result = await BuildSut(ctx).ReviewReturnAsync(1, new ReturnRequestUpdateDto { Action = "reject", Comments = "Not valid" });
            Assert.True(result);
            Assert.Equal(ReturnStatus.Rejected, returnReq.Status);
            Assert.Equal(OrderStatus.ReturnRejected, order.Status);
        }

        [Fact]
        public async Task ReviewReturn_Approve_OnlinePayment_RefundsToWallet()
        {
            using var ctx = CreateCtx();
            ctx.Inventories.Add(new Inventory { ProductId = 1, Quantity = 5 });
            ctx.SaveChanges();
            var inv = ctx.Inventories.First();

            ctx.Orders.Add(MakeOrder(1, "O1", OrderStatus.ReturnRequested, total: 250m,
                items: new List<OrderItem> { new OrderItem { ProductId = 1, Quantity = 2, ProductName = "P", SKU = "S", UnitPrice = 125m, LineTotal = 250m } }));
            ctx.SaveChanges();
            var order = ctx.Orders.First();
            SeedPayment(ctx, order.Id, order.UserId, "UPI");

            var returnReq = new ReturnRequest { Id = 1, OrderId = order.Id, Status = ReturnStatus.Requested };
            _returnRepoMock.Setup(r => r.Get(1)).ReturnsAsync(returnReq);
            _returnRepoMock.Setup(r => r.Update(It.IsAny<int>(), It.IsAny<ReturnRequest>())).ReturnsAsync(returnReq);
            _orderRepoMock.Setup(r => r.Update(It.IsAny<int>(), It.IsAny<Order>())).ReturnsAsync(order);
            _inventoryRepoMock.Setup(r => r.Update(It.IsAny<int>(), It.IsAny<Inventory>())).ReturnsAsync(inv);
            _walletServiceMock.Setup(w => w.CreditAsync(It.IsAny<int>(), It.IsAny<decimal>(), It.IsAny<WalletTxnType>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((250m, 1));

            Assert.True(await BuildSut(ctx).ReviewReturnAsync(1, new ReturnRequestUpdateDto { Action = "approve" }));
            _walletServiceMock.Verify(w => w.CreditAsync(order.UserId, 250m, WalletTxnType.CreditRefund, It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task ReviewReturn_Approve_CODOrder_NoWalletRefund()
        {
            using var ctx = CreateCtx();
            ctx.Inventories.Add(new Inventory { ProductId = 1, Quantity = 5 });
            ctx.SaveChanges();
            var inv = ctx.Inventories.First();

            ctx.Orders.Add(MakeOrder(1, "O1", OrderStatus.ReturnRequested, total: 200m,
                items: new List<OrderItem> { new OrderItem { ProductId = 1, Quantity = 1, ProductName = "P", SKU = "S", UnitPrice = 200m, LineTotal = 200m } }));
            ctx.SaveChanges();
            var order = ctx.Orders.First();
            SeedPayment(ctx, order.Id, order.UserId, "cod");

            var returnReq = new ReturnRequest { Id = 1, OrderId = order.Id, Status = ReturnStatus.Requested };
            _returnRepoMock.Setup(r => r.Get(1)).ReturnsAsync(returnReq);
            _returnRepoMock.Setup(r => r.Update(It.IsAny<int>(), It.IsAny<ReturnRequest>())).ReturnsAsync(returnReq);
            _orderRepoMock.Setup(r => r.Update(It.IsAny<int>(), It.IsAny<Order>())).ReturnsAsync(order);
            _inventoryRepoMock.Setup(r => r.Update(It.IsAny<int>(), It.IsAny<Inventory>())).ReturnsAsync(inv);

            Assert.True(await BuildSut(ctx).ReviewReturnAsync(1, new ReturnRequestUpdateDto { Action = "approve" }));
            _walletServiceMock.Verify(w => w.CreditAsync(It.IsAny<int>(), It.IsAny<decimal>(), It.IsAny<WalletTxnType>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task ReviewReturn_Approve_InventoryRestored()
        {
            using var ctx = CreateCtx();
            ctx.Inventories.Add(new Inventory { ProductId = 1, Quantity = 5 });
            ctx.SaveChanges();
            var inv = ctx.Inventories.First();

            ctx.Orders.Add(MakeOrder(1, "O1", OrderStatus.ReturnRequested, total: 100m,
                items: new List<OrderItem> { new OrderItem { ProductId = 1, Quantity = 4, ProductName = "P", SKU = "S", UnitPrice = 25m, LineTotal = 100m } }));
            ctx.SaveChanges();
            var order = ctx.Orders.First();
            SeedPayment(ctx, order.Id, order.UserId, "UPI");

            var returnReq = new ReturnRequest { Id = 1, OrderId = order.Id, Status = ReturnStatus.Requested };
            _returnRepoMock.Setup(r => r.Get(1)).ReturnsAsync(returnReq);
            _returnRepoMock.Setup(r => r.Update(It.IsAny<int>(), It.IsAny<ReturnRequest>())).ReturnsAsync(returnReq);
            _orderRepoMock.Setup(r => r.Update(It.IsAny<int>(), It.IsAny<Order>())).ReturnsAsync(order);
            _inventoryRepoMock.Setup(r => r.Update(It.IsAny<int>(), It.IsAny<Inventory>())).ReturnsAsync(inv);
            _walletServiceMock.Setup(w => w.CreditAsync(It.IsAny<int>(), It.IsAny<decimal>(), It.IsAny<WalletTxnType>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((100m, 1));

            await BuildSut(ctx).ReviewReturnAsync(1, new ReturnRequestUpdateDto { Action = "approve" });
            _inventoryRepoMock.Verify(r => r.Update(inv.Id, It.Is<Inventory>(i => i.Quantity == 9)), Times.AtLeastOnce);
        }
    }
}
