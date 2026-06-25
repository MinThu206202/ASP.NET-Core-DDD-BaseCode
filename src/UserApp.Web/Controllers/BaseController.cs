using System.Reflection;
using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using UserApp.Application.Common;
using UserApp.Application.Common.Interfaces;
using UserApp.Application.Common.Media;
using UserApp.Application.CommonTables.Interfaces;
using UserApp.Domain.CommonTables;
using UserApp.Infrastructure.Persistence;
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

    private string GetDisplayFieldForEntity(string entityName)
    {
        return DisplayFieldForEntity.TryGetValue(entityName, out var field) ? field : "Name";
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

    private async Task PopulateRelationOptions(object vm)
    {
        var vmType = vm.GetType();
        var sp = HttpContext?.RequestServices;
        if (sp == null) return;

        foreach (var prop in vmType.GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            if (prop.PropertyType != typeof(List<SelectListItem>) || !prop.CanWrite) continue;

            var fieldName = prop.Name.EndsWith("Options") ? prop.Name[..^"Options".Length] : null;
            if (string.IsNullOrEmpty(fieldName)) continue;

            // Pivot: Selected{Name}Ids exists → multi-select
            var selectedProp = vmType.GetProperty($"Selected{fieldName}Ids");
            if (selectedProp != null && selectedProp.PropertyType == typeof(List<Guid>))
            {
                var options = await LoadEntityLookupOptions(fieldName);
                prop.SetValue(vm, options);
                continue;
            }

            // Single relation: {Name}Id exists → dropdown
            var idProp = vmType.GetProperty($"{fieldName}Id");
            if (idProp == null) continue;
            var idType = idProp.PropertyType;
            if (idType != typeof(Guid) && idType != typeof(Guid?)) continue;

            var entityOptions = await LoadEntityLookupOptions(fieldName);
            prop.SetValue(vm, entityOptions);
        }
    }

    private async Task PopulatePivotSelectedIds(object vm, Guid entityId)
    {
        var vmType = vm.GetType();
        var sp = HttpContext?.RequestServices;
        if (sp == null) return;

        var entityName = typeof(TEntity).Name;

        foreach (var prop in vmType.GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            if (prop.PropertyType != typeof(List<Guid>) || !prop.CanWrite) continue;
            if (!prop.Name.StartsWith("Selected") || !prop.Name.EndsWith("Ids")) continue;

            var fieldName = prop.Name["Selected".Length..^"Ids".Length];
            if (string.IsNullOrEmpty(fieldName)) continue;

            var selectedIds = await LoadPivotRelatedIds(entityName, fieldName, entityId);
            prop.SetValue(vm, selectedIds);
        }
    }

    private async Task<List<string>> LoadEntityNamesByIds(string entityName, List<Guid> ids)
    {
        try
        {
            if (ids.Count == 0) return [];

            var sp = HttpContext?.RequestServices;
            if (sp == null) return [];

            var entityType = Type.GetType($"UserApp.Domain.{entityName}s.{entityName}, UserApp.Domain");
            if (entityType == null) return [];

            var db = sp.GetRequiredService<AppDbContext>();

            var setMethod = typeof(DbContext).GetMethod("Set", Type.EmptyTypes)!
                .MakeGenericMethod(entityType);
            var dbSet = setMethod.Invoke(db, null);

            var toList = typeof(Enumerable).GetMethod("ToList")!
                .MakeGenericMethod(entityType);
            var all = (IEnumerable<object>)toList.Invoke(null, [dbSet])!;

            var idProp = entityType.GetProperty("Id");
            var displayField = GetDisplayFieldForEntity(entityName);
            var nameProp = entityType.GetProperty(displayField);
            if (idProp == null || nameProp == null) return [];

            return all
                .Where(e => ids.Contains((Guid)idProp.GetValue(e)!))
                .Select(e => nameProp.GetValue(e)?.ToString() ?? "")
                .Where(n => !string.IsNullOrEmpty(n))
                .ToList();
        }
        catch
        {
            return [];
        }
    }

    private async Task<List<Guid>> LoadPivotRelatedIds(string moduleName, string relatedName, Guid entityId)
    {
        try
        {
            var db = HttpContext?.RequestServices.GetService<AppDbContext>();
            if (db == null) return new();

            var pivotName = $"{moduleName}{relatedName}";
            var pivotType = Type.GetType($"UserApp.Domain.{pivotName}s.{pivotName}, UserApp.Domain");
            if (pivotType == null) return new();

            var setMethod = typeof(DbContext).GetMethod("Set", Type.EmptyTypes)!
                .MakeGenericMethod(pivotType);
            var dbSet = setMethod.Invoke(db, null);

            var toList = typeof(Enumerable).GetMethod("ToList")!
                .MakeGenericMethod(pivotType);
            var all = (IEnumerable<object>)toList.Invoke(null, [dbSet])!;

            var parentIdProp = pivotType.GetProperty($"{moduleName}_id");
            var relatedIdProp = pivotType.GetProperty($"{relatedName}_id");
            if (parentIdProp == null || relatedIdProp == null) return new();

            return all
                .Where(e => (Guid)parentIdProp.GetValue(e)! == entityId)
                .Select(e => (Guid)relatedIdProp.GetValue(e)!)
                .ToList();
        }
        catch
        {
            return new();
        }
    }

    private async Task SavePivotData<T>(T entity, TViewModel vm) where T : class
    {
        var sp = HttpContext?.RequestServices;
        if (sp == null) return;

        var entityType = typeof(T);
        var idPropEntity = entityType.GetProperty("Id");
        if (idPropEntity == null) return;
        var entityId = (Guid)idPropEntity.GetValue(entity)!;

        var entityName = typeof(TEntity).Name;
        var vmType = typeof(TViewModel);

        foreach (var prop in vmType.GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            if (prop.PropertyType != typeof(List<Guid>) || !prop.CanRead) continue;
            if (!prop.Name.StartsWith("Selected") || !prop.Name.EndsWith("Ids")) continue;

            var fieldName = prop.Name["Selected".Length..^"Ids".Length];
            if (string.IsNullOrEmpty(fieldName)) continue;

            var selectedIds = (List<Guid>)prop.GetValue(vm)!;
            await SyncPivotRecords(entityName, fieldName, entityId, selectedIds);
        }
    }

    private async Task SyncPivotRecords(string moduleName, string relatedName, Guid entityId, List<Guid> selectedIds)
    {
        try
        {
            var db = HttpContext?.RequestServices.GetService<AppDbContext>();
            if (db == null) return;

            var pivotName = $"{moduleName}{relatedName}";
            var pivotType = Type.GetType($"UserApp.Domain.{pivotName}s.{pivotName}, UserApp.Domain");
            if (pivotType == null) return;

            var parentIdProp = pivotType.GetProperty($"{moduleName}_id");
            var relatedIdProp = pivotType.GetProperty($"{relatedName}_id");
            if (parentIdProp == null || relatedIdProp == null) return;

            var setMethod = typeof(DbContext).GetMethod("Set", Type.EmptyTypes)!
                .MakeGenericMethod(pivotType);
            var dbSet = setMethod.Invoke(db, null);

            var toList = typeof(Enumerable).GetMethod("ToList")!
                .MakeGenericMethod(pivotType);
            var all = (IEnumerable<object>)toList.Invoke(null, [dbSet])!;

            var existing = all
                .Where(e => (Guid)parentIdProp.GetValue(e)! == entityId)
                .ToList();

            var toRemove = existing
                .Where(e => !selectedIds.Contains((Guid)relatedIdProp.GetValue(e)!))
                .ToList();

            var toAdd = selectedIds
                .Where(id => !existing.Any(e => (Guid)relatedIdProp.GetValue(e)! == id))
                .ToList();

            foreach (var record in toRemove)
                db.Remove(record);

            foreach (var addId in toAdd)
            {
                var record = Activator.CreateInstance(pivotType)!;
                parentIdProp.SetValue(record, entityId);
                relatedIdProp.SetValue(record, addId);
                db.Add(record);
            }

            await db.SaveChangesAsync();
        }
        catch
        {
            // Silently handle pivot save errors
        }
    }

    private async Task<List<SelectListItem>> LoadEntityLookupOptions(string entityName)
    {
        try
        {
            var sp = HttpContext?.RequestServices;
            if (sp == null) return new();

            var entityType = Type.GetType($"UserApp.Domain.{entityName}s.{entityName}, UserApp.Domain");
            if (entityType == null) return new();

            var serviceType = typeof(IBaseService<>).MakeGenericType(entityType);
            var service = sp.GetService(serviceType);
            if (service == null) return new();

            var listMethod = serviceType.GetMethod("ListAsync", new[] { typeof(int), typeof(int) });
            if (listMethod == null) return new();

            var task = (Task)listMethod.Invoke(service, new object[] { 0, 9999 })!;
            await task.ConfigureAwait(false);

            var resultProperty = task.GetType().GetProperty("Result");
            if (resultProperty == null) return new();
            var entities = (IEnumerable<object>)resultProperty.GetValue(task)!;

            var idProp = entityType.GetProperty("Id");
            var displayField = GetDisplayFieldForEntity(entityName);
            var nameProp = entityType.GetProperty(displayField);

            return entities.Select(e => new SelectListItem
            {
                Value = idProp?.GetValue(e)?.ToString() ?? "",
                Text = nameProp?.GetValue(e)?.ToString() ?? e.ToString() ?? ""
            }).ToList();
        }
        catch
        {
            return new();
        }
    }

    private async Task ResolveRelationDisplayNames(List<TViewModel> items)
    {
        var vmType = typeof(TViewModel);

        foreach (var item in items)
        {
            foreach (var nameProp in vmType.GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                if (nameProp.PropertyType != typeof(string) || !nameProp.CanWrite) continue;
                if (!nameProp.Name.EndsWith("Name")) continue;

                var fieldName = nameProp.Name[..^"Name".Length];
                var idProp = vmType.GetProperty($"{fieldName}Id");
                if (idProp == null) continue;
                var idType = idProp.PropertyType;
                if (idType != typeof(Guid) && idType != typeof(Guid?)) continue;

                var idValue = idProp.GetValue(item);
                if (idValue == null || (Guid)idValue == Guid.Empty) continue;

                var entityName = fieldName;
                var displayName = await LoadEntityDisplayName(entityName, (Guid)idValue);
                if (displayName != null)
                    nameProp.SetValue(item, displayName);
            }
        }
    }

    private async Task<string?> LoadEntityDisplayName(string entityName, Guid id)
    {
        try
        {
            var sp = HttpContext?.RequestServices;
            if (sp == null) return null;

            var entityType = Type.GetType($"UserApp.Domain.{entityName}s.{entityName}, UserApp.Domain");
            if (entityType == null) return null;

            var serviceType = typeof(IBaseService<>).MakeGenericType(entityType);
            var service = sp.GetService(serviceType);
            if (service == null) return null;

            var getMethod = serviceType.GetMethod("GetByIdAsync", new[] { typeof(Guid) });
            if (getMethod == null) return null;

            var task = (Task)getMethod.Invoke(service, new object[] { id })!;
            await task.ConfigureAwait(false);

            var resultProperty = task.GetType().GetProperty("Result");
            if (resultProperty == null) return null;
            var entity = resultProperty.GetValue(task);
            if (entity == null) return null;

            var displayField = GetDisplayFieldForEntity(entityName);
            var nameProp = entityType.GetProperty(displayField);
            return nameProp?.GetValue(entity)?.ToString();
        }
        catch
        {
            return null;
        }
    }

    private async Task ResolvePivotSelectedIds(List<TViewModel> items)
    {
        var vmType = typeof(TViewModel);
        var entityName = typeof(TEntity).Name;

        foreach (var item in items)
        {
            foreach (var prop in vmType.GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                if (prop.PropertyType != typeof(List<Guid>) || !prop.CanWrite) continue;
                if (!prop.Name.StartsWith("Selected") || !prop.Name.EndsWith("Ids")) continue;

                var fieldName = prop.Name["Selected".Length..^"Ids".Length];
                if (string.IsNullOrEmpty(fieldName)) continue;

                var idProp = vmType.GetProperty("Id");
                var idValue = idProp?.GetValue(item);
                if (idValue == null || (Guid)idValue == Guid.Empty) continue;

                var selectedIds = await LoadPivotRelatedIds(entityName, fieldName, (Guid)idValue);
                prop.SetValue(item, selectedIds);

                var displayProp = vmType.GetProperty($"{fieldName}Display");
                if (displayProp != null && displayProp.PropertyType == typeof(string) && displayProp.CanWrite)
                {
                    var names = await LoadEntityNamesByIds(fieldName, selectedIds);
                    displayProp.SetValue(item, string.Join(", ", names));
                }
            }
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

    private async Task LoadChildDataAsync(Guid entityId)
    {
        var sp = HttpContext?.RequestServices;
        if (sp == null) return;

        var entityName = typeof(TEntity).Name;
        var domainAssembly = typeof(TEntity).Assembly;
        var childData = new List<Dictionary<string, object>>();

        foreach (var childType in domainAssembly.GetTypes())
        {
            if (!childType.IsClass || childType.IsAbstract) continue;
            if (childType == typeof(TEntity)) continue;
            if (childType.Namespace == null || !childType.Namespace.StartsWith("UserApp.Domain")) continue;
            if (childType.Namespace == "UserApp.Domain.Common") continue;

            var fkProp = childType.GetProperty($"{entityName}Id", BindingFlags.Public | BindingFlags.Instance);
            if (fkProp == null) continue;
            if (fkProp.PropertyType != typeof(Guid) && fkProp.PropertyType != typeof(Guid?)) continue;

            var serviceType = typeof(IBaseService<>).MakeGenericType(childType);
            var service = sp.GetService(serviceType);
            if (service == null) continue;

            var listMethod = serviceType.GetMethod("ListAsync", new[] { typeof(int), typeof(int) });
            if (listMethod == null) continue;

            var task = (Task)listMethod.Invoke(service, new object[] { 0, 99999 })!;
            await task.ConfigureAwait(false);
            var resultProp = task.GetType().GetProperty("Result");
            var allRecords = resultProp?.GetValue(task) as IEnumerable<object>;
            if (allRecords == null) continue;

            var filtered = new List<object>();
            foreach (var record in allRecords)
            {
                var fkVal = fkProp.GetValue(record);
                if (fkVal != null && (Guid)fkVal == entityId)
                    filtered.Add(record);
            }

            if (filtered.Count == 0) continue;

            var idProp = childType.GetProperty("Id");
            var records = new List<Dictionary<string, string>>();
            foreach (var record in filtered)
            {
                var fields = new Dictionary<string, string>
                {
                    ["Id"] = idProp?.GetValue(record)?.ToString() ?? ""
                };
                foreach (var p in childType.GetProperties(BindingFlags.Public | BindingFlags.Instance))
                {
                    if (p.Name == "Id") continue;
                    if (p.Name == "IsDeleted") continue;
                    if (p.Name == "CreatedAt") continue;
                    if (p.Name == "UpdatedAt") continue;
                    if (p.Name == "DeletedAt") continue;
                    if (typeof(System.Collections.IEnumerable).IsAssignableFrom(p.PropertyType) && p.PropertyType != typeof(string)) continue;
                    if (p.PropertyType.IsClass && p.PropertyType != typeof(string)) continue;
                    if (p.PropertyType == typeof(Guid) || p.PropertyType == typeof(Guid?)) continue;
                    if (p.PropertyType == typeof(DateTime) || p.PropertyType == typeof(DateTime?)) continue;
                    if (p.PropertyType == typeof(byte[])) continue;

                    var val = p.GetValue(record);
                    fields[p.Name] = val?.ToString() ?? "";
                }
                records.Add(fields);
            }

            childData.Add(new Dictionary<string, object>
            {
                ["EntityName"] = childType.Name,
                ["ControllerName"] = childType.Name,
                ["Records"] = records
            });

            // Load image URLs for child records if supported
            var imageMethod = serviceType.GetMethod("GetImageUrlsAsync", new[] { typeof(Guid) });
            if (imageMethod != null)
            {
                foreach (var record in records)
                {
                    try
                    {
                        var recordId = Guid.Parse(record["Id"]);
                        var imgTask = (Task)imageMethod.Invoke(service, new object[] { recordId })!;
                        await imgTask.ConfigureAwait(false);
                        var imgResultProp = imgTask.GetType().GetProperty("Result");
                        var urls = imgResultProp?.GetValue(imgTask) as List<string>;
                        if (urls != null && urls.Count > 0)
                            record["Image"] = urls[0];
                    }
                    catch
                    {
                    }
                }
            }
        }

        ViewData["ChildData"] = childData;
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

    private async Task SetFlashMessageAsync(string action)
    {
        var entityName = typeof(TEntity).Name;
        var service = CommonTableService;
        if (service != null)
        {
            var all = await service.ListAsync(0, 999);
            var entry = all.FirstOrDefault(x => x.Type == "FlashMessage" && x.Code == $"{entityName}{action}");
            if (entry != null)
            {
                TempData["Success"] = entry.Name;
                return;
            }
        }
        TempData["Success"] = $"{entityName} {action.ToLower()} successfully";
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
