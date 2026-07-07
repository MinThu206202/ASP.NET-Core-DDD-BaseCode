using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using UserApp.Application.Common;
using UserApp.Application.Common.Interfaces;
using UserApp.Application.Common.Media;
using UserApp.Application.CommonTables.Interfaces;
using UserApp.Web.ViewModels;

namespace UserApp.Web.Controllers;

public abstract partial class BaseController<TEntity, TViewModel> : Controller
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

    protected Dictionary<string, string> DisplayFieldForEntity { get; } = new();

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
        await ResolveRelationDisplayNames(items);
        await ResolvePivotSelectedIds(items);

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
        await ResolveRelationDisplayNames([vm]);
        await ResolvePivotSelectedIds([vm]);
        await LoadChildDataAsync(id);

        return View("Details", vm);
    }

    public virtual async Task<IActionResult> Create()
    {
        var vm = new TViewModel();
        await PopulateLookupOptions(vm);
        await PopulateRelationOptions(vm);
        return View("Create", vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public virtual async Task<IActionResult> Create(TViewModel vm, List<IFormFile>? files = null)
    {
        if (!ValidateModel(vm) || !ValidateFiles(files))
        {
            await PopulateLookupOptions(vm);
            await PopulateRelationOptions(vm);
            return View(vm);
        }

        var entity = _mapper.Map<TEntity>(vm);

        try
        {
            await _service.AddAsync(entity, files);
            await SavePivotData(entity, vm);
        }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError("files", ex.Message);
            await PopulateLookupOptions(vm);
            await PopulateRelationOptions(vm);
            return View(vm);
        }

        await SetFlashMessageAsync("Create");
        return RedirectToAction(nameof(Index));
    }

    public virtual async Task<IActionResult> Edit(Guid id)
    {
        var entity = await _service.GetByIdAsync(id);
        if (entity == null) return NotFound();

        var vm = _mapper.Map<TViewModel>(entity);
        await LoadImageUrls(vm, id);
        await PopulateLookupOptions(vm);
        await PopulateRelationOptions(vm);
        await PopulatePivotSelectedIds(vm, id);

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
            await PopulateRelationOptions(vm);
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
            await SavePivotData(entity, vm);
        }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError("files", ex.Message);
            await LoadImageUrls(vm, id);
            await PopulateLookupOptions(vm);
            await PopulateRelationOptions(vm);
            return View("Edit", vm);
        }

        await SetFlashMessageAsync("Edit");
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

        try
        {
            await _service.RemoveAsync(entity);
        }
        catch (DbUpdateException)
        {
            TempData["Error"] = "Cannot delete this record because it is referenced by other records. Remove or update the dependent records first.";
            return RedirectToAction(nameof(Index));
        }

        await SetFlashMessageAsync("Delete");
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Restore(Guid id)
    {
        await _service.RestoreAsync(id);
        TempData["Success"] = "Record restored successfully.";
        return RedirectToAction(nameof(Index));
    }
}
