using System;

namespace JobTracker.Models
{
    public class UserActivity
    {
        public int UserActivityId { get; set; }
        public string? ActorUserId { get; set; }
        public string SubjectUserId { get; set; } = string.Empty;
        public string Action { get; set; } = string.Empty;
        public string? Details { get; set; }
        public string? IpAddress { get; set; }
        public string? UserAgent { get; set; }
        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    }
}
