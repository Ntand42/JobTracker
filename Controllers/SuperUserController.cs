using JobTracker.Data;
using JobTracker.Models;
using JobTracker.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace JobTracker.Controllers
{
    [Authorize(Roles = "SuperUser")]
    public class SuperUserController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public SuperUserController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            var users = await _userManager.Users.OrderBy(u => u.Email).ToListAsync();
            var rows = new List<SuperUserUserRowViewModel>(users.Count);

            foreach (var user in users)
            {
                var roles = await _userManager.GetRolesAsync(user);
                var isDisabled = user.LockoutEnd.HasValue && user.LockoutEnd.Value > DateTimeOffset.UtcNow;

                rows.Add(new SuperUserUserRowViewModel
                {
                    Id = user.Id,
                    Email = user.Email ?? user.UserName ?? string.Empty,
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    EmailConfirmed = user.EmailConfirmed,
                    IsDisabled = isDisabled,
                    LockoutEnd = user.LockoutEnd,
                    Roles = roles.ToArray()
                });
            }

            return View(new SuperUserUsersViewModel { Users = rows });
        }

        public async Task<IActionResult> Activity(string id)
        {
            var subjectUser = await _userManager.FindByIdAsync(id);
            if (subjectUser == null)
            {
                return NotFound();
            }

            var activities = await _context.UserActivities
                .Where(a => a.SubjectUserId == id)
                .OrderByDescending(a => a.CreatedAtUtc)
                .Take(200)
                .ToListAsync();

            var actorIds = activities
                .Select(a => a.ActorUserId)
                .Where(a => !string.IsNullOrWhiteSpace(a))
                .Distinct()
                .ToList();

            var actorLookup = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            if (actorIds.Count > 0)
            {
                var actorUsers = await _userManager.Users.Where(u => actorIds.Contains(u.Id)).ToListAsync();
                foreach (var actor in actorUsers)
                {
                    actorLookup[actor.Id] = actor.Email ?? actor.UserName ?? actor.Id;
                }
            }

            var rows = activities.Select(a => new SuperUserActivityRowViewModel
            {
                CreatedAtUtc = a.CreatedAtUtc,
                Action = a.Action,
                Details = a.Details,
                ActorEmail = a.ActorUserId != null && actorLookup.TryGetValue(a.ActorUserId, out var value) ? value : null,
                IpAddress = a.IpAddress
            }).ToList();

            return View(new SuperUserActivityViewModel
            {
                SubjectUserId = subjectUser.Id,
                SubjectEmail = subjectUser.Email ?? subjectUser.UserName ?? string.Empty,
                SubjectName = $"{subjectUser.FirstName} {subjectUser.LastName}".Trim(),
                Activities = rows
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Disable(string id)
        {
            var subjectUser = await _userManager.FindByIdAsync(id);
            if (subjectUser == null)
            {
                return NotFound();
            }

            var actorUserId = _userManager.GetUserId(User);
            if (string.Equals(actorUserId, subjectUser.Id, StringComparison.OrdinalIgnoreCase))
            {
                return RedirectToAction(nameof(Index));
            }

            await _userManager.SetLockoutEnabledAsync(subjectUser, true);
            await _userManager.SetLockoutEndDateAsync(subjectUser, DateTimeOffset.UtcNow.AddYears(100));
            await _userManager.UpdateSecurityStampAsync(subjectUser);

            _context.UserActivities.Add(new UserActivity
            {
                ActorUserId = actorUserId,
                SubjectUserId = subjectUser.Id,
                Action = "AccountDisabled",
                Details = subjectUser.Email ?? subjectUser.UserName,
                IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString(),
                UserAgent = Request.Headers.UserAgent.ToString()
            });
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Enable(string id)
        {
            var subjectUser = await _userManager.FindByIdAsync(id);
            if (subjectUser == null)
            {
                return NotFound();
            }

            await _userManager.SetLockoutEndDateAsync(subjectUser, null);
            await _userManager.UpdateSecurityStampAsync(subjectUser);

            var actorUserId = _userManager.GetUserId(User);
            _context.UserActivities.Add(new UserActivity
            {
                ActorUserId = actorUserId,
                SubjectUserId = subjectUser.Id,
                Action = "AccountEnabled",
                Details = subjectUser.Email ?? subjectUser.UserName,
                IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString(),
                UserAgent = Request.Headers.UserAgent.ToString()
            });
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(string id)
        {
            var subjectUser = await _userManager.FindByIdAsync(id);
            if (subjectUser == null)
            {
                return NotFound();
            }

            var actorUserId = _userManager.GetUserId(User);
            if (string.Equals(actorUserId, subjectUser.Id, StringComparison.OrdinalIgnoreCase))
            {
                return RedirectToAction(nameof(Index));
            }

            var jobs = await _context.JobApplications.Where(j => j.UserId == subjectUser.Id).ToListAsync();
            if (jobs.Count > 0)
            {
                _context.JobApplications.RemoveRange(jobs);
            }

            var logs = await _context.UserActivities.Where(a => a.SubjectUserId == subjectUser.Id).ToListAsync();
            if (logs.Count > 0)
            {
                _context.UserActivities.RemoveRange(logs);
            }

            await _context.SaveChangesAsync();

            var deleteResult = await _userManager.DeleteAsync(subjectUser);
            if (!deleteResult.Succeeded)
            {
                return RedirectToAction(nameof(Index));
            }

            _context.UserActivities.Add(new UserActivity
            {
                ActorUserId = actorUserId,
                SubjectUserId = subjectUser.Id,
                Action = "AccountDeleted",
                Details = subjectUser.Email ?? subjectUser.UserName,
                IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString(),
                UserAgent = Request.Headers.UserAgent.ToString()
            });
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }
    }
}
