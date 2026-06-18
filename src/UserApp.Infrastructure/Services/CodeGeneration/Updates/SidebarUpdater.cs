using System;
using Microsoft.AspNetCore.Routing;
using UserApp.Application.Common;
using UserApp.Domain.SidebarItems;

namespace UserApp.Infrastructure.Services.CodeGeneration.Updates;

public class SidebarUpdater
{
    public void Add(string moduleName, Guid groupId)
    {
        var repo = ServiceProviderAccessor.Current?.GetService(typeof(ISidebarItemRepository)) as ISidebarItemRepository;
        if (repo == null) return;

        var item = new SidebarItem
        {
            ModuleName = moduleName,
            ControllerName = moduleName,
            GroupId = groupId,
            DisplayOrder = 0,
            IsActive = true
        };

        repo.AddAsync(item).GetAwaiter().GetResult();
        repo.SaveChangesAsync().GetAwaiter().GetResult();
    }
}
