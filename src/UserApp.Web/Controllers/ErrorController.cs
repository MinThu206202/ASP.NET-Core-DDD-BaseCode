using Microsoft.AspNetCore.Mvc;
using UserApp.Web.ViewModels;

namespace UserApp.Web.Controllers;

[Route("Error")]
public class ErrorController : Controller
{
    private readonly ILogger<ErrorController> _logger;

    public ErrorController(ILogger<ErrorController> logger)
    {
        _logger = logger;
    }

    [HttpGet("{statusCode}")]
    public IActionResult HandleError(int statusCode)
    {
        var message = statusCode switch
        {
            404 => "Page not found",
            403 => "Access denied",
            401 => "Unauthorized",
            _ => "An unexpected error occurred"
        };

        var viewModel = new ErrorViewModel
        {
            Message = message,
            RequestId = HttpContext.TraceIdentifier
        };

        return View("Error", viewModel);
    }
}