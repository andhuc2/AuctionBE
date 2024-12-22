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
            var itemsQuery = _context.Items.AsQueryable();

            var totalItems = await itemsQuery.CountAsync();
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
                    PageSize = size
                });
        }

        // GET: api/Item/{id}
        [HttpGet("{id}")]
        public async Task<Response<Item>> GetItemById(int id)
        {
            var item = await _context.Items.FindAsync(id);
            if (item == null)
            {
                return new Response<Item>(false, "Item not found", null);
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
                if (userId == null || userId < 1) throw new Exception("Need login first!");

                newItem.SellerId = userId;

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
            item.SellerId = updatedItem.SellerId;
            item.CategoryId = updatedItem.CategoryId;
            item.ImagePath = updatedItem.ImagePath;
            item.DocumentPath = updatedItem.DocumentPath;
            item.MinimumBid = updatedItem.MinimumBid;
            item.BidIncrement = updatedItem.BidIncrement;
            item.BidStatus = updatedItem.BidStatus;
            item.BidStartDate = updatedItem.BidStartDate;
            item.BidEndDate = updatedItem.BidEndDate;
            item.UpdatedAt = updatedItem.UpdatedAt;

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
