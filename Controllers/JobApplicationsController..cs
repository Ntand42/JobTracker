using Microsoft.AspNetCore.Mvc;
using JobTracker.Data;
using JobTracker.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authorization;



namespace JobTracker.Controllers
{
    [Authorize]
    public class JobApplicationsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;


       public JobApplicationsController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
{
    _context = context;
    _userManager = userManager;
}


        // GET: JobApplications
        public async Task<IActionResult> Index(ApplicationStatus? status)
        {
            var userId = _userManager.GetUserId(User);

            var jobsQuery = _context.JobApplications
                .Where(j => j.UserId == userId);

            if (status.HasValue)
            {
                jobsQuery = jobsQuery.Where(j => j.Status == status.Value);
            }

            var jobs = await jobsQuery
                .OrderByDescending(j => j.AppliedDate)
                .ToListAsync();

            ViewData["CurrentStatus"] = status;

            return View(jobs);
        }


        // GET: JobApplications/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: JobApplications/Create
[HttpPost]
[ValidateAntiForgeryToken]
public async Task<IActionResult> Create(JobApplication jobApplication)
{
    if (ModelState.IsValid)
    {
        // Assign the logged-in user's Id
        var user = await _userManager.GetUserAsync(User);
        jobApplication.UserId = user?.Id;

        // Set default AppliedDate if not provided
        if (jobApplication.AppliedDate == default)
            jobApplication.AppliedDate = DateTime.Today;

       jobApplication.UserId = _userManager.GetUserId(User);

_context.Add(jobApplication);
await _context.SaveChangesAsync();

        return RedirectToAction(nameof(Index));
    }

    return View(jobApplication);
}




        public async Task<IActionResult> Details(int? id)
{
    if (id == null) return NotFound();

    var job = await _context.JobApplications.FirstOrDefaultAsync(m => m.JobApplicationId == id);
    if (job == null) return NotFound();

    return View(job);
}

public async Task<IActionResult> Edit(int? id)
{
    if (id == null) return NotFound();

    var jobApplication = await _context.JobApplications
        .FirstOrDefaultAsync(j => j.JobApplicationId == id);

    if (jobApplication == null) return NotFound();

    return View(jobApplication);
}

[HttpPost]
[ValidateAntiForgeryToken]
public async Task<IActionResult> Edit(int id, JobApplication jobApplication)
{
    if (id != jobApplication.JobApplicationId)
        return NotFound();

    if (!ModelState.IsValid)
        return View(jobApplication);

    var userId = _userManager.GetUserId(User);

    // Retrieve existing job AND ensure it belongs to the logged-in user
    var existingJob = await _context.JobApplications
        .FirstOrDefaultAsync(j =>
            j.JobApplicationId == id &&
            j.UserId == userId);

    if (existingJob == null)
        return NotFound();

    // Update only editable fields
    existingJob.CompanyName = jobApplication.CompanyName;
    existingJob.Position = jobApplication.Position;
    existingJob.JobType = jobApplication.JobType;
    existingJob.Notes = jobApplication.Notes;
    existingJob.JobLink = jobApplication.JobLink;
    existingJob.Status = jobApplication.Status;
    existingJob.AppliedDate = jobApplication.AppliedDate;

    try
    {
        await _context.SaveChangesAsync();
    }
    catch (DbUpdateConcurrencyException)
    {
        if (!JobApplicationExists(jobApplication.JobApplicationId))
            return NotFound();

        throw;
    }

    return RedirectToAction(nameof(Index));
}



// GET: JobApplications/Delete/5
public async Task<IActionResult> Delete(int? id)
{
    if (id == null) return NotFound();

    var jobApplication = await _context.JobApplications
        .FirstOrDefaultAsync(m => m.JobApplicationId == id);

    if (jobApplication == null) return NotFound();

    return View(jobApplication); // This expects Delete.cshtml
}

// POST: JobApplications/Delete/5
[HttpPost]
[ValidateAntiForgeryToken]
public async Task<IActionResult> Delete(int id)
{
    var job = await _context.JobApplications.FindAsync(id);

    if (job != null)
    {
        _context.JobApplications.Remove(job);
        await _context.SaveChangesAsync();
    }

    return RedirectToAction(nameof(Index));
}




private bool JobApplicationExists(int id)
{
    return _context.JobApplications.Any(e => e.JobApplicationId == id);
}



    }
}
