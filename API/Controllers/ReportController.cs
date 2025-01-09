using API.Models;
using API.Models.Context;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using NET_base.Models.Common;
using API.Utils;

namespace API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ReportController : ControllerBase
    {
        private readonly DBContext _context;

        public ReportController(DBContext context)
        {
            _context = context;
        }

        // GET: api/Report
        [HttpGet]
        [Authorize("Admin")]
        public async Task<Response<List<Report>>> GetReports()
        {
            var reports = await _context.Reports.ToListAsync();
            return new Response<List<Report>>(true, "Reports fetched successfully.", reports);
        }

        // GET: api/Report/{id}
        [HttpGet("{id}")]
        [Authorize("Admin")]
        public async Task<Response<Report>> GetReportById(int id)
        {
            var report = await _context.Reports.FirstOrDefaultAsync(r => r.Id == id);

            if (report == null)
            {
                return new Response<Report>(false, "Report not found.", null);
            }

            return new Response<Report>(true, "Report fetched successfully.", report);
        }

        // GET: api/Report/{userId}
        [HttpGet("{userId}")]
        [Authorize("Admin")]
        public async Task<Response<List<Report>>> GetReportsByUser(int userId)
        {
            var reports = await _context.Reports.Where(r => r.UserId == userId)
                                                 .ToListAsync();

            if (reports == null || reports.Count == 0)
            {
                return new Response<List<Report>>(false, "No reports found for this user.", null);
            }

            return new Response<List<Report>>(true, "Reports fetched successfully.", reports);
        }

        // POST: api/Report
        [Authorize]
        [HttpPost]
        public async Task<Response<bool>> AddReport(Report newReport)
        {
            try
            {
                int? userId = JwtMiddleware.GetUserId(HttpContext);

                var reportedUser = await _context.Users.Where(u => u.Id == newReport.UserId).FirstOrDefaultAsync();
                if (reportedUser == null)
                {
                    return new Response<bool>(false, "User not found", false);
                }

                var reportUser = await _context.Users.Where(u => u.Id == userId).FirstOrDefaultAsync();
                if (userId == null || reportUser == null)
                {
                    return new Response<bool>(false, "User not found", false);
                }

                newReport.CreatedBy = userId;
                newReport.CreatedAt = DateTime.UtcNow;

                await _context.Reports.AddAsync(newReport);
                await _context.SaveChangesAsync();

                var admins = await _context.Users.Where(u => u.Role == Constant.ADMIN_ROLE).ToListAsync();
                foreach (var admin in admins)
                {
                    EmailService.SendMailAsync(admin.Email, "New Report Submitted", $"Report from user {reportUser.Username}: {newReport.Content}");
                }

                return new Response<bool>(true, "Report added successfully.", true);
            }
            catch (Exception ex)
            {
                return new Response<bool>(false, $"Error adding report: {ex.Message}", false);
            }
        }

        // PUT: api/Report
        [Authorize("Admin")]
        [HttpPut]
        public async Task<Response<bool>> UpdateReport(Report updatedReport)
        {
            var report = await _context.Reports.FindAsync(updatedReport.Id);
            if (report == null)
            {
                return new Response<bool>(false, "Report not found.", false);
            }

            report.Content = updatedReport.Content;
            report.CreatedAt = updatedReport.CreatedAt;

            try
            {
                _context.Reports.Update(report);
                await _context.SaveChangesAsync();
                return new Response<bool>(true, "Report updated successfully.", true);
            }
            catch (Exception ex)
            {
                return new Response<bool>(false, $"Error updating report: {ex.Message}", false);
            }
        }

        // DELETE: api/Report/{id}
        [Authorize("Admin")]
        [HttpDelete("{id}")]
        public async Task<Response<bool>> DeleteReport(int id)
        {
            var report = await _context.Reports.FindAsync(id);
            if (report == null)
            {
                return new Response<bool>(false, "Report not found.", false);
            }

            try
            {
                _context.Reports.Remove(report);
                await _context.SaveChangesAsync();
                return new Response<bool>(true, "Report deleted successfully.", true);
            }
            catch (Exception ex)
            {
                return new Response<bool>(false, $"Error deleting report: {ex.Message}", false);
            }
        }
    }
}
