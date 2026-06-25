using System.Reflection;
using System.Text.Encodings.Web;
using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using UserApp.Application.AuditLogs.Interfaces;
using UserApp.Application.Common;
using UserApp.Web.ViewModels;
using UserApp.Web.ViewModels.AuditLogs;

namespace UserApp.Web.Controllers;

public class AuditLogController : Controller
{
    private readonly IAuditLogService _service;
    private readonly IMapper _mapper;
    private readonly IServiceProvider _serviceProvider;

    public AuditLogController(IAuditLogService service, IMapper mapper, IServiceProvider serviceProvider)
    {
        _service = service;
        _mapper = mapper;
        _serviceProvider = serviceProvider;
    }

    public async Task<IActionResult> Index(string? search = null, int page = 1, int size = 10)
    {
        List<Domain.AuditLogs.AuditLog> items;
        int total;

        if (string.IsNullOrWhiteSpace(search))
        {
            items = await _service.ListAsync((page - 1) * size, size);
            total = await _service.CountAsync();
        }
        else
        {
            items = await _service.SearchAsync(search, (page - 1) * size, size);
            total = await _service.CountSearchAsync(search);
        }

        var vm = new ListViewModel<AuditLogViewModel>
        {
            Items = _mapper.Map<List<AuditLogViewModel>>(items),
            Page = page,
            PageSize = size,
            TotalCount = total,
            SearchTerm = search ?? string.Empty
        };
        return View(vm);
    }

    [HttpGet]
    public async Task<IActionResult> SearchData(string? search, int page = 1, int size = 10)
    {
        List<Domain.AuditLogs.AuditLog> items;
        int total;

        if (string.IsNullOrWhiteSpace(search))
        {
            items = await _service.ListAsync((page - 1) * size, size);
            total = await _service.CountAsync();
        }
        else
        {
            items = await _service.SearchAsync(search, (page - 1) * size, size);
            total = await _service.CountSearchAsync(search);
        }

        var vm = new ListViewModel<AuditLogViewModel>
        {
            Items = _mapper.Map<List<AuditLogViewModel>>(items),
            Page = page,
            PageSize = size,
            TotalCount = total,
            SearchTerm = search ?? string.Empty
        };

        return Json(new
        {
            tableHtml = await RenderPartialViewToString("_AuditLogTable", vm),
            paginationHtml = await RenderPartialViewToString("_AuditLogPagination", vm)
        });
    }

    private async Task<string> RenderPartialViewToString(string viewName, object model)
    {
        ViewData.Model = model;
        using var sw = new StringWriter();
        var engine = HttpContext.RequestServices.GetRequiredService<ICompositeViewEngine>();
        var viewResult = engine.FindView(ControllerContext, viewName, isMainPage: false);
        if (viewResult.View == null) return string.Empty;
        var viewContext = new ViewContext(
            ControllerContext,
            viewResult.View,
            ViewData,
            TempData,
            sw,
            new HtmlHelperOptions()
        );
        await viewResult.View.RenderAsync(viewContext);
        return sw.ToString();
    }

    public async Task<IActionResult> Details(Guid id)
    {
        var entity = await _service.GetByIdAsync(id);
        if (entity == null) return NotFound();

        var vm = _mapper.Map<AuditLogViewModel>(entity);
        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Restore(Guid id)
    {
        var auditLog = await _service.GetByIdAsync(id);
        if (auditLog == null) return NotFound();

        var service = ResolveEntityService(auditLog.EntityName);
        if (service == null) return NotFound();

        var restoreMethod = typeof(IBaseService<>)
            .MakeGenericType(service.GetType().GetInterfaces()
                .First(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IBaseService<>))
                .GetGenericArguments()[0])
            .GetMethod(nameof(IBaseService<object>.RestoreAsync));

        if (restoreMethod == null) return NotFound();

        await (Task)restoreMethod.Invoke(service, [Guid.Parse(auditLog.EntityId)])!;

        TempData["Success"] = $"{auditLog.EntityName} restored successfully.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Revert(Guid id)
    {
        var auditLog = await _service.GetByIdAsync(id);
        if (auditLog == null) return NotFound();

        if (string.IsNullOrEmpty(auditLog.OldValues))
        {
            TempData["Warning"] = "No previous values available to revert for this entry.";
            return RedirectToAction(nameof(Index));
        }

        var service = ResolveEntityService(auditLog.EntityName);
        if (service == null) return NotFound();

        var revertMethod = typeof(IBaseService<>)
            .MakeGenericType(service.GetType().GetInterfaces()
                .First(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IBaseService<>))
                .GetGenericArguments()[0])
            .GetMethod(nameof(IBaseService<object>.RevertFromAuditAsync));

        if (revertMethod == null) return NotFound();

        try
        {
            await (Task)revertMethod.Invoke(service, [id])!;
        }
        catch (Exception)
        {
            TempData["Warning"] = "Could not revert. The record may have been deleted or changed.";
            return RedirectToAction(nameof(Index));
        }

        TempData["Success"] = $"{auditLog.EntityName} reverted to previous values.";
        return RedirectToAction(nameof(Index));
    }

    private object? ResolveEntityService(string entityName)
    {
        var domainAssembly = typeof(Domain.Common.Entity<Guid>).Assembly;
        var entityType = domainAssembly.GetTypes()
            .FirstOrDefault(t => t.Name == entityName && !t.IsAbstract && t.IsSubclassOf(typeof(Domain.Common.Entity<Guid>)));

        if (entityType == null) return null;

        var serviceType = typeof(IBaseService<>).MakeGenericType(entityType);
        return _serviceProvider.GetService(serviceType);
    }
}
