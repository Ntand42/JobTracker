using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using JobTracker.Models;


namespace JobTracker.Data;



public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
        
    }

    public DbSet<JobApplication> JobApplications { get; set; }
    public DbSet<UserActivity> UserActivities { get; set; }
    public DbSet<GlobalNotification> GlobalNotifications { get; set; }

}
