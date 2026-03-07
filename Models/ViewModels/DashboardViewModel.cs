using System.Collections.Generic;
using JobTracker.Models;

namespace JobTracker.Models.ViewModels
{
    public class DashboardViewModel
    {
        public int TotalApplications { get; set; }
        public int AppliedCount { get; set; }
        public int InterviewCount { get; set; }
        public int OfferCount { get; set; }
        public int RejectedCount { get; set; }
        public List<JobApplication> RecentApplications { get; set; } = new List<JobApplication>();
        public List<JobApplication> UpcomingInterviews { get; set; } = new List<JobApplication>();
        public List<JobApplication> PendingFollowUps { get; set; } = new List<JobApplication>();

        // Analytics
        public Dictionary<string, int> ApplicationsPerMonth { get; set; } = new Dictionary<string, int>();
        public double SuccessRate { get; set; }
        public double ResponseRate { get; set; }
        public double AverageResponseTimeDays { get; set; }
    }
}