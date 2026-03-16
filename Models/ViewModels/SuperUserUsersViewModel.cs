using System;
using System.Collections.Generic;
using JobTracker.Models;

namespace JobTracker.Models.ViewModels
{
    public class SuperUserPlatformStatsViewModel
    {
        public int TotalUsers { get; set; }
        public int TotalApplications { get; set; }
        public int TotalInterviews { get; set; }
        public int TotalOffers { get; set; }
        public int ApplicationsThisMonth { get; set; }
        public double AverageApplicationsPerUser { get; set; }
        public string CurrentMonthLabel { get; set; } = string.Empty;
    }

    public class SuperUserMostActiveUserViewModel
    {
        public string UserId { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public int TotalApplications { get; set; }
    }

    public class SuperUserUserRowViewModel
    {
        public string Id { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public bool EmailConfirmed { get; set; }
        public bool IsDisabled { get; set; }
        public DateTimeOffset? LockoutEnd { get; set; }
        public IReadOnlyList<string> Roles { get; set; } = Array.Empty<string>();
    }

    public class SuperUserUsersViewModel
    {
        public IReadOnlyList<SuperUserUserRowViewModel> Users { get; set; } = Array.Empty<SuperUserUserRowViewModel>();
        public SuperUserPlatformStatsViewModel Stats { get; set; } = new SuperUserPlatformStatsViewModel();
        public IReadOnlyList<SuperUserMostActiveUserViewModel> MostActiveUsers { get; set; } = Array.Empty<SuperUserMostActiveUserViewModel>();
    }

    public class SuperUserApplicationRowViewModel
    {
        public int JobApplicationId { get; set; }
        public string CompanyName { get; set; } = string.Empty;
        public string Position { get; set; } = string.Empty;
        public ApplicationStatus Status { get; set; }
        public DateTime AppliedDate { get; set; }
        public string? UserId { get; set; }
        public string UserEmail { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
    }

    public class SuperUserApplicationsViewModel
    {
        public IReadOnlyList<SuperUserApplicationRowViewModel> Applications { get; set; } = Array.Empty<SuperUserApplicationRowViewModel>();
        public int TotalCount { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public ApplicationStatus? Status { get; set; }
        public string? Q { get; set; }
    }

    public class SuperUserNotificationRowViewModel
    {
        public int GlobalNotificationId { get; set; }
        public string Message { get; set; } = string.Empty;
        public DateTime CreatedAtUtc { get; set; }
        public DateTime StartsAtUtc { get; set; }
        public DateTime? EndsAtUtc { get; set; }
        public bool IsActive { get; set; }
    }

    public class SuperUserNotificationsViewModel
    {
        public string? Message { get; set; }
        public DateTime? StartsAtUtc { get; set; }
        public DateTime? EndsAtUtc { get; set; }
        public IReadOnlyList<SuperUserNotificationRowViewModel> Notifications { get; set; } = Array.Empty<SuperUserNotificationRowViewModel>();
    }

    public class SuperUserLogRowViewModel
    {
        public DateTime CreatedAtUtc { get; set; }
        public string Action { get; set; } = string.Empty;
        public string? Details { get; set; }
        public string SubjectUserId { get; set; } = string.Empty;
        public string? SubjectEmail { get; set; }
        public string? ActorEmail { get; set; }
        public string? IpAddress { get; set; }
    }

    public class SuperUserLogsViewModel
    {
        public IReadOnlyList<SuperUserLogRowViewModel> Logs { get; set; } = Array.Empty<SuperUserLogRowViewModel>();
        public int TotalCount { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public string? ActionFilter { get; set; }
        public string? Q { get; set; }
    }
}
