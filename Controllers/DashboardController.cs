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
            var user = await _userManager.GetUserAsync(User);
            var userId = user?.Id;

            var allApplications = await _context.JobApplications
                .Where(j => j.UserId == userId)
                .OrderByDescending(j => j.AppliedDate)
                .ToListAsync();

            var viewModel = new DashboardViewModel
            {
                UserName = user?.FirstName ?? user?.UserName ?? "User",
                TotalApplications = allApplications.Count,
                AppliedCount = allApplications.Count(j => j.Status == ApplicationStatus.Applied),
                InterviewCount = allApplications.Count(j => j.Status == ApplicationStatus.Interview),
                OfferCount = allApplications.Count(j => j.Status == ApplicationStatus.Offer),
                RejectedCount = allApplications.Count(j => j.Status == ApplicationStatus.Rejected),
                RecentApplications = allApplications.Take(5).ToList(),
                UpcomingInterviews = allApplications
                    .Where(j => (j.InterviewDate.HasValue && j.InterviewDate.Value >= DateTime.Today) || 
                                (j.Status == ApplicationStatus.Interview && !j.InterviewDate.HasValue))
                    .OrderBy(j => j.InterviewDate ?? DateTime.MaxValue)
                    .Take(5)
                    .ToList(),
                PendingFollowUps = allApplications
                    .Where(j => j.FollowUpDate.HasValue && j.FollowUpDate.Value >= DateTime.Today)
                    .OrderBy(j => j.FollowUpDate)
                    .Take(5)
                    .ToList(),
                
                // Analytics Calculations
                ApplicationsPerMonth = allApplications
                    .GroupBy(j => new { j.AppliedDate.Year, j.AppliedDate.Month })
                    .OrderBy(g => g.Key.Year).ThenBy(g => g.Key.Month)
                    .ToDictionary(
                        g => new DateTime(g.Key.Year, g.Key.Month, 1).ToString("MMM yyyy"), 
                        g => g.Count()
                    ),
                
                SuccessRate = allApplications.Any() 
                    ? Math.Round((double)allApplications.Count(j => j.Status == ApplicationStatus.Offer) / allApplications.Count * 100, 1) 
                    : 0,
                
                ResponseRate = allApplications.Any()
                    ? Math.Round((double)allApplications.Count(j => j.Status == ApplicationStatus.Interview || j.Status == ApplicationStatus.Offer || j.Status == ApplicationStatus.Rejected) / allApplications.Count * 100, 1)
                    : 0,
                
                AverageResponseTimeDays = CalculateAverageResponseTime(allApplications)
            };

            return View(viewModel);
        }

        private double CalculateAverageResponseTime(List<JobApplication> applications)
        {
            var respondedApps = applications.Where(j => j.InterviewDate.HasValue || j.OutcomeDate.HasValue).ToList();
            
            if (!respondedApps.Any()) return 0;

            double totalDays = 0;
            int count = 0;

            foreach (var app in respondedApps)
            {
                DateTime? responseDate = app.InterviewDate ?? app.OutcomeDate;
                if (responseDate.HasValue && responseDate.Value >= app.AppliedDate)
                {
                    totalDays += (responseDate.Value - app.AppliedDate).TotalDays;
                    count++;
                }
            }

            return count > 0 ? Math.Round(totalDays / count, 1) : 0;
        }
    }
}