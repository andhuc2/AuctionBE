﻿using API.Models;
using API.Models.Context;
using Microsoft.AspNetCore.Mvc;
using NET_base.Models.Common;
using Microsoft.EntityFrameworkCore;
using System.Linq.Dynamic.Core;
using Microsoft.AspNetCore.Authorization;
using NET_base.Models.DTO;
using AutoMapper;

namespace API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UserController : ControllerBase
    {
        private readonly DBContext _context;
        private readonly IMapper _mapper;

        public UserController(DBContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }

        // GET: api/User
        [HttpGet]
        [Authorize("Admin")]
        public async Task<Response<PagedResult<User>>> GetUsers([FromQuery] int page = 1, [FromQuery] int size = 10)
        {
            var usersQuery = _context.Users;

            var totalItems = await usersQuery.CountAsync();
            var users = await usersQuery
                .Skip((page - 1) * size)
                .Take(size)
                .ToListAsync();

            return new Response<PagedResult<User>>(
                true,
                "Users fetched successfully.",
                new PagedResult<User>
                {
                    Queryable = users.AsQueryable(),
                    RowCount = totalItems,
                    CurrentPage = page,
                    PageSize = size
                });
        }

        // GET: api/User/{id}
        [HttpGet("{id}")]
        [Authorize("Admin")]
        public async Task<Response<User>> GetUserById(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null)
            {
                return new Response<User>(false, "User not found", null);
            }

            return new Response<User>(true, "User fetched successfully.", user);
        }

        // GET: api/User/Profile/{id}
        [HttpGet("Profile/{id}")]
        public async Task<Response<UserDTO>> GetUserProfile(int id)
        {
            int userId = JwtMiddleware.GetUserId(HttpContext);

            var user = await _context.Users.Where(u => u.Id == id).Include(u => u.Items).Include(u => u.Bids).FirstOrDefaultAsync();
            if (user == null)
            {
                return new Response<UserDTO>(false, "User not found", null);
            }

            UserDTO userDTO = _mapper.Map<UserDTO>(user);

            return new Response<UserDTO>(true, "User fetched successfully.", userDTO);
        }

        // PUT: api/User/Profile
        [HttpPut("Profile")]
        [Authorize]
        public async Task<Response<bool>> UpdateProfile(User updatedUser)
        {
            var user = await _context.Users.FindAsync(updatedUser.Id);
            if (user == null)
            {
                return new Response<bool>(false, "User not found", false);
            }

            user.FullName = updatedUser.FullName;
            user.Phone = updatedUser.Phone;
            user.Address = updatedUser.Address;

            try
            {
                _context.Users.Update(user);
                await _context.SaveChangesAsync();
                return new Response<bool>(true, "User updated successfully.", true);
            }
            catch (Exception ex)
            {
                return new Response<bool>(false, $"Error updating user: {ex.Message}", false);
            }
        }

        // POST: api/User
        [HttpPost]
        [Authorize("Admin")]
        public async Task<Response<bool>> AddUser(User newUser)
        {
            try
            {
                await _context.Users.AddAsync(newUser);
                await _context.SaveChangesAsync();
                return new Response<bool>(true, "User added successfully.", true);
            }
            catch (Exception ex)
            {
                return new Response<bool>(false, $"Error adding user: {ex.Message}", false);
            }
        }

        // PUT: api/User
        [HttpPut]
        [Authorize("Admin")]
        public async Task<Response<bool>> UpdateUser(User updatedUser)
        {
            var user = await _context.Users.FindAsync(updatedUser.Id);
            if (user == null)
            {
                return new Response<bool>(false, "User not found", false);
            }

            user.Username = updatedUser.Username;
            user.Email = updatedUser.Email;
            user.Password = updatedUser.Password;
            user.FullName = updatedUser.FullName;
            user.Phone = updatedUser.Phone;
            user.Address = updatedUser.Address;
            user.Role = updatedUser.Role;
            user.IsDeleted = updatedUser.IsDeleted;

            try
            {
                _context.Users.Update(user);
                await _context.SaveChangesAsync();
                return new Response<bool>(true, "User updated successfully.", true);
            }
            catch (Exception ex)
            {
                return new Response<bool>(false, $"Error updating user: {ex.Message}", false);
            }
        }

        // DELETE: api/User/{id} - Soft delete
        [HttpDelete("{id}")]
        [Authorize("Admin")]
        public async Task<Response<bool>> DeleteUser(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null)
            {
                return new Response<bool>(false, "User not found", false);
            }

            try
            {
                // Set IsDeleted to true instead of removing the record
                user.IsDeleted = true;

                // Save changes
                _context.Users.Update(user);
                await _context.SaveChangesAsync();

                return new Response<bool>(true, "User marked as deleted successfully.", true);
            }
            catch (Exception ex)
            {
                return new Response<bool>(false, $"Error marking user as deleted: {ex.Message}", false);
            }
        }

    }
}
