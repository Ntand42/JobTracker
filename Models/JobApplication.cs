using System;
using System.ComponentModel.DataAnnotations;

namespace JobTracker.Models
{
    public class JobApplication
    {
        public int JobApplicationId { get; set; }

        [Required]
        public string CompanyName { get; set; }

        [Required]
        public string Position { get; set; }

        public string? JobType { get; set; }

        [DataType(DataType.Date)]
        public DateTime AppliedDate { get; set; } = DateTime.Today;

        [DataType(DataType.Date)]
        public DateTime? InterviewDate { get; set; }

        [DataType(DataType.Date)]
        public DateTime? FollowUpDate { get; set; }

        [DataType(DataType.Date)]
        public DateTime? OutcomeDate { get; set; }

        public string? Notes { get; set; }

        public string? JobLink { get; set; }

        public string? UserId { get; set; }

        [Required]
        public ApplicationStatus Status { get; set; } = ApplicationStatus.Applied;
    }
}