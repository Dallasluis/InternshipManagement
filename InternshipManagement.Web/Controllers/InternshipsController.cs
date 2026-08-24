using InternshipManagement.Web.Models.Internship;
using InternshipManagement.Web.Services;
using Microsoft.AspNetCore.Mvc;

namespace InternshipManagement.Web.Controllers
{
    public class InternshipsController : Controller
    {
        private readonly IInternshipApiClient _internshipApiClient;

        public InternshipsController(IInternshipApiClient internshipApiClient)
        {
            _internshipApiClient = internshipApiClient;
        }

        [HttpGet]
        public async Task<IActionResult> Index(InternshipSearchViewModel filters)
        {
            if (filters.PageNumber < 1) filters.PageNumber = 1;
            if (filters.PageSize < 1) filters.PageSize = 12;

            var result = await _internshipApiClient.SearchAsync(filters);

            ViewBag.Filters = filters;
            return View(result);
        }

        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var internship = await _internshipApiClient.GetByIdAsync(id);

            if (internship == null)
                return NotFound();

            return View(internship);
        }
    }
}