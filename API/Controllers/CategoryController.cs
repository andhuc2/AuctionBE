using API.Models;
using API.Models.Context;
using Microsoft.AspNetCore.Mvc;
using NET_base.Models.Common;
using Microsoft.EntityFrameworkCore;
using System.Linq.Dynamic.Core;

namespace API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CategoryController : ControllerBase
    {
        private readonly DBContext _context;

        public CategoryController(DBContext context)
        {
            _context = context;
        }

        // GET: api/Category
        [HttpGet]
        public async Task<Response<PagedResult<Category>>> GetCategories([FromQuery] int page = 1, [FromQuery] int size = 10)
        {
            var categoriesQuery = _context.Categories;

            var totalItems = await categoriesQuery.CountAsync();
            var categories = await categoriesQuery
                .Skip((page - 1) * size)
                .Take(size)
                .ToListAsync();

            return new Response<PagedResult<Category>>(
                true,
                "Categories fetched successfully.",
                new PagedResult<Category>
                {
                    Queryable = categories.AsQueryable(),
                    RowCount = totalItems,
                    CurrentPage = page,
                    PageSize = size
                });
        }

        // GET: api/Category/all
        [HttpGet("all")]
        public async Task<Response<List<Category>>> GetAllCategories()
        {
            var categories = await _context.Categories.ToListAsync();

            return new Response<List<Category>>(
                true,
                "Categories fetched successfully.",
                categories);
        }

        // GET: api/Category/{id}
        [HttpGet("{id}")]
        public async Task<Response<Category>> GetCategoryById(int id)
        {
            var category = await _context.Categories
                .Include(c => c.InverseParentCategory) // Include child categories
                .Include(c => c.Items) // Include associated items
                .FirstOrDefaultAsync(c => c.Id == id);

            if (category == null)
            {
                return new Response<Category>(false, "Category not found", null);
            }

            return new Response<Category>(true, "Category fetched successfully.", category);
        }

        /*// POST: api/Category
        [HttpPost]
        public async Task<Response<bool>> AddCategory(Category newCategory)
        {
            try
            {
                await _context.Categories.AddAsync(newCategory);
                await _context.SaveChangesAsync();
                return new Response<bool>(true, "Category added successfully.", true);
            }
            catch (Exception ex)
            {
                return new Response<bool>(false, $"Error adding category: {ex.Message}", false);
            }
        }

        // PUT: api/Category
        [HttpPut]
        public async Task<Response<bool>> UpdateCategory(Category updatedCategory)
        {
            var category = await _context.Categories.FindAsync(updatedCategory.Id);
            if (category == null)
            {
                return new Response<bool>(false, "Category not found", false);
            }

            category.CategoryName = updatedCategory.CategoryName;
            category.ParentCategoryId = updatedCategory.ParentCategoryId;
            category.CreatedAt = updatedCategory.CreatedAt;

            try
            {
                _context.Categories.Update(category);
                await _context.SaveChangesAsync();
                return new Response<bool>(true, "Category updated successfully.", true);
            }
            catch (Exception ex)
            {
                return new Response<bool>(false, $"Error updating category: {ex.Message}", false);
            }
        }*/
    }
}
