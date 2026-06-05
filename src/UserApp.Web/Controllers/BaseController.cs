using Microsoft.AspNetCore.Mvc;
using UserApp.Web.ViewModels;

namespace UserApp.Web.Controllers;

public abstract class BaseController : Controller
{
    protected IActionResult RenderError(string message)
    {
        var model = new ErrorViewModel
        {
            Message = message,
            RequestId = HttpContext.TraceIdentifier
        };

        return View("Error", model);
    }
}
