using Entities.Models.Projects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Entities.Configurations.Projects
{
    public class ProjectCostCategoryConfiguration : IEntityTypeConfiguration<ProjectCostCategory>
    {
        public void Configure(EntityTypeBuilder<ProjectCostCategory> builder)
        {
            builder.Property(x => x.Name)
                .IsRequired()
                .HasMaxLength(100);

            builder.Property(x => x.Code)
                .HasMaxLength(20);

            builder.Property(x => x.Color)
                .HasMaxLength(7);

            builder.HasIndex(x => new { x.ProjectId, x.Code })
                .IsUnique()
                .HasFilter("[ProjectCostCategory_Code] IS NOT NULL");
        }
    }
}
