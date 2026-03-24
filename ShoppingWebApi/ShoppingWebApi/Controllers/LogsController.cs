using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ShoppingWebApi.Contexts;
using ShoppingWebApi.Models.DTOs.AdminLogs;
using ShoppingWebApi.Models.DTOs.Common;

namespace ShoppingWebApi.Controllers
{

    [ApiController]
    [Route("api/admin/logs")]
    [Authorize(Policy = "AdminOnly")]
    public class LogsController : ControllerBase
    {
        private readonly AppDbContext _db;
        private readonly IMapper _mapper;

        public LogsController(AppDbContext db, IMapper mapper)
        {
            _db = db;
            _mapper = mapper;
        }

        [HttpPost("search")]
        public async Task<ActionResult<PagedResult<LogEntryReadDto>>> SearchLogs(
            [FromBody] LogQueryDto query,
            CancellationToken ct = default)
        {
            var logs = _db.Logs.AsNoTracking().AsQueryable();

            // ✅ LEVEL FILTER
            if (!string.IsNullOrWhiteSpace(query.Level))
                logs = logs.Where(l => l.Level == query.Level);

            // ✅ SOURCE FILTER
            if (!string.IsNullOrWhiteSpace(query.Source))
                logs = logs.Where(l => l.Source == query.Source);

            // ✅ KEYWORD SEARCH (Message + Exception)
            if (!string.IsNullOrWhiteSpace(query.Search))
            {
                var keyword = query.Search.ToLower();
                logs = logs.Where(l =>
                    l.Message.ToLower().Contains(keyword) ||
                    (l.Exception != null && l.Exception.ToLower().Contains(keyword)));
            }

            // ✅ DATE RANGE
            if (query.From.HasValue)
                logs = logs.Where(l => l.CreatedUtc >= query.From.Value);

            if (query.To.HasValue)
                logs = logs.Where(l => l.CreatedUtc <= query.To.Value);

            // ✅ SORTING
            bool desc = query.SortDir?.ToLower() == "desc";

            logs = query.SortBy?.ToLower() switch
            {
                "level" => desc ? logs.OrderByDescending(l => l.Level) : logs.OrderBy(l => l.Level),
                "source" => desc ? logs.OrderByDescending(l => l.Source) : logs.OrderBy(l => l.Source),
                _ => desc ? logs.OrderByDescending(l => l.CreatedUtc) : logs.OrderBy(l => l.CreatedUtc),
            };

            // ✅ PAGINATION
            int total = await logs.CountAsync(ct);

            var items = await logs
                .Skip((query.Page - 1) * query.Size)
                .Take(query.Size)
                .ToListAsync(ct);
            return Ok(new PagedResult<LogEntryReadDto>
            {
                PageNumber = query.Page,
                PageSize = query.Size,
                TotalCount = total,
                Items = _mapper.Map<List<LogEntryReadDto>>(items) 
            });

        }
    }

}
