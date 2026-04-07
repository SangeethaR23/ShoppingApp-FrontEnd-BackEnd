using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ShoppingWebApi.Common;
using ShoppingWebApi.Contexts;
using ShoppingWebApi.Exceptions;
using ShoppingWebApi.Interfaces;
using ShoppingWebApi.Models;
using ShoppingWebApi.Models.DTOs.Common;
using ShoppingWebApi.Models.DTOs.Orders;
using ShoppingWebApi.Models.DTOs.Return;
using ShoppingWebApi.Models.enums; 

namespace ShoppingWebApi.Services
{
    public class OrderService : IOrderService
    {
        private readonly IRepository<int, User> _userRepo;
        private readonly IRepository<int, Address> _addressRepo;
        private readonly IRepository<int, Carts> _cartRepo;
        private readonly IRepository<int, CartItem> _cartItemRepo;
        private readonly IRepository<int, Order> _orderRepo;
        private readonly IRepository<int, OrderItem> _orderItemRepo;
        private readonly IRepository<int, Inventory> _inventoryRepo;
        private readonly IRepository<int, Payment> _paymentRepo;
        private readonly IRepository<int, Refund> _refundRepo;
        private readonly IRepository<int, ReturnRequest> _returnRepo;
        private readonly IPromoService _promoService;
        private readonly IWalletService _wallet;
        private readonly AppDbContext _db;
        private readonly ILogWriter _loggerDb;
        private readonly ILogger<OrderService> _logger;

        public OrderService(
            IRepository<int, User> userRepo,
            IRepository<int, Address> addressRepo,
            IRepository<int, Carts> cartRepo,
            IRepository<int, CartItem> cartItemRepo,
            IRepository<int, Order> orderRepo,
            IRepository<int, OrderItem> orderItemRepo,
            IRepository<int, Inventory> inventoryRepo,
            IRepository<int, Payment> paymentRepo,
            IRepository<int, Refund> refundRepo,
            IRepository<int, ReturnRequest> returnRepo,
            IPromoService promoService,
            IWalletService wallet,
            AppDbContext db,
            ILogWriter loggerDb,
            ILogger<OrderService> logger)
        {
            _userRepo = userRepo;
            _addressRepo = addressRepo;
            _cartRepo = cartRepo;
            _cartItemRepo = cartItemRepo;
            _orderRepo = orderRepo;
            _orderItemRepo = orderItemRepo;
            _inventoryRepo = inventoryRepo;
            _paymentRepo = paymentRepo;
            _refundRepo = refundRepo;
            _returnRepo = returnRepo;
            _promoService = promoService;
            _wallet = wallet;
            _db = db;
            _loggerDb = loggerDb;
            _logger = logger;
        }

        // ----------------------------------------------------------------------
        // PLACE ORDER � create Payment (Success) and sync Order.PaymentStatus
        // ----------------------------------------------------------------------
        public async Task<PlaceOrderResponseDto> PlaceOrderAsync(PlaceOrderRequestDto request, CancellationToken ct = default)
        {
            await _loggerDb.InfoAsync("OrderService.PlaceOrderAsync", "Place order started", ct: ct);

            try
            {
                // ? 1. Validate user
                var userExists = await _userRepo.GetQueryable()
                    .AsNoTracking()
                    .AnyAsync(u => u.Id == request.UserId, ct);
                if (!userExists)
                    throw new NotFoundException($"User {request.UserId} not found.");

                // ? 2. Validate address
                var address = await _addressRepo.GetQueryable()
                    .AsNoTracking()
                    .FirstOrDefaultAsync(a => a.Id == request.AddressId && a.UserId == request.UserId, ct);
                if (address == null)
                    throw new BusinessValidationException("Invalid address for this user.");

                // ? 3. Load cart + product data
                var cart = await _cartRepo.GetQueryable()
                    .Include(c => c.Items).ThenInclude(i => i.Product)
                    .FirstOrDefaultAsync(c => c.UserId == request.UserId, ct);

                if (cart == null || cart.Items.Count==0)
                {
                    throw new BusinessValidationException("Cart is empty.");
                    //throw new BusinessValidationException("CartItmes shoud not be more than 10");
                }
                //if(cart.Items.Count>10)
                //{
                //    throw new BusinessValidationException("CartItmes shoud not be more than 10");
                //}

                var lines = cart.Items.Select(i => new
                {
                    i.ProductId,
                    i.Quantity,
                    UnitPrice = i.UnitPrice > 0 ? i.UnitPrice : (i.Product?.Price ?? 0m),
                    ProductName = i.Product?.Name ?? "",
                    SKU = i.Product?.SKU ?? ""
                }).ToList();

                if (lines.Any(l => l.UnitPrice <= 0))
                    throw new BusinessValidationException("One or more products have invalid unit price.");

                // ? 4. Inventory pre-check
                var productIds = lines.Select(l => l.ProductId).Distinct().ToList();
                var invMap = await _inventoryRepo.GetQueryable()
                    .Where(inv => productIds.Contains(inv.ProductId))
                    .ToDictionaryAsync(inv => inv.ProductId, ct);

                foreach (var l in lines)
                {
                    if (!invMap.TryGetValue(l.ProductId, out var inv))
                        throw new BusinessValidationException($"Inventory missing for product {l.ProductId}.");

                    if (inv.Quantity < l.Quantity)
                        throw new BusinessValidationException($"Insufficient inventory for product {l.ProductId}. Available: {inv.Quantity}, Requested: {l.Quantity}");
                }

                // ✅ 4b. Per-order limit: max 3 of same item | Monthly limit: max 5 of same item
                var monthStart = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1, 0, 0, 0, DateTimeKind.Utc);

                var monthlyQtyMap = await _orderItemRepo.GetQueryable()
                    .Where(oi => productIds.Contains(oi.ProductId)
                              && oi.Order.UserId == request.UserId
                              && oi.Order.PlacedAtUtc >= monthStart
                              && oi.Order.Status != OrderStatus.Cancelled)
                    .GroupBy(oi => oi.ProductId)
                    .Select(g => new { ProductId = g.Key, TotalQty = g.Sum(x => x.Quantity) })
                    .ToDictionaryAsync(x => x.ProductId, x => x.TotalQty, ct);

                foreach (var l in lines)
                {

                    var alreadyThisMonth = monthlyQtyMap.TryGetValue(l.ProductId, out var mq) ? mq : 0;
                    if (alreadyThisMonth + l.Quantity > 3)
                        throw new BusinessValidationException(
                            $"You cannot order more than 3 units of '{l.ProductName}' in a month. Already ordered: {alreadyThisMonth}/3.");
                }

                // ✅ 5. SubTotal, Shipping, Discount
                var subTotal = lines.Sum(l => l.UnitPrice * l.Quantity);
                var shipping = request.ShippingFee ?? 0m;
                var discount = request.Discount ?? 0m;

                var total = subTotal + shipping - discount;
                if (total < 0) total = 0;

                // ? 6. Promo Code (Optional)
                if (!string.IsNullOrWhiteSpace(request.PromoCode))
                {
                    var promo = await _promoService.GetValidPromoAsync(request.PromoCode.ToUpper(), total, ct);

                    if (promo != null)
                    {
                        total -= promo.DiscountAmount;
                        if (total < 0) total = 0;

                        await _loggerDb.InfoAsync("OrderService.PlaceOrderAsync",
                            $"Promo applied: {promo.Code} discount {promo.DiscountAmount}", ct: ct);
                    }
                }

                // ? 7. Wallet usage (AFTER promo)
                decimal walletUsed = 0m;

                if (request.WalletUseAmount > 0)
                {
                    var wallet = await _wallet.GetAsync(request.UserId, ct);

                    if (wallet != null && wallet.Balance > 0)
                    {
                        var allowed = Math.Min(request.WalletUseAmount,
                                       Math.Min(wallet.Balance, total));

                        if (allowed > 0)
                        {
                            await _wallet.DebitAsync(
                                request.UserId,
                                allowed,
                                WalletTxnType.DebitOrder,
                                reference: "Order checkout",
                                remarks: "Wallet used for order",
                                ct: ct);

                            walletUsed = allowed;
                            total -= walletUsed;
                        }
                    }
                }

                // ? 8�13. Persist order, items, inventory, cart clear, payment � all atomic
                await using var tx = await _db.Database.BeginTransactionSafeAsync(ct);
                Order addedOrder;
                try
                {
                    // ? 8. Create ORDER entity
                    var order = new Order
                    {
                        UserId = request.UserId,
                        OrderNumber = await GenerateUniqueOrderNumberAsync(ct),
                        Status = OrderStatus.Pending,
                        PaymentStatus = PaymentStatus.Pending,
                        PlacedAtUtc = DateTime.UtcNow,

                        ShipToName = address.FullName,
                        ShipToPhone = address.Phone,
                        ShipToLine1 = address.Line1,
                        ShipToLine2 = address.Line2,
                        ShipToCity = address.City,
                        ShipToState = address.State,
                        ShipToPostalCode = address.PostalCode,
                        ShipToCountry = address.Country,

                        SubTotal = subTotal,
                        ShippingFee = shipping,
                        Discount = discount,
                        Total = total,
                        WalletUsed = walletUsed
                    };

                    // ? 9. Save the order
                    addedOrder = await _orderRepo.Add(order);

                    // ? 10. Create order items + decrement inventory
                    foreach (var l in lines)
                    {
                        var inv = invMap[l.ProductId];
                        inv.Quantity -= l.Quantity;
                        inv.UpdatedUtc = DateTime.UtcNow;
                        await _inventoryRepo.Update(inv.Id, inv);

                        await _orderItemRepo.Add(new OrderItem
                        {
                            OrderId = addedOrder.Id,
                            ProductId = l.ProductId,
                            ProductName = l.ProductName,
                            SKU = l.SKU,
                            UnitPrice = l.UnitPrice,
                            Quantity = l.Quantity,
                            LineTotal = l.UnitPrice * l.Quantity
                        });
                    }

                    // ? 11. Clear cart
                    foreach (var ci in cart.Items.ToList())
                        await _cartItemRepo.Delete(ci.Id);

                    // ? 12. Create Payment entry
                    var payment = new Payment
                    {
                        OrderId = addedOrder.Id,
                        UserId = request.UserId,
                        TotalAmount = total,
                        PaymentType = request.PaymentType.ToString(),
                        CreatedAt = DateTime.UtcNow
                    };
                    await _paymentRepo.Add(payment);

                    // ? 13. Update order payment status
                    addedOrder.PaymentStatus = payment.PaymentType.ToLower() == "cod"
                        ? PaymentStatus.Pending
                        : PaymentStatus.Paid;
                    addedOrder.UpdatedUtc = DateTime.UtcNow;
                    await _orderRepo.Update(addedOrder.Id, addedOrder);

                    await tx.CommitAsync(ct);
                }
                catch
                {
                    await tx.RollbackAsync(ct);
                    throw;
                }

                // ? Success log
                await _loggerDb.InfoAsync("OrderService.PlaceOrderAsync", "Place order success", ct: ct);
                _logger.LogInformation("Order placed. OrderId={OrderId}, UserId={UserId}, Total={Total}",
                    addedOrder.Id, request.UserId, total);

                // ? 14. Return DTO
                return new PlaceOrderResponseDto
                {
                    Id = addedOrder.Id,
                    OrderNumber = addedOrder.OrderNumber,
                    Total = addedOrder.Total,
                    Status = addedOrder.Status.ToString(),
                    PaymentStatus = addedOrder.PaymentStatus.ToString(),
                    PlacedAtUtc = addedOrder.PlacedAtUtc
                };
            }
            catch (Exception ex)
            {
                await _loggerDb.ErrorAsync("OrderService.PlaceOrderAsync", "Place order failed", ex, ct: ct);
                _logger.LogError(ex, "Failed to place order for user {UserId}", request.UserId);
                throw;
            }
        } 

        // ----------------------------------------------------------------------
        // GET ORDER BY ID (Include Items) � NO payment in DTO
        // ----------------------------------------------------------------------
        public async Task<OrderReadDto?> GetByIdAsync(int orderId, CancellationToken ct = default)
        {
            await _loggerDb.InfoAsync("OrderService.GetByIdAsync", "Get order by id", ct: ct);

            var order = await _orderRepo.GetQueryable()
                .AsNoTracking()
                .Include(o => o.Items)
                .FirstOrDefaultAsync(o => o.Id == orderId, ct);

            if (order == null) return null;

            return new OrderReadDto
            {
                Id = order.Id,
                OrderNumber = order.OrderNumber,
                Status = order.Status.ToString(),
                PaymentStatus = order.PaymentStatus.ToString(),
                PlacedAtUtc = order.PlacedAtUtc,

                ShipToName = order.ShipToName,
                ShipToPhone = order.ShipToPhone,
                ShipToLine1 = order.ShipToLine1,
                ShipToLine2 = order.ShipToLine2,
                ShipToCity = order.ShipToCity,
                ShipToState = order.ShipToState,
                ShipToPostalCode = order.ShipToPostalCode,
                ShipToCountry = order.ShipToCountry,

                SubTotal = order.SubTotal,
                ShippingFee = order.ShippingFee,
                Discount = order.Discount,
                Total = order.Total,

                Items = order.Items.Select(oi => new OrderDetailDto
                {
                    Id = oi.Id,
                    ProductId = oi.ProductId,
                    ProductName = oi.ProductName,
                    SKU = oi.SKU,
                    UnitPrice = oi.UnitPrice,
                    Quantity = oi.Quantity,
                    LineTotal = oi.LineTotal
                }).ToList()
            };
        }

        // ----------------------------------------------------------------------
        // USER ORDERS (paged) � NO payment in DTO
        // ----------------------------------------------------------------------
        public async Task<PagedResult<OrderSummaryDto>> GetUserOrdersAsync(
            int userId, int page = 1, int size = 10, string? sortBy = "date", bool desc = true,
            string? status = null, DateTime? from = null, DateTime? to = null,
            CancellationToken ct = default)
        {
            await _loggerDb.InfoAsync("OrderService.GetUserOrdersAsync", "List user orders", ct: ct);

            page = page < 1 ? 1 : page;
            size = size < 1 ? 10 : size;

            var q = _orderRepo.GetQueryable()
                .AsNoTracking()
                .Where(o => o.UserId == userId);

            if (!string.IsNullOrWhiteSpace(status) &&
                Enum.TryParse<ShoppingWebApi.Models.enums.OrderStatus>(status, true, out var parsedStatus))
                q = q.Where(o => o.Status == parsedStatus);

            if (from.HasValue) q = q.Where(o => o.PlacedAtUtc >= from.Value);
            if (to.HasValue)   q = q.Where(o => o.PlacedAtUtc <= to.Value.AddDays(1));

            q = (sortBy?.ToLowerInvariant()) switch
            {
                "total" => desc ? q.OrderByDescending(o => o.Total) : q.OrderBy(o => o.Total),
                "status" => desc ? q.OrderByDescending(o => o.Status) : q.OrderBy(o => o.Status),
                _ => desc ? q.OrderByDescending(o => o.PlacedAtUtc) : q.OrderBy(o => o.PlacedAtUtc),
            };

            var totalCount = await q.CountAsync(ct);

            var rows = await q
                .Skip((page - 1) * size)
                .Take(size)
                .Select(o => new
                {
                    o.Id,
                    o.OrderNumber,
                    o.Status,
                    o.PlacedAtUtc,
                    o.Total,
                    ItemsCount = o.Items.Count
                })
                .ToListAsync(ct);

            var items = rows.Select(r => new OrderSummaryDto
            {
                Id = r.Id,
                OrderNumber = r.OrderNumber,
                Status = r.Status.ToString(),
                PlacedAtUtc = r.PlacedAtUtc,
                Total = r.Total,
                ItemsCount = r.ItemsCount
            }).ToList();

            return new PagedResult<OrderSummaryDto>
            {
                Items = items,
                TotalCount = totalCount,
                PageNumber = page,
                PageSize = size
            };
        }

        // ----------------------------------------------------------------------
        // CANCEL ORDER � restore inventory + create Refund row (Initiated semantics via row only)
        // ----------------------------------------------------------------------
        public async Task<CancelOrderResponseDto> CancelOrderAsync(
            int orderId,
            int userId,
            bool isAdmin = false,
            string? reason = null,
            CancellationToken ct = default)
        {
            await _loggerDb.InfoAsync("OrderService.CancelOrderAsync", "Cancel order started", ct: ct);

            try
            {
                var order = await _orderRepo.GetQueryable()
                    .Include(o => o.Items)
                    .Include(o => o.Payment)
                    .FirstOrDefaultAsync(o => o.Id == orderId, ct);

                if (order == null)
                    throw new NotFoundException("Order not found.");

                if (!isAdmin && order.UserId != userId)
                    throw new ForbiddenException("You cannot cancel another user's order.");

                if (!isAdmin)
                {
                    var daysSincePlaced = (DateTime.UtcNow - order.PlacedAtUtc).TotalDays;
                    if (daysSincePlaced > 1)
                        throw new BusinessValidationException("Orders can only be cancelled within 1 days of placement.");
                }

                if (order.Status == OrderStatus.Shipped || order.Status == OrderStatus.Delivered)
                    throw new BusinessValidationException($"Order already {order.Status}, cannot cancel.");

                if (order.Status == OrderStatus.Cancelled)
                {
                    return new CancelOrderResponseDto
                    {
                        Id = order.Id,
                        Status = order.Status.ToString(),
                        Message = "Order already cancelled."
                    };
                }

                // ? Restore inventory, update order, insert refund � all atomic
                var productIds = order.Items.Select(i => i.ProductId).Distinct().ToList();
                var invMap = await _inventoryRepo.GetQueryable()
                    .Where(inv => productIds.Contains(inv.ProductId))
                    .ToDictionaryAsync(inv => inv.ProductId, ct);

                await using var tx = await _db.Database.BeginTransactionSafeAsync(ct);
                try
                {
                    foreach (var it in order.Items)
                    {
                        if (invMap.TryGetValue(it.ProductId, out var inv))
                        {
                            inv.Quantity += it.Quantity;
                            inv.UpdatedUtc = DateTime.UtcNow;
                            await _inventoryRepo.Update(inv.Id, inv);
                        }
                    }

                    order.Status = OrderStatus.Cancelled;
                    order.PaymentStatus = PaymentStatus.Pending;
                    order.UpdatedUtc = DateTime.UtcNow;
                    await _orderRepo.Update(order.Id, order);

                    if (order.Payment != null)
                    {
                        await _refundRepo.Add(new Refund
                        {
                            PaymentId = order.Payment.PaymentId,
                            OrderId = order.Id,
                            UserId = order.UserId,
                            RefundAmount = order.Total,
                            CreatedAt = DateTime.UtcNow
                        });
                    }

                    await tx.CommitAsync(ct);
                }
                catch
                {
                    await tx.RollbackAsync(ct);
                    throw;
                }

                // ? Wallet Refund Logic
                bool isCod = string.Equals(order.Payment?.PaymentType, "cod", StringComparison.OrdinalIgnoreCase);

                if (isCod)
                {
                    // ? COD ? user didn't pay online ? DO NOT credit order.Total
                    // ? But if wallet was used earlier, refund THAT amount.
                    if (order.WalletUsed > 0)
                    {
                        await _wallet.CreditAsync(
                            order.UserId,
                            order.WalletUsed,
                            WalletTxnType.CreditRefund,
                            reference: $"Order:{order.Id}",
                            remarks: "Cancellation - refund walletUsed (COD)",
                            ct: ct
                        );

                        await _loggerDb.InfoAsync("OrderService.CancelOrderAsync",
                            $"Wallet credited {order.WalletUsed} for COD cancellation Order {order.Id}", ct: ct);
                    }
                }
                else
                {
                    // ? ONLINE PAYMENT ? refund full total to wallet
                    if (order.Total > 0)
                    {
                        await _wallet.CreditAsync(
                            order.UserId,
                            order.Total,
                            WalletTxnType.CreditRefund,
                            reference: $"Order:{order.Id}",
                            remarks: "Cancellation - online refund to wallet",
                            ct: ct
                        );

                        await _loggerDb.InfoAsync("OrderService.CancelOrderAsync",
                            $"Wallet credited {order.Total} for ONLINE cancellation Order {order.Id}", ct: ct);
                    }
                }

                await _loggerDb.InfoAsync("OrderService.CancelOrderAsync", "Cancel order success", ct: ct);

                return new CancelOrderResponseDto
                {
                    Id = order.Id,
                    Status = order.Status.ToString(),
                    Message = "Order cancelled. Refund processed."
                };
            }
            catch (Exception ex)
            {
                await _loggerDb.ErrorAsync("OrderService.CancelOrderAsync", "Cancel order failed", ex, ct: ct);
                _logger.LogError(ex, "Cancel order failed. OrderId={OrderId}", orderId);
                throw;
            }
        }

        // ----------------------------------------------------------------------
        // ADMIN GET ALL (paged + filters) � NO payment in DTO
        // ----------------------------------------------------------------------
        public async Task<PagedResult<OrderReadDto>> GetAllAsync(
            string? status = null, DateTime? from = null, DateTime? to = null, int? userId = null,
            int page = 1, int size = 10, string? sortBy = "date", bool desc = true, CancellationToken ct = default)
        {
            await _loggerDb.InfoAsync("OrderService.GetAllAsync", "Admin orders query", ct: ct);

            page = page < 1 ? 1 : page;
            size = page < 1 ? 10 : size;

            var q = _orderRepo.GetQueryable().AsNoTracking();

            if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<OrderStatus>(status, true, out var st))
                q = q.Where(o => o.Status == st);
            if (userId.HasValue) q = q.Where(o => o.UserId == userId.Value);
            if (from.HasValue) q = q.Where(o => o.PlacedAtUtc >= from.Value);
            if (to.HasValue) q = q.Where(o => o.PlacedAtUtc <= to.Value);

            q = (sortBy?.ToLowerInvariant()) switch
            {
                "total" => desc ? q.OrderByDescending(o => o.Total) : q.OrderBy(o => o.Total),
                "status" => desc ? q.OrderByDescending(o => o.Status) : q.OrderBy(o => o.Status),
                _ => desc ? q.OrderByDescending(o => o.PlacedAtUtc) : q.OrderBy(o => o.PlacedAtUtc),
            };

            var totalCount = await q.CountAsync(ct);

            var orders = await q
                .Skip((page - 1) * size)
                .Take(size)
                .Include(o => o.Items)
                .ToListAsync(ct);

            var items = orders.Select(order => new OrderReadDto
            {
                Id = order.Id,
                OrderNumber = order.OrderNumber,
                Status = order.Status.ToString(),
                PaymentStatus = order.PaymentStatus.ToString(),
                PlacedAtUtc = order.PlacedAtUtc,

                ShipToName = order.ShipToName,
                ShipToPhone = order.ShipToPhone,
                ShipToLine1 = order.ShipToLine1,
                ShipToLine2 = order.ShipToLine2,
                ShipToCity = order.ShipToCity,
                ShipToState = order.ShipToState,
                ShipToPostalCode = order.ShipToPostalCode,
                ShipToCountry = order.ShipToCountry,

                SubTotal = order.SubTotal,
                ShippingFee = order.ShippingFee,
                Discount = order.Discount,
                Total = order.Total,

                Items = order.Items.Select(oi => new OrderDetailDto
                {
                    Id = oi.Id,
                    ProductId = oi.ProductId,
                    ProductName = oi.ProductName,
                    SKU = oi.SKU,
                    UnitPrice = oi.UnitPrice,
                    Quantity = oi.Quantity,
                    LineTotal = oi.LineTotal
                }).ToList()
            }).ToList();

            return new PagedResult<OrderReadDto>
            {
                Items = items,
                TotalCount = totalCount,
                PageNumber = page,
                PageSize = size
            };
        }

        // ----------------------------------------------------------------------
        // UPDATE STATUS (simple) � if Cancelled, reuse cancel flow
        // ----------------------------------------------------------------------
        public async Task<bool> UpdateStatusAsync(int orderId, string newStatus, CancellationToken ct = default)
        {
            await _loggerDb.InfoAsync("OrderService.UpdateStatusAsync", "Update order status", ct: ct);

            if (!Enum.TryParse<OrderStatus>(newStatus, true, out var parsed))
                throw new BusinessValidationException("Invalid order status value.");

            if (parsed == OrderStatus.Cancelled)
            {
                await CancelOrderAsync(orderId, userId: 0, isAdmin: true, reason: "Admin status update to Cancelled", ct: ct);
                return true;
            }

            var order = await _orderRepo.Get(orderId);
            if (order == null) return false;

            if (order.Status == OrderStatus.Cancelled)
                throw new ConflictException("Cancelled orders cannot change status.");

            order.Status = parsed;
            order.UpdatedUtc = DateTime.UtcNow;
            await _orderRepo.Update(orderId, order);

            await _loggerDb.InfoAsync("OrderService.UpdateStatusAsync", "Update order status success", ct: ct);
            return true;
        }

        // ----------------- helpers -----------------

        private async Task<string> GenerateUniqueOrderNumberAsync(CancellationToken ct)
        {
            string ord;
            do
            {
                var token = Convert.ToHexString(Guid.NewGuid().ToByteArray()).Substring(0, 8);
                ord = $"ORD-{DateTime.UtcNow:yyyyMMdd}-{token}";
            }
            while (await _orderRepo.GetQueryable()
                .AsNoTracking()
                .AnyAsync(o => o.OrderNumber == ord, ct));

            return ord;
        }
        //RequestReturnMethod

        public async Task<bool> RequestReturnAsync(int userId,ReturnRequestCreateDto dto, CancellationToken ct=default)
        {
            await _loggerDb.InfoAsync("OrderService.RequestReturnAsync", "Return request started", ct: ct);
            var order=await _orderRepo.GetQueryable()
                .Include(o=>o.Payment)
                .AsNoTracking()
                .FirstOrDefaultAsync(o=>o.Id==dto.OrderId && o.UserId==userId, ct);
            if (order == null)
                throw new NotFoundException("Order not found.");
            if (order.Status != OrderStatus.Delivered)
                throw new BusinessValidationException("Return allowed only after delivery.");
            bool alreadyRequested=await _returnRepo.GetQueryable()
                .AnyAsync(r=>r.OrderId==dto.OrderId && r.Status==ReturnStatus.Requested, ct);
            if (alreadyRequested)
                throw new BusinessValidationException("Return already requested.");
            var req = new ReturnRequest
            { 
              OrderId = dto.OrderId,
              Reason= dto.Reason,
              Status= ReturnStatus.Requested
            };

            await using var tx = await _db.Database.BeginTransactionSafeAsync(ct);
            try
            {
                await _returnRepo.Add(req);

                order.Status = OrderStatus.ReturnRequested;
                order.UpdatedUtc = DateTime.UtcNow;
                await _orderRepo.Update(order.Id, order);

                await tx.CommitAsync(ct);
            }
            catch
            {
                await tx.RollbackAsync(ct);
                throw;
            }

            await _loggerDb.InfoAsync("OrderService.RequestReturnAsync", "Return request submitted", ct: ct);

            return true;

        }

        public async Task<bool> ReviewReturnAsync(int returnId, ReturnRequestUpdateDto dto, CancellationToken ct = default)
        {
            await _loggerDb.InfoAsync("OrderService.ReviewReturnAsync", "Admin reviewing return request", ct: ct);

            var req = await _returnRepo.Get(returnId);
            if (req == null)
                throw new NotFoundException("Return request not found.");

            var order = await _orderRepo.GetQueryable()
                .Include(o => o.Payment)
                .Include(o => o.Items)
                .FirstOrDefaultAsync(o => o.Id == req.OrderId, ct);

            if (order == null)
                throw new NotFoundException("Order not found.");

            // ? REJECT FLOW
            if (dto.Action.Equals("reject", StringComparison.OrdinalIgnoreCase))
            {
                req.Status = ReturnStatus.Rejected;
                req.Comments = dto.Comments;
                req.ReviewedAtUtc = DateTime.UtcNow;

                order.Status = OrderStatus.ReturnRejected;
                order.UpdatedUtc = DateTime.UtcNow;

                await using var rejectTx = await _db.Database.BeginTransactionSafeAsync(ct);
                try
                {
                    await _returnRepo.Update(returnId, req);
                    await _orderRepo.Update(order.Id, order);
                    await rejectTx.CommitAsync(ct);
                }
                catch { await rejectTx.RollbackAsync(ct); throw; }

                await _loggerDb.InfoAsync("OrderService.ReviewReturnAsync",
                    $"Return rejected for order {order.Id}", ct: ct);

                return true;
            }

            // ? APPROVE RETURN
            req.Status = ReturnStatus.Approved;
            req.Comments = dto.Comments;
            req.ReviewedAtUtc = DateTime.UtcNow;

            order.Status = OrderStatus.ReturnApproved;
            order.UpdatedUtc = DateTime.UtcNow;

            // ? 1. Load inventory before transaction
            var productIds = order.Items.Select(i => i.ProductId).Distinct().ToList();
            var invMap = await _inventoryRepo.GetQueryable()
                .Where(i => productIds.Contains(i.ProductId))
                .ToDictionaryAsync(inv => inv.ProductId, ct);

            // ? 2. Check payment type
            bool isCod = string.Equals(order.Payment?.PaymentType, "cod", StringComparison.OrdinalIgnoreCase);

            await using var approveTx = await _db.Database.BeginTransactionSafeAsync(ct);
            try
            {
                await _returnRepo.Update(returnId, req);
                await _orderRepo.Update(order.Id, order);

                // ? Restore Inventory
                foreach (var item in order.Items)
                {
                    if (invMap.TryGetValue(item.ProductId, out var inv))
                    {
                        inv.Quantity += item.Quantity;
                        inv.UpdatedUtc = DateTime.UtcNow;
                        await _inventoryRepo.Update(inv.Id, inv);
                    }
                }

                // ? COD Return Flow � NO REFUND
                if (isCod)
                {
                    order.Status = OrderStatus.Returned;
                    order.UpdatedUtc = DateTime.UtcNow;
                    await _orderRepo.Update(order.Id, order);

                    req.Status = ReturnStatus.Completed;
                    await _returnRepo.Update(returnId, req);

                    await approveTx.CommitAsync(ct);

                    await _loggerDb.InfoAsync("OrderService.ReviewReturnAsync",
                        $"Return approved for COD order {order.Id}. No refund required.", ct: ct);

                    return true;
                }

                // ? ONLINE PAYMENT RETURN ? REFUND FULL ORDER TOTAL TO WALLET
                decimal refundAmount = order.Total;
                if (refundAmount > 0)
                {
                    await _wallet.CreditAsync(
                        order.UserId,
                        refundAmount,
                        WalletTxnType.CreditRefund,
                        reference: $"Order:{order.Id}",
                        remarks: "Return approved - refund to wallet",
                        ct: ct);

                    await _loggerDb.InfoAsync("OrderService.ReviewReturnAsync",
                        $"Refund {refundAmount} credited to wallet for online return Order {order.Id}", ct: ct);
                }

                // ? Complete return
                req.Status = ReturnStatus.Completed;
                await _returnRepo.Update(returnId, req);

                order.Status = OrderStatus.Returned;
                order.UpdatedUtc = DateTime.UtcNow;
                await _orderRepo.Update(order.Id, order);

                await approveTx.CommitAsync(ct);
            }
            catch { await approveTx.RollbackAsync(ct); throw; }
            await _orderRepo.Update(order.Id, order);

            return true;
        }


    }
}
