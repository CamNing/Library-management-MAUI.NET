using Microsoft.AspNetCore.Mvc;

namespace LibraryAPI.Helpers
{
    public class StringHelper : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
