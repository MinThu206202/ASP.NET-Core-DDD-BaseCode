using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using UserApp.Application.AuditLogs.Interfaces;
using UserApp.Web.ViewModels;
using UserApp.Web.ViewModels.AuditLogs;

namespace UserApp.Web.Controllers;

public class AuditLogController : Controller
{
    private readonly IAuditLogService _service;
    private readonly IMapper _mapper;

    public AuditLogController(IAuditLogService service, IMapper mapper)
    {
        _service = service;
        _mapper = mapper;
    }

    public async Task<IActionResult> Index(int page = 1, int size = 20)
    {
        var items = await _service.ListAsync((page - 1) * size, size);
        var total = await _service.CountAsync();
        var vm = new ListViewModel<AuditLogViewModel>
        {
            Items = _mapper.Map<List<AuditLogViewModel>>(items),
            Page = page,
            PageSize = size,
            TotalCount = total
        };
        return View(vm);
    }

    public async Task<IActionResult> Details(Guid id)
    {
        var entity = await _service.GetByIdAsync(id);
        if (entity == null) return NotFound();

        var vm = _mapper.Map<AuditLogViewModel>(entity);
        return View(vm);
    }
}
