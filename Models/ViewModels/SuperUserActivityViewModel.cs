using System;
using System.Collections.Generic;

namespace JobTracker.Models.ViewModels
{
    public class SuperUserActivityRowViewModel
    {
        public DateTime CreatedAtUtc { get; set; }
        public string Action { get; set; } = string.Empty;
        public string? Details { get; set; }
        public string? ActorEmail { get; set; }
        public string? IpAddress { get; set; }
    }

    public class SuperUserActivityViewModel
    {
        public string SubjectUserId { get; set; } = string.Empty;
        public string SubjectEmail { get; set; } = string.Empty;
        public string SubjectName { get; set; } = string.Empty;
        public IReadOnlyList<SuperUserActivityRowViewModel> Activities { get; set; } = Array.Empty<SuperUserActivityRowViewModel>();
    }
}
