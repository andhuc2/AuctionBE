using API.Models;
using API.Models.Context;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NET_base.Models.Common;
using System.Linq.Dynamic.Core;

namespace API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class BidController : ControllerBase
    {
        private readonly DBContext _context;

        public BidController(DBContext context)
        {
            _context = context;
        }

        // GET: api/Bid
        [HttpGet]
        public async Task<Response<PagedResult<Bid>>> GetBids([FromQuery] int page = 1, [FromQuery] int size = 10)
        {
            var bidsQuery = _context.Bids.Include(b => b.Bidder);

            var totalItems = await bidsQuery.CountAsync();
            var pageCount = (int)Math.Ceiling((double)totalItems / size);

            var bids = await bidsQuery
                .Skip((page - 1) * size)
                .Take(size)
                .ToListAsync();

            return new Response<PagedResult<Bid>>(
                true,
                "Bids fetched successfully.",
                new PagedResult<Bid>
                {
                    Queryable = bids.AsQueryable(),
                    RowCount = totalItems,
                    CurrentPage = page,
                    PageSize = size,
                    PageCount = pageCount
                });
        }

        // GET: api/Bid/{id}
        [HttpGet("{id}")]
        public async Task<Response<Bid>> GetBidById(int id)
        {
            var bid = await _context.Bids.Include(b => b.Bidder).Include(b => b.Item).FirstOrDefaultAsync(b => b.Id == id);

            if (bid == null)
            {
                return new Response<Bid>(false, "Bid not found", null);
            }

            return new Response<Bid>(true, "Bid fetched successfully.", bid);
        }

        // GET: api/Bid/item/{itemId}
        [HttpGet("item/{itemId}")]
        public async Task<Response<PagedResult<Bid>>> GetBidsByItem(int itemId, [FromQuery] int page = 1, [FromQuery] int size = 10)
        {
            var bidsQuery = _context.Bids.Where(b => b.ItemId == itemId).Include(b => b.Bidder).Include(b => b.Item);

            var totalItems = await bidsQuery.CountAsync();
            var pageCount = (int)Math.Ceiling((double)totalItems / size);

            var bids = await bidsQuery
                .OrderByDescending(b => b.BidAmount)
                .Skip((page - 1) * size)
                .Take(size)
                .ToListAsync();

            return new Response<PagedResult<Bid>>(
                true,
                "Bids for the item fetched successfully.",
                new PagedResult<Bid>
                {
                    Queryable = bids.AsQueryable(),
                    RowCount = totalItems,
                    CurrentPage = page,
                    PageSize = size,
                    PageCount = pageCount
                });
        }

        // POST: api/Bid
        [Authorize]
        [HttpPost]
        public async Task<Response<bool>> PlaceBid(Bid newBid)
        {
            try
            {
                int userId = JwtMiddleware.GetUserId(HttpContext);

                // Ensure bidder ID is set
                newBid.BidderId = userId;

                var item = await _context.Items.FindAsync(newBid.ItemId);
                if (item == null)
                {
                    return new Response<bool>(false, "Item not found", false);
                }

                if (item.SellerId == userId)
                {
                    return new Response<bool>(false, "Cant bid your own item!", false);
                }

                var user = await _context.Users.Where(u => u.Id == userId).FirstOrDefaultAsync();
                if (user==null || user.Credit < 1000)
                {
                    return new Response<bool>(false, "Insufficient Credits: You need at least 1 credit to place bid. Please recharge your credits.", false);
                }

                var currentTime = DateTime.Now;
                if (item.BidStartDate.HasValue && currentTime < item.BidStartDate.Value)
                {
                    return new Response<bool>(false, "Bidding for this item has not started yet.", false);
                }

                if (item.BidEndDate.HasValue && currentTime > item.BidEndDate.Value)
                {
                    return new Response<bool>(false, "Bidding for this item has ended.", false);
                }

                if (newBid.BidAmount < item.MinimumBid)
                {
                    return new Response<bool>(false, "Bid amount is less than the minimum bid", false);
                }

                var highestBid = await _context.Bids
                    .Where(b => b.ItemId == newBid.ItemId)
                    .OrderByDescending(b => b.BidAmount)
                    .FirstOrDefaultAsync();

                if (highestBid != null && newBid.BidAmount <= highestBid.BidAmount)
                {
                    return new Response<bool>(false, $"Bid amount must be higher than the current highest bid of ${highestBid.BidAmount}.", false);
                }

                if (highestBid != null && newBid.BidAmount < highestBid.BidAmount + item.BidIncrement)
                {
                    return new Response<bool>(false, $"Bid amount must be higher with increment amount: ${item.BidIncrement}.", false);
                }

                // Add the bid
                await _context.Bids.AddAsync(newBid);
                user.Credit -= 2000;
                await _context.SaveChangesAsync();

                return new Response<bool>(true, "Bid placed successfully.", true);
            }
            catch (Exception ex)
            {
                return new Response<bool>(false, $"Error placing bid: {ex.Message}", false);
            }
        }

        // DELETE: api/Bid/{id}
        [Authorize]
        [HttpDelete("{id}")]
        public async Task<Response<bool>> DeleteBid(int id)
        {
            var bid = await _context.Bids.FindAsync(id);
            if (bid == null)
            {
                return new Response<bool>(false, "Bid not found", false);
            }

            try
            {
                // Ensure only the owner or an admin can delete the bid
                int userId = JwtMiddleware.GetUserId(HttpContext);
                if (bid.BidderId != userId)
                {
                    return new Response<bool>(false, "Unauthorized to delete this bid", false);
                }

                _context.Bids.Remove(bid);
                await _context.SaveChangesAsync();

                return new Response<bool>(true, "Bid deleted successfully.", true);
            }
            catch (Exception ex)
            {
                return new Response<bool>(false, $"Error deleting bid: {ex.Message}", false);
            }
        }
    }
}
