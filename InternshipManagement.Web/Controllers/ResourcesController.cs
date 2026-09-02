using Microsoft.AspNetCore.Mvc;

namespace InternshipManagement.Web.Controllers
{
    public class ResourcesController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }

        public IActionResult InternshipGuide()
        {
            return View();
        }

        public IActionResult InternshipTips()
        {
            return View();
        }

        public IActionResult CVTips()
        {
            return View();
        }

        public IActionResult ApplicationTips()
        {
            return View();
        }

        public IActionResult InterviewTips()
        {
            return View();
        }

        public IActionResult CareerAdvice()
        {
            return View();
        }
    }
}