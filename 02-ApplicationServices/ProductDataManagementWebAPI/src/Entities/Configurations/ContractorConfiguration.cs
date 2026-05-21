using Entities.Models.Tenants;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Entities.Configurations
{
    public class ContractorConfiguration : IEntityTypeConfiguration<Contractor>
    {
        public void Configure(EntityTypeBuilder<Contractor> builder)
        {
            builder.ToTable("Contractors");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.Id)
                .HasDefaultValueSql("NEWSEQUENTIALID()");

            builder.Property(x => x.Name)
                .IsRequired()
                .HasMaxLength(500);

            builder.Property(x => x.TaxId)
                .HasMaxLength(50);

            builder.Property(x => x.Email)
                .HasMaxLength(200);

            builder.Property(x => x.PhoneNumber)
                .HasMaxLength(20);

            builder.Property(x => x.Street)
                .HasMaxLength(300);

            builder.Property(x => x.City)
                .HasMaxLength(100);

            builder.Property(x => x.PostalCode)
                .HasMaxLength(20);

            builder.Property(x => x.Country)
                .HasMaxLength(100);

            builder.Property(x => x.Notes)
                .HasMaxLength(2000);

            builder.HasOne(x => x.Tenant)
                .WithMany()
                .HasForeignKey(x => x.TenantId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasIndex(x => x.TenantId);

            builder.HasIndex(x => new { x.TenantId, x.Name })
                .HasFilter("[IsDeleted] = 0");
        }
    }
}
