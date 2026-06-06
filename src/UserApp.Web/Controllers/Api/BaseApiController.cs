using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UserApp.Application.Common;
using UserApp.Domain.Common;

namespace UserApp.Web.Controllers.Api;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public abstract class BaseApiController<TEntity, TViewModel> : ControllerBase
    where TEntity : Entity<Guid>
    where TViewModel : class
{
    protected readonly IBaseService<TEntity> _service;
    protected readonly IMapper _mapper;

    protected BaseApiController(IBaseService<TEntity> service, IMapper mapper)
    {
        _service = service;
        _mapper = mapper;
    }

    // ---------------- GET ALL ----------------
    [HttpGet]
    public async Task<ActionResult<ApiResponse<object>>> GetAll(int page = 1, int pageSize = 10)
    {
        var items = await _service.ListAsync((page - 1) * pageSize, pageSize);
        var total = await _service.CountAsync();

        var vm = _mapper.Map<List<TViewModel>>(items);

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

        return Ok(ApiResponse<TViewModel>.Ok(vm, "Data retrieved successfully"));
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

        if (entity == null)
            return NotFound(ApiResponse<object>.Fail("Data not found"));

        _mapper.Map(vm, entity);
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
}