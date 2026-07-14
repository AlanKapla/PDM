using Entities.Models.Projects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Entities.Configurations.Projects
{
    public class ProjectUnitConfiguration : IEntityTypeConfiguration<ProjectUnit>
    {
        public void Configure(EntityTypeBuilder<ProjectUnit> builder)
        {
            builder.Property(x => x.Code)
                .IsRequired()
                .HasMaxLength(20);

            builder.Property(x => x.Name)
                .IsRequired()
                .HasMaxLength(100);

            builder.Property(x => x.Symbol)
                .HasMaxLength(10);
        }
    }
}
