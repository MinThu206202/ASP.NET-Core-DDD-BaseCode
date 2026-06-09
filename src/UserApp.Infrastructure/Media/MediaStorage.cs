using Microsoft.AspNetCore.Http;

namespace UserApp.Infrastructure.Media;

public class MediaStorage
{
    private readonly string _rootPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads");

    public MediaStorage()
    {
        Directory.CreateDirectory(_rootPath);
    }

    public async Task<string> SaveAsync(IFormFile file)
    {
        var fileName = Guid.NewGuid() + Path.GetExtension(file.FileName);
        var fullPath = Path.Combine(_rootPath, fileName);

        using var stream = new FileStream(fullPath, FileMode.Create);
        await file.CopyToAsync(stream);

        // return relative path (important for web access)
        return "/uploads/" + fileName;
    }
}