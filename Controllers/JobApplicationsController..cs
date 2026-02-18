using Microsoft.AspNetCore.Mvc;
using JobTracker.Data;
using JobTracker.Models;

namespace JobTracker.Controllers
{
    public class JobApplicationsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public JobApplicationsController(ApplicationDbContext context)
        {
            _context = context;
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
        public IActionResult Create(JobApplication jobApplication)
        {
            if (ModelState.IsValid)
            {
                _context.JobApplications.Add(jobApplication);
                _context.SaveChanges();
                return RedirectToAction(nameof(Index));
            }
            return View(jobApplication);
        }
    }
}
