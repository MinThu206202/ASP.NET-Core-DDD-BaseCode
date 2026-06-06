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
    where TEntity : Entity<Guid>   // ✅ IMPORTANT FIX
    where TViewModel : class
{
    protected readonly IBaseService<TEntity> _service;
    protected readonly IMapper _mapper;

    protected BaseApiController(IBaseService<TEntity> service, IMapper mapper)
    {
        _service = service;
        _mapper = mapper;
    }

    // GET ALL
    [HttpGet]
    public async Task<ActionResult> GetAll(int page = 1, int pageSize = 10)
    {
        var items = await _service.ListAsync((page - 1) * pageSize, pageSize);
        var total = await _service.CountAsync();

        var vm = _mapper.Map<List<TViewModel>>(items);

        return Ok(new
        {
            Page = page,
            PageSize = pageSize,
            TotalCount = total,
            Items = vm
        });
    }

    // GET BY ID
    [HttpGet("{id}")]
    public async Task<ActionResult<TViewModel>> Get(Guid id)
    {
        var entity = await _service.GetByIdAsync(id);
        if (entity == null) return NotFound();

        return Ok(_mapper.Map<TViewModel>(entity));
    }

    // CREATE
    [HttpPost]
    public async Task<ActionResult<TViewModel>> Create([FromBody] TViewModel vm)
    {
        var entity = _mapper.Map<TEntity>(vm);

        await _service.AddAsync(entity);
        await _service.SaveAsync();

        // ✅ FIXED: no dynamic
        return CreatedAtAction(nameof(Get), new { id = entity.Id }, vm);
    }

    // UPDATE
    [HttpPut("{id}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] TViewModel vm)
    {
        var entity = await _service.GetByIdAsync(id);
        if (entity == null) return NotFound();

        _mapper.Map(vm, entity);
        await _service.UpdateAsync(entity);

        return NoContent();
    }

    // DELETE
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var entity = await _service.GetByIdAsync(id);
        if (entity == null) return NotFound();

        await _service.RemoveAsync(entity);
        return NoContent();
    }
}