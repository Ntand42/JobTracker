using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using JobTracker.Data;
using JobTracker.Models;
using JobTracker.Models.ViewModels;

namespace JobTracker.Controllers
{
    [Authorize]
    public class DashboardController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public DashboardController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            var userId = _userManager.GetUserId(User);

            var allApplications = await _context.JobApplications
                .Where(j => j.UserId == userId)
                .OrderByDescending(j => j.AppliedDate)
                .ToListAsync();

            var viewModel = new DashboardViewModel
            {
                TotalApplications = allApplications.Count,
                AppliedCount = allApplications.Count(j => j.Status == ApplicationStatus.Applied),
                InterviewCount = allApplications.Count(j => j.Status == ApplicationStatus.Interview),
                OfferCount = allApplications.Count(j => j.Status == ApplicationStatus.Offer),
                RejectedCount = allApplications.Count(j => j.Status == ApplicationStatus.Rejected),
                RecentApplications = allApplications.Take(5).ToList()
            };

            return View(viewModel);
        }
    }
}