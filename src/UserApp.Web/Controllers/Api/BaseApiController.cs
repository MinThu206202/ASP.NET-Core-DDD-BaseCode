using System.Reflection;
using AutoMapper;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using UserApp.Application.Common;
using UserApp.Application.Common.Interfaces;
using UserApp.Application.Common.Media;
using UserApp.Application.CommonTables.Interfaces;
using UserApp.Domain.Common;
using UserApp.Domain.CommonTables;

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

    // ---------------- GET ALL ----------------
    [HttpGet]
    public async Task<ActionResult<ApiResponse<object>>> GetAll(int page = 1, int pageSize = 10)
    {
        var items = await _service.ListAsync((page - 1) * pageSize, pageSize);
        var total = await _service.CountAsync();

        var vm = _mapper.Map<List<TViewModel>>(items);

        await ResolveLookupDisplayNames(vm);

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

    foreach (var file in files)
    {
        if (file.Length == 0) continue;

        using var mem = new MemoryStream();
        await file.CopyToAsync(mem);

        var input = new MediaFileInput
        {
            FileName = file.FileName,
            ContentType = file.ContentType,
            Data = mem.ToArray()
        };

        try
        {
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
}