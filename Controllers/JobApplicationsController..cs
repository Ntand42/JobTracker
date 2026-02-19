using Microsoft.AspNetCore.Mvc;
using JobTracker.Data;
using JobTracker.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;



namespace JobTracker.Controllers
{
    public class JobApplicationsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;


       public JobApplicationsController(ApplicationDbContext context, UserManager<IdentityUser> userManager)
{
    _context = context;
    _userManager = userManager;
}


        // GET: JobApplications
        public IActionResult Index()
        {
            var jobs = _context.JobApplications.ToList();
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

    // Retrieve existing job from the database
    var existingJob = await _context.JobApplications
        .FirstOrDefaultAsync(j => j.JobApplicationId == id);

    if (existingJob == null)
        return NotFound();

    // Update only the editable fields
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
        else
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
