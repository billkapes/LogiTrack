using System.Diagnostics;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace LogiTrack.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class InventoryController : ControllerBase
    {
        private readonly LogiTrackContext _context;
        private readonly IMemoryCache _cache;
        private readonly ILogger<InventoryController> _logger;
        private const string InventoryListCacheKey = "InventoryItems_All";

        public InventoryController(LogiTrackContext context, IMemoryCache cache, ILogger<InventoryController> logger)
        {
            _context = context;
            _cache = cache;
            _logger = logger;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<InventoryItem>>> GetAll()
        {
            var stopwatch = Stopwatch.StartNew();
            var cacheHit = _cache.TryGetValue(InventoryListCacheKey, out List<InventoryItem>? items);

            if (!cacheHit)
            {
                items = await _context.InventoryItems
                    .AsNoTracking()
                    .ToListAsync();

                _cache.Set(InventoryListCacheKey, items, new MemoryCacheEntryOptions
                {
                    SlidingExpiration = TimeSpan.FromMinutes(5),
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(15)
                });
            }

            stopwatch.Stop();
            var elapsedMs = stopwatch.ElapsedMilliseconds;

            Response.Headers["X-Cache-Hit"] = cacheHit.ToString();
            Response.Headers["X-Query-Duration-ms"] = elapsedMs.ToString();
            Response.Headers["X-Inventory-Count"] = items.Count.ToString();

            _logger.LogInformation("GetAll inventory {CacheStatus} in {ElapsedMilliseconds}ms with {Count} items.",
                cacheHit ? "hit" : "miss",
                elapsedMs,
                items.Count);

            return Ok(items);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<InventoryItem>> GetById(int id)
        {
            var item = await _context.InventoryItems
                .AsNoTracking()
                .FirstOrDefaultAsync(i => i.ItemId == id);

            if (item is null)
            {
                return NotFound(new { message = $"Inventory item with ID {id} was not found." });
            }

            return Ok(item);
        }

        [HttpPost]
        [Authorize(Roles = "Manager")]
        public async Task<ActionResult<InventoryItem>> Create(InventoryItem item)
        {
            _context.InventoryItems.Add(item);
            await _context.SaveChangesAsync();
            _cache.Remove(InventoryListCacheKey);

            return CreatedAtAction(nameof(GetById), new { id = item.ItemId }, item);
        }

        [HttpPut("{id}")]
        [Authorize(Roles = "Manager")]
        public async Task<IActionResult> Update(int id, InventoryItem item)
        {
            if (id != item.ItemId)
            {
                return BadRequest();
            }

            _context.Entry(item).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
                _cache.Remove(InventoryListCacheKey);
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!await _context.InventoryItems.AnyAsync(i => i.ItemId == id))
                {
                    return NotFound();
                }

                throw;
            }

            return NoContent();
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "Manager")]
        public async Task<IActionResult> Delete(int id)
        {
            var item = await _context.InventoryItems.FindAsync(id);
            if (item is null)
            {
                return NotFound();
            }

            _context.InventoryItems.Remove(item);
            await _context.SaveChangesAsync();
            _cache.Remove(InventoryListCacheKey);

            return NoContent();
        }
    }
}
