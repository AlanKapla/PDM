using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Entities.Models;

namespace Entities.Configurations
{
    public class RoleConfiguration : IEntityTypeConfiguration<Role>
    {
        public void Configure(EntityTypeBuilder<Role> builder)
        {
            builder.HasKey(r => r.Id);

            builder.HasIndex(r => new { r.Scope, r.Code }).IsUnique();
            builder.HasIndex(r => r.Scope);
            builder.HasIndex(r => r.IsActive);

            builder.Property(r => r.Code).IsRequired().HasMaxLength(100);
            builder.Property(r => r.Name).IsRequired().HasMaxLength(200);
            builder.Property(r => r.Description).HasMaxLength(500);
            builder.Property(r => r.Scope).HasConversion<string>().IsRequired();
            builder.Property(r => r.IsBuiltIn).HasDefaultValue(false);
            builder.Property(r => r.IsActive).HasDefaultValue(true);
            builder.Property(r => r.CreatedAt).IsRequired();
            builder.Property(r => r.UpdatedAt).IsRequired();
        }
    }
}
