using Entities.Models.Projects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Entities.Configurations.Projects
{
    public class ProjectParamsConfiguration : IEntityTypeConfiguration<ProjectParams>
    {
        public void Configure(EntityTypeBuilder<ProjectParams> builder)
        {
            builder.ToTable("ProjectParams");

            builder.HasDiscriminator<string>("ParamType")
                .HasValue<ProjectCurrency>("Currency")
                .HasValue<ProjectUnit>("Unit");

            builder.HasOne(x => x.Project)
                .WithMany(p => p.Params)
                .HasForeignKey(x => x.ProjectId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasIndex(x => x.ProjectId);
            // Unique tylko dla Currency — Unit może mieć wiele wierszy per projekt
            builder.HasIndex("ProjectId", "ParamType")
                .IsUnique()
                .HasFilter("[ParamType] = 'Currency'");
        }
    }
}
