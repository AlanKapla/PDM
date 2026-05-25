using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Entities.Migrations
{
    /// <inheritdoc />
    public partial class AddSubscriptionBilling : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "GracePeriodDays",
                table: "TenantSubscriptions",
                type: "int",
                nullable: false,
                defaultValue: 7);

            migrationBuilder.AddColumn<DateTime>(
                name: "GracePeriodEndsAt",
                table: "TenantSubscriptions",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "LastPaidAmount",
                table: "TenantSubscriptions",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastPaidAt",
                table: "TenantSubscriptions",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "NextPaymentDue",
                table: "TenantSubscriptions",
                type: "datetime2",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "SubscriptionNotifications",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Type = table.Column<int>(type: "int", nullable: false),
                    RecipientEmail = table.Column<string>(type: "varchar(256)", nullable: false),
                    Subject = table.Column<string>(type: "varchar(512)", nullable: false),
                    Body = table.Column<string>(type: "text", nullable: false),
                    SentAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SubscriptionNotifications", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SubscriptionPayments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantSubscriptionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Plan = table.Column<int>(type: "int", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Currency = table.Column<string>(type: "varchar(8)", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    PeriodStart = table.Column<DateTime>(type: "datetime2", nullable: false),
                    PeriodEnd = table.Column<DateTime>(type: "datetime2", nullable: false),
                    PaidAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ExternalTransactionId = table.Column<string>(type: "varchar(256)", nullable: true),
                    FailureReason = table.Column<string>(type: "varchar(1024)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SubscriptionPayments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SubscriptionPayments_TenantSubscriptions_TenantSubscriptionId",
                        column: x => x.TenantSubscriptionId,
                        principalTable: "TenantSubscriptions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SubscriptionNotifications_TenantId_Type_SentAt",
                table: "SubscriptionNotifications",
                columns: new[] { "TenantId", "Type", "SentAt" });

            migrationBuilder.CreateIndex(
                name: "IX_SubscriptionPayments_TenantId",
                table: "SubscriptionPayments",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_SubscriptionPayments_TenantSubscriptionId_Status",
                table: "SubscriptionPayments",
                columns: new[] { "TenantSubscriptionId", "Status" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SubscriptionNotifications");

            migrationBuilder.DropTable(
                name: "SubscriptionPayments");

            migrationBuilder.DropColumn(
                name: "GracePeriodDays",
                table: "TenantSubscriptions");

            migrationBuilder.DropColumn(
                name: "GracePeriodEndsAt",
                table: "TenantSubscriptions");

            migrationBuilder.DropColumn(
                name: "LastPaidAmount",
                table: "TenantSubscriptions");

            migrationBuilder.DropColumn(
                name: "LastPaidAt",
                table: "TenantSubscriptions");

            migrationBuilder.DropColumn(
                name: "NextPaymentDue",
                table: "TenantSubscriptions");
        }
    }
}
