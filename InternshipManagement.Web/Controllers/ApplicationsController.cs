using Microsoft.AspNetCore.Mvc;

namespace InternshipManagement.Web.Controllers
{
    public class ApplicationsController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
