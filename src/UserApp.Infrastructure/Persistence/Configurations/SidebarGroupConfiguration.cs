using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using UserApp.Domain.SidebarGroups;

namespace UserApp.Infrastructure.Persistence.Configurations;

public class SidebarGroupConfiguration : IEntityTypeConfiguration<SidebarGroup>
{
    public void Configure(EntityTypeBuilder<SidebarGroup> builder)
    {
        builder.ToTable("SidebarGroups");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Name).HasMaxLength(100).IsRequired();
        builder.Property(x => x.IconSvg).HasMaxLength(500);
    }
}
