using Entities.Enums;
using Entities.Models.Subscriptions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Entities.Configurations
{
    public class SubscriptionPlanDefinitionConfiguration : IEntityTypeConfiguration<SubscriptionPlanDefinition>
    {
        // Deterministyczne seed Guidy — nie zmieniaj tych wartości
        private static readonly Guid FreePlanId       = new("00000001-0000-0000-0000-000000000001");
        private static readonly Guid StandardPlanId   = new("00000001-0000-0000-0000-000000000002");
        private static readonly Guid PremiumPlanId    = new("00000001-0000-0000-0000-000000000003");
        private static readonly Guid EnterprisePlanId = new("00000001-0000-0000-0000-000000000004");

        public void Configure(EntityTypeBuilder<SubscriptionPlanDefinition> builder)
        {
            builder.ToTable("SubscriptionPlanDefinitions");

            builder.HasKey(p => p.Id);

            builder.Property(p => p.Plan)
                .HasConversion<int>()
                .IsRequired();

            builder.HasIndex(p => p.Plan)
                .IsUnique();

            builder.Property(p => p.Name)
                .IsRequired()
                .HasMaxLength(128);

            builder.Property(p => p.MaxProjects)
                .IsRequired();

            builder.Property(p => p.MaxUsers)
                .IsRequired();

            builder.Property(p => p.Price)
                .HasColumnType("decimal(18,2)");

            builder.Property(p => p.Currency)
                .HasMaxLength(8)
                .HasDefaultValue("PLN");

            builder.Property(p => p.IsActive)
                .HasDefaultValue(true);

            // Seed danych
            builder.HasData(
                new SubscriptionPlanDefinition
                {
                    Id          = FreePlanId,
                    Plan        = SubscriptionPlan.Free,
                    Name        = "Free",
                    MaxProjects = 1,
                    MaxUsers    = 1,
                    Price       = 0.00m,
                    Currency    = "PLN",
                    IsActive    = true,
                    CreatedAt   = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc)
                },
                new SubscriptionPlanDefinition
                {
                    Id          = StandardPlanId,
                    Plan        = SubscriptionPlan.Standard,
                    Name        = "Standard",
                    MaxProjects = 5,
                    MaxUsers    = 10,
                    Price       = 0.00m,
                    Currency    = "PLN",
                    IsActive    = true,
                    CreatedAt   = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc)
                },
                new SubscriptionPlanDefinition
                {
                    Id          = PremiumPlanId,
                    Plan        = SubscriptionPlan.Premium,
                    Name        = "Premium",
                    MaxProjects = -1,
                    MaxUsers    = 50,
                    Price       = 0.00m,
                    Currency    = "PLN",
                    IsActive    = true,
                    CreatedAt   = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc)
                },
                new SubscriptionPlanDefinition
                {
                    Id          = EnterprisePlanId,
                    Plan        = SubscriptionPlan.Enterprise,
                    Name        = "Enterprise",
                    MaxProjects = -1,
                    MaxUsers    = -1,
                    Price       = 0.00m,
                    Currency    = "PLN",
                    IsActive    = true,
                    CreatedAt   = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc)
                }
            );
        }
    }
}
