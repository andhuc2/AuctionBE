using API.Models;
using API.Models.Context;
using Microsoft.AspNetCore.Mvc;
using NET_base.Models.Common;
using Microsoft.EntityFrameworkCore;
using System.Linq.Dynamic.Core;
using Microsoft.AspNetCore.Authorization;

namespace API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ItemController : ControllerBase
    {
        private readonly DBContext _context;

        public ItemController(DBContext context)
        {
            _context = context;
        }

        // GET: api/Item
        [HttpGet]
        public async Task<Response<PagedResult<Item>>> GetItems([FromQuery] int page = 1, [FromQuery] int size = 10)
        {
            var itemsQuery = _context.Items;

            var totalItems = await itemsQuery.CountAsync();
            var pageCount = (int)Math.Ceiling((double)totalItems / size);
            var items = await itemsQuery
                .Skip((page - 1) * size)
                .Take(size)
                .ToListAsync();

            return new Response<PagedResult<Item>>(
                true,
                "Items fetched successfully.",
                new PagedResult<Item>
                {
                    Queryable = items.AsQueryable(),
                    RowCount = totalItems,
                    CurrentPage = page,
                    PageSize = size,
                    PageCount = pageCount
                });
        }

        // GET: api/Item/person
        [HttpGet("person")]
        [Authorize]
        public async Task<Response<PagedResult<Item>>> GetItemsPerson([FromQuery] int page = 1, [FromQuery] int size = 10)
        {
            int userId = JwtMiddleware.GetUserId(HttpContext);

            var itemsQuery = _context.Items.Where(i => i.SellerId == userId);

            var totalItems = await itemsQuery.CountAsync();
            var pageCount = (int)Math.Ceiling((double)totalItems / size);
            var items = await itemsQuery
                .Skip((page - 1) * size)
                .Take(size)
                .ToListAsync();

            return new Response<PagedResult<Item>>(
                true,
                "Items fetched successfully.",
                new PagedResult<Item>
                {
                    Queryable = items.AsQueryable(),
                    RowCount = totalItems,
                    CurrentPage = page,
                    PageSize = size,
                    PageCount = pageCount
                });
        }

        // GET: api/Item/home
        [HttpGet("home")]
        public async Task<Response<PagedResult<Item>>> GetItemsHome(
            [FromQuery] int page = 1,
            [FromQuery] int size = 10,
            [FromQuery] string? search = null,
            [FromQuery] int? categoryId = null)
        {
            try
            {
                var itemsQuery = _context.Items.AsQueryable();

                // Apply category filter if categoryId is provided
                if (categoryId.HasValue && categoryId > 0)
                {
                    itemsQuery = itemsQuery.Where(item => item.CategoryId == categoryId.Value);
                }

                // Apply search filter if search term is provided
                if (!string.IsNullOrEmpty(search))
                {
                    itemsQuery = itemsQuery.Where(item => item.Title.ToLower().Contains(search.ToLower()) || item.Description.ToLower().Contains(search.ToLower()));
                }

                var totalItems = await itemsQuery.CountAsync();
                var pageCount = (int)Math.Ceiling((double)totalItems / size);

                var items = await itemsQuery
                    .Skip((page - 1) * size)
                    .Take(size)
                    .ToListAsync();

                return new Response<PagedResult<Item>>(
                    true,
                    "Items fetched successfully.",
                    new PagedResult<Item>
                    {
                        Queryable = items.AsQueryable(),
                        RowCount = totalItems,
                        CurrentPage = page,
                        PageSize = size,
                        PageCount = pageCount
                    });
            } catch (Exception ex)
            {
                return new Response<PagedResult<Item>>(false, Constant.FAIL_MESSAGE, null);
            }
        }


        // GET: api/Item/{id}
        [HttpGet("{id}")]
        public async Task<Response<Item>> GetItemById(int id)
        {
            var item = await _context.Items.Include(i => i.Bids).ThenInclude(b => b.Bidder).Include(i => i.Seller).FirstOrDefaultAsync(i => i.Id == id);
            if (item == null)
            {
                return new Response<Item>(false, "Item not found", null);
            }

            item.Seller.Bids = null;
            item.Seller.Notifications = null;
            item.Seller.RatingRatees = null;
            item.Seller.RatingRaters = null;
            item.Seller.Reports = null;
            item.Seller.Items = null;
            item.Seller.Password = null;

            foreach (var bid in item?.Bids)
            {
                bid.Item = null;
                if (bid.Bidder != null)
                {
                    bid.Bidder.Password = null;
                    bid.Bidder.Bids = null;
                }
            }

            return new Response<Item>(true, "Item fetched successfully.", item);
        }

        // POST: api/Item
        [Authorize]
        [HttpPost]
        public async Task<Response<bool>> AddItem(Item newItem)
        {
            try
            {
                int userId = JwtMiddleware.GetUserId(HttpContext);
                var user = await _context.Users.FindAsync(userId);
                if (user == null || user.Credit < 5)
                {
                    throw new Exception("Not enought credit!");
                }

                newItem.SellerId = userId;

                user.Credit -= 5000;

                await _context.Items.AddAsync(newItem);
                await _context.SaveChangesAsync();
                return new Response<bool>(true, "Item added successfully.", true);
            }
            catch (Exception ex)
            {
                return new Response<bool>(false, $"Error adding item: {ex.Message}", false);
            }
        }

        // PUT: api/Item
        [Authorize]
        [HttpPut]
        public async Task<Response<bool>> UpdateItem(Item updatedItem)
        {
            var item = await _context.Items.FindAsync(updatedItem.Id);
            if (item == null)
            {
                return new Response<bool>(false, "Item not found", false);
            }

            item.Title = updatedItem.Title;
            item.Description = updatedItem.Description;
            item.CategoryId = updatedItem.CategoryId;
            item.MinimumBid = updatedItem.MinimumBid;
            item.BidIncrement = updatedItem.BidIncrement;
            item.BidStartDate = updatedItem.BidStartDate;
            item.BidEndDate = updatedItem.BidEndDate;

            item.ImagePath = String.IsNullOrEmpty(updatedItem.ImagePath) ? item.ImagePath : updatedItem.ImagePath;
            item.DocumentPath = String.IsNullOrEmpty(updatedItem.DocumentPath) ? item.DocumentPath : updatedItem.DocumentPath;

            try
            {
                _context.Items.Update(item);
                await _context.SaveChangesAsync();
                return new Response<bool>(true, "Item updated successfully.", true);
            }
            catch (Exception ex)
            {
                return new Response<bool>(false, $"Error updating item: {ex.Message}", false);
            }
        }

        // DELETE: api/Item/{id} - Hard delete
        [Authorize]
        [HttpDelete("{id}")]
        public async Task<Response<bool>> DeleteItem(int id)
        {
            var item = await _context.Items.FindAsync(id);
            if (item == null)
            {
                return new Response<bool>(false, "Item not found", false);
            }

            try
            {
                var bids = _context.Bids.Where(b => b.ItemId == id);
                _context.Bids.RemoveRange(bids);

                // Save changes for updating bids
                await _context.SaveChangesAsync();

                // Remove the item from the DbContext
                _context.Items.Remove(item);

                // Save changes to the database
                await _context.SaveChangesAsync();

                return new Response<bool>(true, "Item deleted successfully.", true);
            }
            catch (Exception ex)
            {
                return new Response<bool>(false, $"Error deleting item: {ex.Message}", false);
            }
        }

    }
}
