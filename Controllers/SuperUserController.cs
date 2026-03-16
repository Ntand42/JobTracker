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
            var today = DateTime.Today;
            var monthStart = new DateTime(today.Year, today.Month, 1);
            var nextMonthStart = monthStart.AddMonths(1);

            var totalUsers = await _userManager.Users.CountAsync();
            var totalApplications = await _context.JobApplications.CountAsync();
            var totalInterviews = await _context.JobApplications.CountAsync(j => j.Status == ApplicationStatus.Interview);
            var totalOffers = await _context.JobApplications.CountAsync(j => j.Status == ApplicationStatus.Offer);
            var applicationsThisMonth = await _context.JobApplications.CountAsync(j => j.AppliedDate >= monthStart && j.AppliedDate < nextMonthStart);

            var averageApplicationsPerUser = totalUsers > 0
                ? Math.Round((double)totalApplications / totalUsers, 2)
                : 0;

            var mostActive = await _context.JobApplications
                .Where(j => !string.IsNullOrWhiteSpace(j.UserId))
                .GroupBy(j => j.UserId!)
                .Select(g => new { UserId = g.Key, TotalApplications = g.Count() })
                .OrderByDescending(x => x.TotalApplications)
                .Take(5)
                .ToListAsync();

            var mostActiveUserIds = mostActive.Select(x => x.UserId).ToList();
            var mostActiveUsers = mostActiveUserIds.Count == 0
                ? new List<ApplicationUser>()
                : await _userManager.Users.Where(u => mostActiveUserIds.Contains(u.Id)).ToListAsync();

            var mostActiveLookup = mostActiveUsers.ToDictionary(u => u.Id, u => u);
            var mostActiveRows = mostActive.Select(x =>
            {
                if (!mostActiveLookup.TryGetValue(x.UserId, out var user))
                {
                    return new SuperUserMostActiveUserViewModel
                    {
                        UserId = x.UserId,
                        Email = x.UserId,
                        Name = string.Empty,
                        TotalApplications = x.TotalApplications
                    };
                }

                return new SuperUserMostActiveUserViewModel
                {
                    UserId = user.Id,
                    Email = user.Email ?? user.UserName ?? string.Empty,
                    Name = $"{user.FirstName} {user.LastName}".Trim(),
                    TotalApplications = x.TotalApplications
                };
            }).ToList();

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

            return View(new SuperUserUsersViewModel
            {
                Users = rows,
                Stats = new SuperUserPlatformStatsViewModel
                {
                    TotalUsers = totalUsers,
                    TotalApplications = totalApplications,
                    TotalInterviews = totalInterviews,
                    TotalOffers = totalOffers,
                    ApplicationsThisMonth = applicationsThisMonth,
                    AverageApplicationsPerUser = averageApplicationsPerUser,
                    CurrentMonthLabel = monthStart.ToString("MMMM yyyy")
                },
                MostActiveUsers = mostActiveRows
            });
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

        public async Task<IActionResult> Applications(ApplicationStatus? status, string? q, int page = 1, int pageSize = 50)
        {
            if (page < 1) page = 1;
            if (pageSize < 10) pageSize = 10;
            if (pageSize > 200) pageSize = 200;

            var baseQuery =
                from j in _context.JobApplications.AsNoTracking()
                join u in _userManager.Users.AsNoTracking() on j.UserId equals u.Id into ju
                from u in ju.DefaultIfEmpty()
                select new
                {
                    Job = j,
                    UserId = j.UserId,
                    UserEmail = u != null ? (u.Email ?? u.UserName) : null,
                    UserName = u != null ? (u.FirstName + " " + u.LastName) : null
                };

            if (status.HasValue)
            {
                baseQuery = baseQuery.Where(x => x.Job.Status == status.Value);
            }

            if (!string.IsNullOrWhiteSpace(q))
            {
                var term = q.Trim();
                baseQuery = baseQuery.Where(x =>
                    x.Job.CompanyName.Contains(term) ||
                    x.Job.Position.Contains(term) ||
                    ((x.UserEmail ?? "")).Contains(term));
            }

            var totalCount = await baseQuery.CountAsync();

            var items = await baseQuery
                .OrderByDescending(x => x.Job.AppliedDate)
                .ThenByDescending(x => x.Job.JobApplicationId)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(x => new SuperUserApplicationRowViewModel
                {
                    JobApplicationId = x.Job.JobApplicationId,
                    CompanyName = x.Job.CompanyName,
                    Position = x.Job.Position,
                    Status = x.Job.Status,
                    AppliedDate = x.Job.AppliedDate,
                    UserId = x.UserId,
                    UserEmail = x.UserEmail ?? x.UserId ?? string.Empty,
                    UserName = (x.UserName ?? string.Empty).Trim()
                })
                .ToListAsync();

            return View(new SuperUserApplicationsViewModel
            {
                Applications = items,
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize,
                Status = status,
                Q = q
            });
        }

        public async Task<IActionResult> Notifications()
        {
            var rows = await _context.GlobalNotifications
                .AsNoTracking()
                .OrderByDescending(n => n.CreatedAtUtc)
                .Take(100)
                .Select(n => new SuperUserNotificationRowViewModel
                {
                    GlobalNotificationId = n.GlobalNotificationId,
                    Message = n.Message,
                    CreatedAtUtc = n.CreatedAtUtc,
                    StartsAtUtc = n.StartsAtUtc,
                    EndsAtUtc = n.EndsAtUtc,
                    IsActive = n.IsActive
                })
                .ToListAsync();

            return View(new SuperUserNotificationsViewModel
            {
                StartsAtUtc = DateTime.Now,
                Notifications = rows
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Notifications(string message, DateTime? startsAtUtc, DateTime? endsAtUtc)
        {
            if (string.IsNullOrWhiteSpace(message))
            {
                return await Notifications();
            }

            var starts = startsAtUtc ?? DateTime.Now;
            DateTime? ends = endsAtUtc;
            if (ends.HasValue && ends.Value <= starts)
            {
                ends = null;
            }

            var actorUserId = _userManager.GetUserId(User);
            var notification = new GlobalNotification
            {
                Message = message.Trim(),
                CreatedByUserId = actorUserId,
                CreatedAtUtc = DateTime.Now,
                StartsAtUtc = starts,
                EndsAtUtc = ends,
                IsActive = true
            };

            _context.GlobalNotifications.Add(notification);
            _context.UserActivities.Add(new UserActivity
            {
                ActorUserId = actorUserId,
                SubjectUserId = actorUserId ?? string.Empty,
                Action = "GlobalNotificationCreated",
                Details = notification.Message,
                IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString(),
                UserAgent = Request.Headers.UserAgent.ToString()
            });

            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Notifications));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeactivateNotification(int id)
        {
            var notification = await _context.GlobalNotifications.FirstOrDefaultAsync(n => n.GlobalNotificationId == id);
            if (notification == null)
            {
                return RedirectToAction(nameof(Notifications));
            }

            notification.IsActive = false;
            notification.EndsAtUtc = DateTime.Now;

            var actorUserId = _userManager.GetUserId(User);
            _context.UserActivities.Add(new UserActivity
            {
                ActorUserId = actorUserId,
                SubjectUserId = actorUserId ?? string.Empty,
                Action = "GlobalNotificationDeactivated",
                Details = notification.Message,
                IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString(),
                UserAgent = Request.Headers.UserAgent.ToString()
            });

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Notifications));
        }

        public async Task<IActionResult> Logs(string? actionFilter, string? q, int page = 1, int pageSize = 50)
        {
            if (page < 1) page = 1;
            if (pageSize < 10) pageSize = 10;
            if (pageSize > 200) pageSize = 200;

            var baseQuery =
                from a in _context.UserActivities.AsNoTracking()
                join subject in _userManager.Users.AsNoTracking() on a.SubjectUserId equals subject.Id into subjects
                from subject in subjects.DefaultIfEmpty()
                join actor in _userManager.Users.AsNoTracking() on a.ActorUserId equals actor.Id into actors
                from actor in actors.DefaultIfEmpty()
                select new
                {
                    Activity = a,
                    SubjectEmail = subject != null ? (subject.Email ?? subject.UserName) : null,
                    ActorEmail = actor != null ? (actor.Email ?? actor.UserName) : null
                };

            if (!string.IsNullOrWhiteSpace(actionFilter))
            {
                var act = actionFilter.Trim();
                baseQuery = baseQuery.Where(x => x.Activity.Action == act);
            }

            if (!string.IsNullOrWhiteSpace(q))
            {
                var term = q.Trim();
                baseQuery = baseQuery.Where(x =>
                    (x.Activity.Details ?? "").Contains(term) ||
                    (x.SubjectEmail ?? "").Contains(term) ||
                    (x.ActorEmail ?? "").Contains(term) ||
                    (x.Activity.IpAddress ?? "").Contains(term));
            }

            var totalCount = await baseQuery.CountAsync();

            var logs = await baseQuery
                .OrderByDescending(x => x.Activity.CreatedAtUtc)
                .ThenByDescending(x => x.Activity.UserActivityId)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(x => new SuperUserLogRowViewModel
                {
                    CreatedAtUtc = x.Activity.CreatedAtUtc,
                    Action = x.Activity.Action,
                    Details = x.Activity.Details,
                    SubjectUserId = x.Activity.SubjectUserId,
                    SubjectEmail = x.SubjectEmail,
                    ActorEmail = x.ActorEmail,
                    IpAddress = x.Activity.IpAddress
                })
                .ToListAsync();

            return View(new SuperUserLogsViewModel
            {
                Logs = logs,
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize,
                ActionFilter = actionFilter,
                Q = q
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
