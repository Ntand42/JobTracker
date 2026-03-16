using System;

namespace JobTracker.Models
{
    public class GlobalNotification
    {
        public int GlobalNotificationId { get; set; }
        public string Message { get; set; } = string.Empty;
        public string? CreatedByUserId { get; set; }
        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
        public DateTime StartsAtUtc { get; set; } = DateTime.UtcNow;
        public DateTime? EndsAtUtc { get; set; }
        public bool IsActive { get; set; } = true;
    }
}
