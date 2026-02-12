using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace JobTracker.Models
{
    public class JobApplication
    {
        [Required]
        public int JobApplicationId { get; set; }

        [Required]
        public string CompanyName { get; set; }

        [Required]
        public string JobTitle { get; set; }

        [Required]
        public string JobType { get; set; }

        [Required]
        public string Status { get; set; }

        [DataType(DataType.Date)]
        public DateTime ApplicationDate { get; set; }

        public string Notes { get; set; }

        public string JobLink { get; set; }

        [Required]
        public string UserId { get; set; }
    }
}
