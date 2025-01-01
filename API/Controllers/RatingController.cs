using API.Models;
using API.Models.Context;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using NET_base.Models.Common;

namespace API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class RatingController : ControllerBase
    {
        private readonly DBContext _context;

        public RatingController(DBContext context)
        {
            _context = context;
        }

        // GET: api/Rating
        [HttpGet]
        [Authorize("Admin")]
        public async Task<Response<List<Rating>>> GetRatings()
        {
            var ratings = await _context.Ratings.Include(r => r.Item)
                                                .Include(r => r.Rater)
                                                .Include(r => r.Ratee)
                                                .ToListAsync();
            return new Response<List<Rating>>(true, "Ratings fetched successfully.", ratings);
        }

        // GET: api/Rating/{id}
        [HttpGet("{id}")]
        [Authorize("Admin")]
        public async Task<Response<Rating>> GetRatingById(int id)
        {
            var rating = await _context.Ratings.Include(r => r.Item)
                                               .Include(r => r.Rater)
                                               .Include(r => r.Ratee)
                                               .FirstOrDefaultAsync(r => r.Id == id);

            if (rating == null)
            {
                return new Response<Rating>(false, "Rating not found.", null);
            }

            return new Response<Rating>(true, "Rating fetched successfully.", rating);
        }

        // GET: api/Rating/{userId}/{itemId}
        [HttpGet("{userId}/{itemId}")]
        [Authorize]
        public async Task<Response<Rating>> GetRatingByUserAndItem(int userId, int itemId)
        {
            var rating = await _context.Ratings.FirstOrDefaultAsync(r => r.RateeId == userId && r.ItemId == itemId);

            if (rating == null)
            {
                return new Response<Rating>(false, "Rating not found.", null);
            }

            return new Response<Rating>(true, "Rating fetched successfully.", rating);
        }

        // POST: api/Rating
        [Authorize]
        [HttpPost]
        public async Task<Response<bool>> AddRating(Rating newRating)
        {
            try
            {

                Rating rating = await _context.Ratings.FirstOrDefaultAsync(r => r.RateeId == newRating.RateeId && r.ItemId == newRating.ItemId);
                if (rating != null)
                {
                    rating.RatingValue = newRating.RatingValue;
                    await _context.SaveChangesAsync();
                }
                else
                {
                    int userId = JwtMiddleware.GetUserId(HttpContext);
                    newRating.RaterId = userId;
                    await _context.Ratings.AddAsync(newRating);
                    await _context.SaveChangesAsync();
                }
                
                return new Response<bool>(true, "Rating added successfully.", true);
            }
            catch (Exception ex)
            {
                return new Response<bool>(false, $"Error adding rating: {ex.Message}", false);
            }
        }

        // PUT: api/Rating
        [Authorize]
        [HttpPut]
        public async Task<Response<bool>> UpdateRating(Rating updatedRating)
        {
            var rating = await _context.Ratings.FindAsync(updatedRating.Id);
            if (rating == null)
            {
                return new Response<bool>(false, "Rating not found.", false);
            }

            rating.RatingValue = updatedRating.RatingValue;
            rating.CreatedAt = updatedRating.CreatedAt;

            try
            {
                _context.Ratings.Update(rating);
                await _context.SaveChangesAsync();
                return new Response<bool>(true, "Rating updated successfully.", true);
            }
            catch (Exception ex)
            {
                return new Response<bool>(false, $"Error updating rating: {ex.Message}", false);
            }
        }

        // DELETE: api/Rating/{id}
        [Authorize]
        [HttpDelete("{id}")]
        public async Task<Response<bool>> DeleteRating(int id)
        {
            var rating = await _context.Ratings.FindAsync(id);
            if (rating == null)
            {
                return new Response<bool>(false, "Rating not found.", false);
            }

            try
            {
                _context.Ratings.Remove(rating);
                await _context.SaveChangesAsync();
                return new Response<bool>(true, "Rating deleted successfully.", true);
            }
            catch (Exception ex)
            {
                return new Response<bool>(false, $"Error deleting rating: {ex.Message}", false);
            }
        }
    }
}
