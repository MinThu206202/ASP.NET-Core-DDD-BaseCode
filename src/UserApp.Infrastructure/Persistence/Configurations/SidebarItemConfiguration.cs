using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using UserApp.Domain.SidebarItems;

namespace UserApp.Infrastructure.Persistence.Configurations;

public class SidebarItemConfiguration : IEntityTypeConfiguration<SidebarItem>
{
    public void Configure(EntityTypeBuilder<SidebarItem> builder)
    {
        builder.ToTable("SidebarItems");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.ModuleName).HasMaxLength(100).IsRequired();
        builder.Property(x => x.ControllerName).HasMaxLength(100).IsRequired();
        builder.Property(x => x.AreaName).HasMaxLength(100);
        builder.Property(x => x.IconSvg).HasMaxLength(500);
        builder.HasOne(x => x.Group)
            .WithMany(x => x.Items)
            .HasForeignKey(x => x.GroupId)
            .OnDelete(DeleteBehavior.Cascade);
    
    }
}
