using System.Reflection;
using AutoMapper;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using UserApp.Application.Common;
using UserApp.Application.Common.Interfaces;
using UserApp.Application.Common.Media;
using UserApp.Application.CommonTables.Interfaces;
using UserApp.Domain.Common;
using UserApp.Domain.CommonTables;
using UserApp.Infrastructure.Persistence;

namespace UserApp.Web.Controllers.Api;

[ApiController]
[Route("api/[controller]")]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
public abstract class BaseApiController<TEntity, TViewModel> : ControllerBase
    where TEntity : Entity<Guid>
    where TViewModel : class
{
    protected readonly IBaseService<TEntity> _service;
    protected readonly IMapper _mapper;
    private readonly IMediaService? _mediaService;

    private IMediaService? MediaService =>
        _mediaService ?? HttpContext?.RequestServices.GetService<IMediaService>();

    private ICommonTableService? CommonTableService =>
        HttpContext?.RequestServices.GetService<ICommonTableService>();

    protected BaseApiController(IBaseService<TEntity> service, IMapper mapper, IMediaService? mediaService = null)
    {
        _service = service;
        _mapper = mapper;
        _mediaService = mediaService;
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

    private async Task PopulateLookupOptions(TViewModel vm)
    {
        var service = CommonTableService;
        if (service == null) return;

        var entityName = typeof(TEntity).Name;
        var vmType = typeof(TViewModel);
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

    private async Task PopulateRelationOptions(TViewModel vm)
    {
        var vmType = typeof(TViewModel);
        var sp = HttpContext?.RequestServices;
        if (sp == null) return;

        foreach (var prop in vmType.GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            if (prop.PropertyType != typeof(List<SelectListItem>) || !prop.CanWrite) continue;

            var fieldName = prop.Name.EndsWith("Options") ? prop.Name[..^"Options".Length] : null;
            if (string.IsNullOrEmpty(fieldName)) continue;

            var selectedProp = vmType.GetProperty($"Selected{fieldName}Ids");
            if (selectedProp != null && selectedProp.PropertyType == typeof(List<Guid>))
            {
                var options = await LoadEntityLookupOptions(fieldName);
                prop.SetValue(vm, options);
                continue;
            }

            var idProp = vmType.GetProperty($"{fieldName}Id");
            if (idProp == null) continue;
            var idType = idProp.PropertyType;
            if (idType != typeof(Guid) && idType != typeof(Guid?)) continue;

            var entityOptions = await LoadEntityLookupOptions(fieldName);
            prop.SetValue(vm, entityOptions);
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

                var displayName = await LoadEntityDisplayName(fieldName, (Guid)idValue);
                if (displayName != null)
                    nameProp.SetValue(item, displayName);
            }
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
            var nameProp = entityType.GetProperty("Name");

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

            var nameProp = entityType.GetProperty("Name");
            return nameProp?.GetValue(entity)?.ToString();
        }
        catch
        {
            return null;
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
            var nameProp = entityType.GetProperty("Name");
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

    private async Task SavePivotData(TEntity entity, TViewModel vm)
    {
        var sp = HttpContext?.RequestServices;
        if (sp == null) return;

        var idPropEntity = typeof(TEntity).GetProperty("Id");
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
        }
    }

    // ---------------- GET ALL ----------------
    [HttpGet]
    public async Task<ActionResult<ApiResponse<object>>> GetAll(int page = 1, int pageSize = 10)
    {
        var items = await _service.ListAsync((page - 1) * pageSize, pageSize);
        var total = await _service.CountAsync();

        var vm = _mapper.Map<List<TViewModel>>(items);

        await ResolveLookupDisplayNames(vm);
        await ResolveRelationDisplayNames(vm);
        await ResolvePivotSelectedIds(vm);

        return Ok(ApiResponse<object>.Ok(new
        {
            page,
            pageSize,
            totalCount = total,
            items = vm
        }, "Data retrieved successfully"));
    }

    // ---------------- GET BY ID ----------------
    [HttpGet("{id}")]
    public async Task<ActionResult<ApiResponse<TViewModel>>> Get(Guid id)
    {
        var entity = await _service.GetByIdAsync(id);

        if (entity == null)
            return NotFound(ApiResponse<TViewModel>.Fail("Data not found"));

        var vm = _mapper.Map<TViewModel>(entity);
        await ResolveLookupDisplayNames([vm]);
        await ResolveRelationDisplayNames([vm]);
        await ResolvePivotSelectedIds([vm]);

        return Ok(ApiResponse<TViewModel>.Ok(vm, "Data retrieved successfully"));
    }

    // ---------------- GET OPTIONS ----------------
    [HttpGet("options")]
    public async Task<ActionResult<ApiResponse<object>>> GetOptions()
    {
        var vm = Activator.CreateInstance(typeof(TViewModel)) as TViewModel;
        if (vm == null)
            return Ok(ApiResponse<object>.Ok(new Dictionary<string, object>(), "No options available"));

        await PopulateLookupOptions(vm);
        await PopulateRelationOptions(vm);

        var optionsDict = new Dictionary<string, object>();
        var vmType = typeof(TViewModel);

        foreach (var prop in vmType.GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            if (prop.PropertyType != typeof(List<SelectListItem>) || !prop.CanRead) continue;

            var options = prop.GetValue(vm) as List<SelectListItem>;
            if (options != null && options.Count > 0)
            {
                var fieldName = prop.Name.EndsWith("Options") ? prop.Name[..^"Options".Length] : prop.Name;
                optionsDict[fieldName] = options.Select(o => new { value = o.Value, label = o.Text });
            }
        }

        return Ok(ApiResponse<object>.Ok(optionsDict, "Options retrieved successfully"));
    }

    // ---------------- CREATE ----------------
    [HttpPost]
    public async Task<ActionResult<ApiResponse<TViewModel>>> Create([FromBody] TViewModel vm)
    {
        var entity = _mapper.Map<TEntity>(vm);

        await _service.AddAsync(entity);
        await _service.SaveAsync();

        await SavePivotData(entity, vm);

        var resultVm = _mapper.Map<TViewModel>(entity);

        return CreatedAtAction(
            nameof(Get),
            new { id = entity.Id },
            ApiResponse<TViewModel>.Ok(resultVm, "Created successfully")
        );
    }

    [HttpPost("with-media")]
    [RequestSizeLimit(10 * 1024 * 1024)]
    public async Task<ActionResult<ApiResponse<TViewModel>>> CreateWithMedia([FromForm] TViewModel vm, List<IFormFile> files)
    {
        var validationError = ValidateApiFiles(files);
        if (validationError != null)
            return BadRequest(ApiResponse<object>.Fail(validationError));

        var entity = _mapper.Map<TEntity>(vm);

        await _service.AddAsync(entity, files);
        await _service.SaveAsync();

        await SavePivotData(entity, vm);

        var resultVm = _mapper.Map<TViewModel>(entity);

        return CreatedAtAction(
            nameof(Get),
            new { id = entity.Id },
            ApiResponse<TViewModel>.Ok(resultVm, "Created successfully")
        );
    }

    // ---------------- UPDATE ----------------
    [HttpPut("{id}")]
    public async Task<ActionResult<ApiResponse<object>>> Update(Guid id, [FromBody] TViewModel vm)
    {
        var entity = await _service.GetByIdAsync(id);
        if (entity == null) return NotFound(ApiResponse<object>.Fail("Data not found"));

        _mapper.Map(vm, entity);

        // 2. Use Reflection to set ID, bypassing access modifiers
        var prop = typeof(TEntity).GetProperty("Id");
        if (prop != null && prop.CanWrite)
        {
            prop.SetValue(entity, id);
        }
        else
        {
            // Fallback: If PropertyInfo failed, check if base class has it
            var baseProp = typeof(Entity<Guid>).GetProperty("Id");
            baseProp?.SetValue(entity, id);
        }

        await _service.UpdateAsync(entity);
        await _service.SaveAsync();

        await SavePivotData(entity, vm);

        return Ok(ApiResponse<object>.Ok(null, "Updated successfully"));
    }

    // ---------------- DELETE ----------------
    [HttpDelete("{id}")]
    public async Task<ActionResult<ApiResponse<object>>> Delete(Guid id)
    {
        var entity = await _service.GetByIdAsync(id);

        if (entity == null)
            return NotFound(ApiResponse<object>.Fail("Data not found"));

        await _service.RemoveAsync(entity);
        await _service.SaveAsync();

        return Ok(ApiResponse<object>.Ok(null, "Deleted successfully"));
    }

    // ---------------- MEDIA UPLOAD ----------------
    [HttpPost("{id}/media")]
[RequestSizeLimit(10 * 1024 * 1024)]
public async Task<ActionResult<ApiResponse<object>>> UploadMedia(Guid id, List<IFormFile> files)
{
    var entity = await _service.GetByIdAsync(id);
    if (entity == null)
        return NotFound(ApiResponse<object>.Fail("Data not found"));

    var ms = MediaService;
    if (ms == null)
        return BadRequest(ApiResponse<object>.Fail("Media upload not supported for this entity"));

    var validationError = ValidateApiFiles(files);
    if (validationError != null)
        return BadRequest(ApiResponse<object>.Fail(validationError));

    foreach (var file in files)
    {
        if (file.Length == 0) continue;

        try
        {
            using var mem = new MemoryStream();
            await file.CopyToAsync(mem);

            var input = new MediaFileInput
            {
                FileName = file.FileName,
                ContentType = file.ContentType,
                Data = mem.ToArray()
            };

            await ms.UploadAsync(typeof(TEntity).Name, id, input);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResponse<object>.Fail(ex.Message));
        }
    }

    return Ok(ApiResponse<object>.Ok(null, "Media uploaded successfully"));
}

    // ---------------- GET MEDIA ----------------
    [HttpGet("{id}/media")]
    public async Task<ActionResult<ApiResponse<List<object>>>> GetMedia(Guid id)
    {
        var entity = await _service.GetByIdAsync(id);
        if (entity == null)
            return NotFound(ApiResponse<object>.Fail("Data not found"));

        var ms = MediaService;
        if (ms == null)
            return Ok(ApiResponse<List<object>>.Ok(new List<object>(), "No media support"));

        var mediaList = await ms.GetAsync(typeof(TEntity).Name, id);

        return Ok(ApiResponse<List<object>>.Ok(
            mediaList.Select(m => new { m.Id, m.Url, m.OriginalName } as object).ToList(),
            "Media retrieved successfully"));
    }

    // ---------------- DELETE MEDIA ----------------
    [HttpDelete("{id}/media/{mediaId}")]
    public async Task<ActionResult<ApiResponse<object>>> DeleteMedia(Guid id, Guid mediaId)
    {
        var entity = await _service.GetByIdAsync(id);
        if (entity == null)
            return NotFound(ApiResponse<object>.Fail("Data not found"));

        var ms = MediaService;
        if (ms == null)
            return BadRequest(ApiResponse<object>.Fail("Media upload not supported for this entity"));

        await ms.DeleteAsync(mediaId);

        return Ok(ApiResponse<object>.Ok(null, "Media deleted successfully"));
    }

    private static string? ValidateApiFiles(IEnumerable<IFormFile> files)
    {
        const int maxSize = 5 * 1024 * 1024;
        var allowedTypes = new[] { "image/jpeg", "image/png", "image/webp" };

        foreach (var file in files)
        {
            if (file.Length > maxSize)
                return "Image size must be 5 MB or smaller.";

            if (!allowedTypes.Contains(file.ContentType.ToLowerInvariant()))
                return "Only JPG, PNG, and WEBP files are allowed.";
        }

        return null;
    }
}