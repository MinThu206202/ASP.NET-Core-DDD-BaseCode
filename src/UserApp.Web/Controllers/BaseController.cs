using System.Reflection;
using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.DependencyInjection;
using UserApp.Application.Common;
using UserApp.Application.Common.Interfaces;
using UserApp.Application.Common.Media;
using UserApp.Application.CommonTables.Interfaces;
using UserApp.Domain.CommonTables;
using UserApp.Web.Common;
using UserApp.Web.ViewModels;
using System.Security.Claims;

namespace UserApp.Web.Controllers;

public abstract class BaseController<TEntity, TViewModel> : Controller
    where TEntity : class
    where TViewModel : class, new()
{
    protected readonly IBaseService<TEntity> _service;
    protected readonly IMapper _mapper;
    protected readonly IMediaService? _mediaService;

    protected readonly IPermissionChecker? _permissionService;
    private IMediaService? MediaService =>
        _mediaService ?? HttpContext?.RequestServices.GetService<IMediaService>();

    private ICommonTableService? CommonTableService =>
        HttpContext?.RequestServices.GetService<ICommonTableService>();

    protected BaseController(
        IBaseService<TEntity> service,
        IMapper mapper,
        IMediaService? mediaService = null,
        IPermissionChecker? permissionService = null)
    {
        _service = service;
        _mapper = mapper;
        _mediaService = mediaService;
        _permissionService = permissionService;
    }

    private async Task LoadImageUrls(object vm, Guid? entityId = null)
    {
        var mediaService = MediaService;
        if (mediaService == null) return;

        var idProp = vm.GetType().GetProperty("Id");
        if (idProp == null) return;

        var id = entityId ?? (Guid)idProp.GetValue(vm)!;
        var media = await mediaService.GetAsync(typeof(TEntity).Name, id);

        var imgProp = vm.GetType().GetProperty("ImageUrls");
        if (imgProp != null)
        {
            var urls = media.Select(x => x.Url).ToList();
            imgProp.SetValue(vm, urls);
        }

        var mediaProp = vm.GetType().GetProperty("MediaList");
        if (mediaProp != null)
        {
            mediaProp.SetValue(vm, media);
        }
    }

    private async Task ResolveLookupDisplayNames(List<TViewModel> items)
    {
        var service = CommonTableService;
        if (service == null) return;

        var entityName = typeof(TEntity).Name;
        var vmType = typeof(TViewModel);
        var all = await service.ListAsync(0, 999);

        foreach (var item in items)
        {
            foreach (var prop in vmType.GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                if (prop.PropertyType != typeof(string) || !prop.CanRead) continue;

                var fieldName = prop.Name;
                var nameProp = vmType.GetProperty($"{fieldName}Name", BindingFlags.Public | BindingFlags.Instance);
                if (nameProp == null || !nameProp.CanWrite || nameProp.PropertyType != typeof(string)) continue;

                var code = prop.GetValue(item) as string;
                if (string.IsNullOrEmpty(code)) continue;

                var type = $"{entityName}{fieldName}";
                var displayName = all.FirstOrDefault(x => x.Type == type && x.Code == code)?.Name;
                if (displayName != null)
                    nameProp.SetValue(item, displayName);
            }
        }
    }

    private async Task PopulateLookupOptions(object vm)
    {
        var service = CommonTableService;
        if (service == null) return;

        var entityName = typeof(TEntity).Name;
        var vmType = vm.GetType();
        var all = await service.ListAsync(0, 999);

        foreach (var prop in vmType.GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            if (prop.PropertyType != typeof(List<SelectListItem>) || !prop.CanWrite) continue;

            var fieldName = prop.Name.EndsWith("Options") ? prop.Name[..^"Options".Length] : null;
            if (string.IsNullOrEmpty(fieldName)) continue;

            var type = $"{entityName}{fieldName}";
            var options = all
                .Where(x => x.Type == type)
                .OrderBy(x => x.Name)
                .Select(x => new SelectListItem { Value = x.Code, Text = x.Name })
                .ToList();

            prop.SetValue(vm, options);
        }
    }

    public virtual async Task<IActionResult> Index(int page = 1, int size = 10)
    {
        var data = await _service.ListAsync((page - 1) * size, size);
        var totalCount = await _service.CountAsync();

        var items = _mapper.Map<List<TViewModel>>(data);

        foreach (var item in items)
        {
            await LoadImageUrls(item);
        }

        await ResolveLookupDisplayNames(items);

        return View("Index", new ListViewModel<TViewModel>
        {
            Page = page,
            PageSize = size,
            TotalCount = totalCount,
            Items = items
        });
    }

    public async Task<IActionResult> Details(Guid id)
    {
        var entity = await _service.GetByIdAsync(id);
        if (entity == null) return NotFound();

        var vm = _mapper.Map<TViewModel>(entity);
        await LoadImageUrls(vm, id);
        await ResolveLookupDisplayNames([vm]);

        return View("Details", vm);
    }

    public virtual async Task<IActionResult> Create()
    {
        var vm = new TViewModel();
        await PopulateLookupOptions(vm);
        return View("Create", vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public virtual async Task<IActionResult> Create(TViewModel vm, List<IFormFile>? files = null)
    {
        if (!ValidateModel(vm) || !ValidateFiles(files))
        {
            await PopulateLookupOptions(vm);
            return View(vm);
        }

        var entity = _mapper.Map<TEntity>(vm);

        try
        {
            await _service.AddAsync(entity, files);
        }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError("files", ex.Message);
            await PopulateLookupOptions(vm);
            return View(vm);
        }

        return RedirectToAction(nameof(Index));
    }

    public virtual async Task<IActionResult> Edit(Guid id)
    {
        var entity = await _service.GetByIdAsync(id);
        if (entity == null) return NotFound();

        var vm = _mapper.Map<TViewModel>(entity);
        await LoadImageUrls(vm, id);
        await PopulateLookupOptions(vm);

        return View("Edit", vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public virtual async Task<IActionResult> Edit(Guid id, TViewModel vm, List<IFormFile>? files = null)
    {
        if (!ModelState.IsValid || !ValidateFiles(Request.Form.Files))
        {
            await LoadImageUrls(vm, id);
            await PopulateLookupOptions(vm);
            return View("Edit", vm);
        }

        var entity = await _service.GetByIdAsync(id);
        if (entity == null) return NotFound();

        _mapper.Map(vm, entity);

        try
        {
            var mediaService = MediaService;
            if (mediaService != null)
            {
                var entityName = typeof(TEntity).Name;
                foreach (var formFile in Request.Form.Files)
                {
                    if (!formFile.Name.StartsWith("replace_") || formFile.Length == 0) continue;

                    var mediaIdStr = formFile.Name["replace_".Length..];
                    if (!Guid.TryParse(mediaIdStr, out var mediaId)) continue;

                    await mediaService.DeleteAsync(mediaId);

                    using var ms = new MemoryStream();
                    await formFile.CopyToAsync(ms);
                    var input = new MediaFileInput
                    {
                        FileName = formFile.FileName,
                        ContentType = formFile.ContentType,
                        Data = ms.ToArray()
                    };
                    await mediaService.UploadAsync(entityName, id, input);
                }
            }

            await _service.UpdateAsync(entity, files);
        }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError("files", ex.Message);
            await LoadImageUrls(vm, id);
            await PopulateLookupOptions(vm);
            return View("Edit", vm);
        }

        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Delete(Guid id)
    {
        var entity = await _service.GetByIdAsync(id);
        if (entity == null) return NotFound();

        var vm = _mapper.Map<TViewModel>(entity);
        return View("Delete", vm);
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(Guid id)
    {
        var entity = await _service.GetByIdAsync(id);
        if (entity == null) return NotFound();

        await _service.RemoveAsync(entity);

        return RedirectToAction(nameof(Index));
    }

    protected bool ValidateModel<T>(T model)
    {
        var results = DynamicValidator.Validate(model);

        foreach (var error in results)
        {
            foreach (var member in error.MemberNames)
            {
                ModelState.AddModelError(member, error.ErrorMessage!);
            }
        }

        return ModelState.IsValid;
    }

    private bool ValidateFiles(IEnumerable<IFormFile>? files)
    {
        if (files == null || !files.Any()) return true;

        const int maxSize = 5 * 1024 * 1024;
        var allowedTypes = new[] { "image/jpeg", "image/png", "image/webp" };

        foreach (var file in files)
        {
            if (file.Length > maxSize)
            {
                ModelState.AddModelError("files", "Image size must be 5 MB or smaller.");
                return false;
            }

            if (!allowedTypes.Contains(file.ContentType.ToLowerInvariant()))
            {
                ModelState.AddModelError("files", "Only JPG, PNG, and WEBP files are allowed.");
                return false;
            }
        }

        return true;
    }

    protected async Task<bool> HasPermission(string permission)
    {
        if (_permissionService == null)
            return true;

        var userId = HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null) return false;

        return await _permissionService.HasPermissionAsync(
            Guid.Parse(userId),
            permission
        );
    }
}
