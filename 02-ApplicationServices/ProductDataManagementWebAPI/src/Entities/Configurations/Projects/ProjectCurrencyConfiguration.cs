using Entities.Models.Projects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Entities.Configurations.Projects
{
    public class ProjectCurrencyConfiguration : IEntityTypeConfiguration<ProjectCurrency>
    {
        public void Configure(EntityTypeBuilder<ProjectCurrency> builder)
        {
            builder.Property(x => x.Code)
                .IsRequired()
                .HasMaxLength(10);

            builder.Property(x => x.Name)
                .IsRequired()
                .HasMaxLength(100);

            builder.Property(x => x.Symbol)
                .HasMaxLength(10);
        }
    }
}
