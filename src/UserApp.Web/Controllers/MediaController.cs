using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using UserApp.Application.Common.Interfaces;
using UserApp.Application.Common.Media;

public class MediaController : Controller
{
    private readonly IMediaService _mediaService;

    public MediaController(IMediaService mediaService)
    {
        _mediaService = mediaService;
    }

    [HttpPost]
    public async Task<IActionResult> Upload(string entityName, Guid entityId, IFormFile file)
    {
        using var ms = new MemoryStream();
        await file.CopyToAsync(ms);

        var input = new MediaFileInput
        {
            FileName = file.FileName,
            ContentType = file.ContentType,
            Data = ms.ToArray()
        };

        await _mediaService.UploadAsync(entityName, entityId, input);

        return Ok();
    }
}