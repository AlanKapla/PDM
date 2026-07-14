using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Entities.Migrations
{
    /// <inheritdoc />
    public partial class migration1 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Chats",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    IsGroupChat = table.Column<bool>(type: "bit", nullable: false),
                    ProjectId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Chats", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Tenants",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Tenants", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Email = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    FirstName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    LastName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    AzureAdB2CObjectId = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    SystemRole = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PhoneNumber = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    CompanyName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    TaxId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Street = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    City = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    PostalCode = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    Country = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ChatMembers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ChatId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    JoinedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastReadAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsAdmin = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChatMembers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ChatMembers_Chats_ChatId",
                        column: x => x.ChatId,
                        principalTable: "Chats",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MessageHistories",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ChatId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Content = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EditedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ReplyToMessageId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MessageHistories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MessageHistories_Chats_ChatId",
                        column: x => x.ChatId,
                        principalTable: "Chats",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_MessageHistories_MessageHistories_ReplyToMessageId",
                        column: x => x.ReplyToMessageId,
                        principalTable: "MessageHistories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Contractors",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWSEQUENTIALID()"),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    TaxId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Email = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    PhoneNumber = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    Street = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    City = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    PostalCode = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    Country = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Contractors", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Contractors_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TenantInvitations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Email = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    Token = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    InvitedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    AcceptedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TenantInvitations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TenantInvitations_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TenantInvitations_Users_InvitedByUserId",
                        column: x => x.InvitedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TenantMembers",
                columns: table => new
                {
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IsAdmin = table.Column<bool>(type: "bit", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TenantMembers", x => new { x.TenantId, x.UserId });
                    table.ForeignKey(
                        name: "FK_TenantMembers_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TenantMembers_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserProfiles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProfileType = table.Column<string>(type: "nvarchar(21)", maxLength: 21, nullable: false),
                    Version = table.Column<int>(type: "int", nullable: true),
                    ActiveTenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserProfiles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserProfiles_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserSessions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RefreshToken = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsRevoked = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserSessions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserSessions_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Projects",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BudgetNet = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: true),
                    BudgetGross = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Projects", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Projects_TenantMembers_TenantId_CreatedByUserId",
                        columns: x => new { x.TenantId, x.CreatedByUserId },
                        principalTable: "TenantMembers",
                        principalColumns: new[] { "TenantId", "UserId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Projects_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CostEstimates",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProjectId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OwnerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    Status = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    TotalNet = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    TotalGross = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    TotalVat = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastCalculatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    SchemaVersion = table.Column<int>(type: "int", nullable: false, defaultValue: 1),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CostEstimates", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CostEstimates_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CostEstimates_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CostEstimates_Users_OwnerId",
                        column: x => x.OwnerId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Notifications",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProjectId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Type = table.Column<int>(type: "int", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Message = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsRead = table.Column<bool>(type: "bit", nullable: false),
                    MetadataJson = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Notifications", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Notifications_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Notifications_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ProjectFilePackages",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProjectId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OwnerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ParentId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProjectFilePackages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProjectFilePackages_ProjectFilePackages_ParentId",
                        column: x => x.ParentId,
                        principalTable: "ProjectFilePackages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ProjectFilePackages_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ProjectFilePackages_TenantMembers_TenantId_CreatedByUserId",
                        columns: x => new { x.TenantId, x.CreatedByUserId },
                        principalTable: "TenantMembers",
                        principalColumns: new[] { "TenantId", "UserId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ProjectFilePackages_TenantMembers_TenantId_OwnerId",
                        columns: x => new { x.TenantId, x.OwnerId },
                        principalTable: "TenantMembers",
                        principalColumns: new[] { "TenantId", "UserId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ProjectFilePackages_Users_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ProjectFilePackages_Users_OwnerId",
                        column: x => x.OwnerId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ProjectMembers",
                columns: table => new
                {
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProjectId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IsAdmin = table.Column<bool>(type: "bit", nullable: false),
                    JoinedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProjectMembers", x => new { x.TenantId, x.ProjectId, x.UserId });
                    table.ForeignKey(
                        name: "FK_ProjectMembers_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ProjectMembers_TenantMembers_TenantId_UserId",
                        columns: x => new { x.TenantId, x.UserId },
                        principalTable: "TenantMembers",
                        principalColumns: new[] { "TenantId", "UserId" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ProjectParams",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProjectId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ParamType = table.Column<string>(type: "nvarchar(13)", maxLength: 13, nullable: false),
                    Code = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Symbol = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProjectParams", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProjectParams_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CostEstimateAdditionalFields",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CostEstimateId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    FieldType = table.Column<int>(type: "int", nullable: false),
                    Order = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CostEstimateAdditionalFields", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CostEstimateAdditionalFields_CostEstimates_CostEstimateId",
                        column: x => x.CostEstimateId,
                        principalTable: "CostEstimates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CostEstimateGroups",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CostEstimateId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    ParentGroupId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Level = table.Column<int>(type: "int", nullable: false),
                    Order = table.Column<int>(type: "int", nullable: false),
                    TotalNet = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    TotalGross = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    TotalVat = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    LastCalculatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CostEstimateGroups", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CostEstimateGroups_CostEstimateGroups_ParentGroupId",
                        column: x => x.ParentGroupId,
                        principalTable: "CostEstimateGroups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CostEstimateGroups_CostEstimates_CostEstimateId",
                        column: x => x.CostEstimateId,
                        principalTable: "CostEstimates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "WorkSchedules",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProjectId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CostEstimateId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkSchedules", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WorkSchedules_CostEstimates_CostEstimateId",
                        column: x => x.CostEstimateId,
                        principalTable: "CostEstimates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_WorkSchedules_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_WorkSchedules_TenantMembers_TenantId_CreatedByUserId",
                        columns: x => new { x.TenantId, x.CreatedByUserId },
                        principalTable: "TenantMembers",
                        principalColumns: new[] { "TenantId", "UserId" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ProjectMemberModulePermissions",
                columns: table => new
                {
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProjectId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Module = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProjectMemberModulePermissions", x => new { x.TenantId, x.ProjectId, x.UserId, x.Module });
                    table.ForeignKey(
                        name: "FK_ProjectMemberModulePermissions_ProjectMembers_TenantId_ProjectId_UserId",
                        columns: x => new { x.TenantId, x.ProjectId, x.UserId },
                        principalTable: "ProjectMembers",
                        principalColumns: new[] { "TenantId", "ProjectId", "UserId" },
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SharedCostEstimates",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProjectId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CostEstimateId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SharedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SharedWithUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SharedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SharedCostEstimates", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SharedCostEstimates_CostEstimates_CostEstimateId",
                        column: x => x.CostEstimateId,
                        principalTable: "CostEstimates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SharedCostEstimates_ProjectMembers_TenantId_ProjectId_SharedByUserId",
                        columns: x => new { x.TenantId, x.ProjectId, x.SharedByUserId },
                        principalTable: "ProjectMembers",
                        principalColumns: new[] { "TenantId", "ProjectId", "UserId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SharedCostEstimates_ProjectMembers_TenantId_ProjectId_SharedWithUserId",
                        columns: x => new { x.TenantId, x.ProjectId, x.SharedWithUserId },
                        principalTable: "ProjectMembers",
                        principalColumns: new[] { "TenantId", "ProjectId", "UserId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SharedCostEstimates_TenantMembers_TenantId_SharedByUserId",
                        columns: x => new { x.TenantId, x.SharedByUserId },
                        principalTable: "TenantMembers",
                        principalColumns: new[] { "TenantId", "UserId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SharedCostEstimates_TenantMembers_TenantId_SharedWithUserId",
                        columns: x => new { x.TenantId, x.SharedWithUserId },
                        principalTable: "TenantMembers",
                        principalColumns: new[] { "TenantId", "UserId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SharedCostEstimates_Users_SharedByUserId",
                        column: x => x.SharedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SharedCostEstimates_Users_SharedWithUserId",
                        column: x => x.SharedWithUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CostEstimateItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CostEstimateId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    GroupId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ParentItemId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RelationType = table.Column<string>(type: "nvarchar(450)", nullable: false, defaultValue: "None"),
                    Order = table.Column<int>(type: "int", nullable: false),
                    Quantity = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: true),
                    Unit = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    UnitPriceNet = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    VatRate = table.Column<decimal>(type: "decimal(5,4)", precision: 5, scale: 4, nullable: true),
                    UnitPriceGross = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    IsSelected = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    IsStageWork = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    NetValue = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    GrossValue = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    VatValue = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CostEstimateItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CostEstimateItems_CostEstimateGroups_GroupId",
                        column: x => x.GroupId,
                        principalTable: "CostEstimateGroups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CostEstimateItems_CostEstimateItems_ParentItemId",
                        column: x => x.ParentItemId,
                        principalTable: "CostEstimateItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CostEstimateItems_CostEstimates_CostEstimateId",
                        column: x => x.CostEstimateId,
                        principalTable: "CostEstimates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "WorkScheduleStages",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProjectId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    WorkScheduleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ParentStageId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Order = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CostEstimateGroupId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkScheduleStages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WorkScheduleStages_CostEstimateGroups_CostEstimateGroupId",
                        column: x => x.CostEstimateGroupId,
                        principalTable: "CostEstimateGroups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_WorkScheduleStages_WorkScheduleStages_ParentStageId",
                        column: x => x.ParentStageId,
                        principalTable: "WorkScheduleStages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_WorkScheduleStages_WorkSchedules_WorkScheduleId",
                        column: x => x.WorkScheduleId,
                        principalTable: "WorkSchedules",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CostEstimateAdditionalFieldValues",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AdditionalFieldId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    GroupId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ItemId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    StringValue = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    DecimalValue = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: true),
                    BoolValue = table.Column<bool>(type: "bit", nullable: true),
                    DateTimeValue = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CostEstimateAdditionalFieldValues", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CostEstimateAdditionalFieldValues_CostEstimateAdditionalFields_AdditionalFieldId",
                        column: x => x.AdditionalFieldId,
                        principalTable: "CostEstimateAdditionalFields",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CostEstimateAdditionalFieldValues_CostEstimateGroups_GroupId",
                        column: x => x.GroupId,
                        principalTable: "CostEstimateGroups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CostEstimateAdditionalFieldValues_CostEstimateItems_ItemId",
                        column: x => x.ItemId,
                        principalTable: "CostEstimateItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CostEstimateItemFiles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ItemId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CostEstimateId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OriginalFileName = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: false),
                    BlobName = table.Column<string>(type: "nvarchar(1024)", maxLength: 1024, nullable: false),
                    ContentType = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    FileSize = table.Column<long>(type: "bigint", nullable: false),
                    Order = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CostEstimateItemFiles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CostEstimateItemFiles_CostEstimateItems_ItemId",
                        column: x => x.ItemId,
                        principalTable: "CostEstimateItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CostEstimateItemFiles_CostEstimates_CostEstimateId",
                        column: x => x.CostEstimateId,
                        principalTable: "CostEstimates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CostEstimateItemFiles_Users_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "WorkScheduleStageWorks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProjectId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    WorkScheduleStageId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CostEstimateItemId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Order = table.Column<int>(type: "int", nullable: false),
                    ColorRgb = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    PlannedStartDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    PlannedEndDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkScheduleStageWorks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WorkScheduleStageWorks_CostEstimateItems_CostEstimateItemId",
                        column: x => x.CostEstimateItemId,
                        principalTable: "CostEstimateItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_WorkScheduleStageWorks_WorkScheduleStages_WorkScheduleStageId",
                        column: x => x.WorkScheduleStageId,
                        principalTable: "WorkScheduleStages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Costs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWSEQUENTIALID()"),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProjectId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Number = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Description = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    Net = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    Gross = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    ContractorId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Date = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CostEstimateItemId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    WorkScheduleStageWorkId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    WorkScheduleStageWorkId1 = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CostType = table.Column<string>(type: "nvarchar(13)", maxLength: 13, nullable: false),
                    CostEstimateItemId1 = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ApprovalStatus = table.Column<string>(type: "nvarchar(450)", nullable: true, defaultValue: "Draft"),
                    ApprovedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ApprovedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Costs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Costs_Contractors_ContractorId",
                        column: x => x.ContractorId,
                        principalTable: "Contractors",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Costs_CostEstimateItems_CostEstimateItemId",
                        column: x => x.CostEstimateItemId,
                        principalTable: "CostEstimateItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Costs_CostEstimateItems_CostEstimateItemId1",
                        column: x => x.CostEstimateItemId1,
                        principalTable: "CostEstimateItems",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Costs_ProjectMembers_TenantId_ProjectId_UserId",
                        columns: x => new { x.TenantId, x.ProjectId, x.UserId },
                        principalTable: "ProjectMembers",
                        principalColumns: new[] { "TenantId", "ProjectId", "UserId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Costs_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Costs_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Costs_WorkScheduleStageWorks_WorkScheduleStageWorkId",
                        column: x => x.WorkScheduleStageWorkId,
                        principalTable: "WorkScheduleStageWorks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Costs_WorkScheduleStageWorks_WorkScheduleStageWorkId1",
                        column: x => x.WorkScheduleStageWorkId1,
                        principalTable: "WorkScheduleStageWorks",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "WorkScheduleStageWorkAssignments",
                columns: table => new
                {
                    WorkScheduleStageWorkId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProjectId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkScheduleStageWorkAssignments", x => new { x.WorkScheduleStageWorkId, x.TenantId, x.ProjectId, x.UserId });
                    table.ForeignKey(
                        name: "FK_WorkScheduleStageWorkAssignments_ProjectMembers_TenantId_ProjectId_UserId",
                        columns: x => new { x.TenantId, x.ProjectId, x.UserId },
                        principalTable: "ProjectMembers",
                        principalColumns: new[] { "TenantId", "ProjectId", "UserId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_WorkScheduleStageWorkAssignments_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_WorkScheduleStageWorkAssignments_TenantMembers_TenantId_UserId",
                        columns: x => new { x.TenantId, x.UserId },
                        principalTable: "TenantMembers",
                        principalColumns: new[] { "TenantId", "UserId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_WorkScheduleStageWorkAssignments_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_WorkScheduleStageWorkAssignments_WorkScheduleStageWorks_WorkScheduleStageWorkId",
                        column: x => x.WorkScheduleStageWorkId,
                        principalTable: "WorkScheduleStageWorks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "WorkScheduleStageWorkComments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProjectId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    WorkScheduleStageWorkId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Content = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkScheduleStageWorkComments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WorkScheduleStageWorkComments_Users_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_WorkScheduleStageWorkComments_WorkScheduleStageWorks_WorkScheduleStageWorkId",
                        column: x => x.WorkScheduleStageWorkId,
                        principalTable: "WorkScheduleStageWorks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "WorkScheduleStageWorkDependencies",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProjectId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    WorkScheduleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PredecessorWorkId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SuccessorWorkId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DependencyType = table.Column<int>(type: "int", nullable: false),
                    LagDays = table.Column<int>(type: "int", nullable: false, defaultValue: 0)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkScheduleStageWorkDependencies", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WorkScheduleStageWorkDependencies_WorkScheduleStageWorks_PredecessorWorkId",
                        column: x => x.PredecessorWorkId,
                        principalTable: "WorkScheduleStageWorks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_WorkScheduleStageWorkDependencies_WorkScheduleStageWorks_SuccessorWorkId",
                        column: x => x.SuccessorWorkId,
                        principalTable: "WorkScheduleStageWorks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_WorkScheduleStageWorkDependencies_WorkSchedules_WorkScheduleId",
                        column: x => x.WorkScheduleId,
                        principalTable: "WorkSchedules",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "WorkScheduleStageWorkPeriods",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProjectId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    WorkScheduleStageWorkId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StartDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EndDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsClosed = table.Column<bool>(type: "bit", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkScheduleStageWorkPeriods", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WorkScheduleStageWorkPeriods_WorkScheduleStageWorks_WorkScheduleStageWorkId",
                        column: x => x.WorkScheduleStageWorkId,
                        principalTable: "WorkScheduleStageWorks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CostAttachments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWSEQUENTIALID()"),
                    CostId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProjectId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OriginalFileName = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    BlobName = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    ContentType = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    FileSize = table.Column<long>(type: "bigint", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CostAttachments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CostAttachments_Costs_CostId",
                        column: x => x.CostId,
                        principalTable: "Costs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ProjectFiles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProjectId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProjectFilePackageId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OwnerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FileName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    DisplayName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CurrentVersionId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProjectFiles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProjectFiles_ProjectFilePackages_ProjectFilePackageId",
                        column: x => x.ProjectFilePackageId,
                        principalTable: "ProjectFilePackages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ProjectFiles_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ProjectFiles_TenantMembers_TenantId_OwnerId",
                        columns: x => new { x.TenantId, x.OwnerId },
                        principalTable: "TenantMembers",
                        principalColumns: new[] { "TenantId", "UserId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ProjectFiles_Users_OwnerId",
                        column: x => x.OwnerId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ProjectFileVersions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProjectFileId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProjectId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    VersionNumber = table.Column<int>(type: "int", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BlobFileName = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    BlobPath = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    ContentType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    FileSizeBytes = table.Column<long>(type: "bigint", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProjectFileVersions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProjectFileVersions_ProjectFiles_ProjectFileId",
                        column: x => x.ProjectFileId,
                        principalTable: "ProjectFiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ProjectFileVersions_Users_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SharedProjectFiles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProjectId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProjectFilePackageId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProjectFileId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Access = table.Column<string>(type: "nvarchar(10)", nullable: false),
                    SharedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SharedWithUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SharedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SharedProjectFiles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SharedProjectFiles_ProjectFilePackages_ProjectFilePackageId",
                        column: x => x.ProjectFilePackageId,
                        principalTable: "ProjectFilePackages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SharedProjectFiles_ProjectFiles_ProjectFileId",
                        column: x => x.ProjectFileId,
                        principalTable: "ProjectFiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SharedProjectFiles_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_SharedProjectFiles_TenantMembers_TenantId_SharedByUserId",
                        columns: x => new { x.TenantId, x.SharedByUserId },
                        principalTable: "TenantMembers",
                        principalColumns: new[] { "TenantId", "UserId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SharedProjectFiles_TenantMembers_TenantId_SharedWithUserId",
                        columns: x => new { x.TenantId, x.SharedWithUserId },
                        principalTable: "TenantMembers",
                        principalColumns: new[] { "TenantId", "UserId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SharedProjectFiles_Users_SharedByUserId",
                        column: x => x.SharedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SharedProjectFiles_Users_SharedWithUserId",
                        column: x => x.SharedWithUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ProjectFileVersionComments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProjectFileVersionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProjectId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Content = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EditedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProjectFileVersionComments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProjectFileVersionComments_ProjectFileVersions_ProjectFileVersionId",
                        column: x => x.ProjectFileVersionId,
                        principalTable: "ProjectFileVersions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ProjectFileVersionComments_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ChatMembers_ChatId_UserId",
                table: "ChatMembers",
                columns: new[] { "ChatId", "UserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ChatMembers_UserId",
                table: "ChatMembers",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Contractors_TenantId",
                table: "Contractors",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_Contractors_TenantId_Name",
                table: "Contractors",
                columns: new[] { "TenantId", "Name" },
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_CostAttachments_CostId",
                table: "CostAttachments",
                column: "CostId");

            migrationBuilder.CreateIndex(
                name: "IX_CostAttachments_TenantId_ProjectId",
                table: "CostAttachments",
                columns: new[] { "TenantId", "ProjectId" });

            migrationBuilder.CreateIndex(
                name: "IX_CostEstimateAdditionalFields_CostEstimateId",
                table: "CostEstimateAdditionalFields",
                column: "CostEstimateId");

            migrationBuilder.CreateIndex(
                name: "IX_CostEstimateAdditionalFields_CostEstimateId_Order",
                table: "CostEstimateAdditionalFields",
                columns: new[] { "CostEstimateId", "Order" });

            migrationBuilder.CreateIndex(
                name: "IX_CostEstimateAdditionalFieldValues_AdditionalFieldId",
                table: "CostEstimateAdditionalFieldValues",
                column: "AdditionalFieldId");

            migrationBuilder.CreateIndex(
                name: "IX_CostEstimateAdditionalFieldValues_GroupId",
                table: "CostEstimateAdditionalFieldValues",
                column: "GroupId");

            migrationBuilder.CreateIndex(
                name: "IX_CostEstimateAdditionalFieldValues_ItemId",
                table: "CostEstimateAdditionalFieldValues",
                column: "ItemId");

            migrationBuilder.CreateIndex(
                name: "IX_CostEstimateGroups_CostEstimateId",
                table: "CostEstimateGroups",
                column: "CostEstimateId");

            migrationBuilder.CreateIndex(
                name: "IX_CostEstimateGroups_CostEstimateId_Level",
                table: "CostEstimateGroups",
                columns: new[] { "CostEstimateId", "Level" });

            migrationBuilder.CreateIndex(
                name: "IX_CostEstimateGroups_CostEstimateId_ParentGroupId",
                table: "CostEstimateGroups",
                columns: new[] { "CostEstimateId", "ParentGroupId" });

            migrationBuilder.CreateIndex(
                name: "IX_CostEstimateGroups_IsDeleted",
                table: "CostEstimateGroups",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_CostEstimateGroups_ParentGroupId",
                table: "CostEstimateGroups",
                column: "ParentGroupId");

            migrationBuilder.CreateIndex(
                name: "IX_CostEstimateGroups_ParentGroupId_Order",
                table: "CostEstimateGroups",
                columns: new[] { "ParentGroupId", "Order" });

            migrationBuilder.CreateIndex(
                name: "IX_CostEstimateItemFiles_CostEstimateId",
                table: "CostEstimateItemFiles",
                column: "CostEstimateId");

            migrationBuilder.CreateIndex(
                name: "IX_CostEstimateItemFiles_CreatedByUserId",
                table: "CostEstimateItemFiles",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_CostEstimateItemFiles_IsDeleted",
                table: "CostEstimateItemFiles",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_CostEstimateItemFiles_ItemId",
                table: "CostEstimateItemFiles",
                column: "ItemId");

            migrationBuilder.CreateIndex(
                name: "IX_CostEstimateItemFiles_ItemId_CostEstimateId",
                table: "CostEstimateItemFiles",
                columns: new[] { "ItemId", "CostEstimateId" });

            migrationBuilder.CreateIndex(
                name: "IX_CostEstimateItems_CostEstimateId",
                table: "CostEstimateItems",
                column: "CostEstimateId");

            migrationBuilder.CreateIndex(
                name: "IX_CostEstimateItems_GroupId",
                table: "CostEstimateItems",
                column: "GroupId");

            migrationBuilder.CreateIndex(
                name: "IX_CostEstimateItems_GroupId_Order",
                table: "CostEstimateItems",
                columns: new[] { "GroupId", "Order" });

            migrationBuilder.CreateIndex(
                name: "IX_CostEstimateItems_IsDeleted",
                table: "CostEstimateItems",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_CostEstimateItems_ParentItemId",
                table: "CostEstimateItems",
                column: "ParentItemId");

            migrationBuilder.CreateIndex(
                name: "IX_CostEstimateItems_ParentItemId_RelationType",
                table: "CostEstimateItems",
                columns: new[] { "ParentItemId", "RelationType" });

            migrationBuilder.CreateIndex(
                name: "IX_CostEstimates_CreatedAt",
                table: "CostEstimates",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_CostEstimates_IsDeleted",
                table: "CostEstimates",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_CostEstimates_OwnerId",
                table: "CostEstimates",
                column: "OwnerId");

            migrationBuilder.CreateIndex(
                name: "IX_CostEstimates_ProjectId",
                table: "CostEstimates",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_CostEstimates_Status",
                table: "CostEstimates",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_CostEstimates_TenantId",
                table: "CostEstimates",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_CostEstimates_TenantId_ProjectId",
                table: "CostEstimates",
                columns: new[] { "TenantId", "ProjectId" });

            migrationBuilder.CreateIndex(
                name: "IX_Costs_ContractorId",
                table: "Costs",
                column: "ContractorId");

            migrationBuilder.CreateIndex(
                name: "IX_Costs_CostEstimateItemId",
                table: "Costs",
                column: "CostEstimateItemId");

            migrationBuilder.CreateIndex(
                name: "IX_Costs_CostEstimateItemId1",
                table: "Costs",
                column: "CostEstimateItemId1");

            migrationBuilder.CreateIndex(
                name: "IX_Costs_Date",
                table: "Costs",
                column: "Date");

            migrationBuilder.CreateIndex(
                name: "IX_Costs_ProjectId",
                table: "Costs",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_Costs_TenantId_ProjectId",
                table: "Costs",
                columns: new[] { "TenantId", "ProjectId" });

            migrationBuilder.CreateIndex(
                name: "IX_Costs_TenantId_ProjectId_ApprovalStatus",
                table: "Costs",
                columns: new[] { "TenantId", "ProjectId", "ApprovalStatus" });

            migrationBuilder.CreateIndex(
                name: "IX_Costs_TenantId_ProjectId_IsDeleted",
                table: "Costs",
                columns: new[] { "TenantId", "ProjectId", "IsDeleted" });

            migrationBuilder.CreateIndex(
                name: "IX_Costs_TenantId_ProjectId_UserId",
                table: "Costs",
                columns: new[] { "TenantId", "ProjectId", "UserId" });

            migrationBuilder.CreateIndex(
                name: "IX_Costs_WorkScheduleStageWorkId",
                table: "Costs",
                column: "WorkScheduleStageWorkId");

            migrationBuilder.CreateIndex(
                name: "IX_Costs_WorkScheduleStageWorkId1",
                table: "Costs",
                column: "WorkScheduleStageWorkId1");

            migrationBuilder.CreateIndex(
                name: "IX_MessageHistories_ChatId_CreatedAt",
                table: "MessageHistories",
                columns: new[] { "ChatId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_MessageHistories_ReplyToMessageId",
                table: "MessageHistories",
                column: "ReplyToMessageId",
                filter: "ReplyToMessageId IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_ProjectId",
                table: "Notifications",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_TenantId",
                table: "Notifications",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_TenantId_IsRead",
                table: "Notifications",
                columns: new[] { "TenantId", "IsRead" });

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_UserId_IsRead",
                table: "Notifications",
                columns: new[] { "UserId", "IsRead" });

            migrationBuilder.CreateIndex(
                name: "IX_ProjectFilePackages_CreatedByUserId",
                table: "ProjectFilePackages",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectFilePackages_OwnerId_ProjectId",
                table: "ProjectFilePackages",
                columns: new[] { "OwnerId", "ProjectId" });

            migrationBuilder.CreateIndex(
                name: "IX_ProjectFilePackages_ParentId",
                table: "ProjectFilePackages",
                column: "ParentId");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectFilePackages_ProjectId_IsDeleted",
                table: "ProjectFilePackages",
                columns: new[] { "ProjectId", "IsDeleted" });

            migrationBuilder.CreateIndex(
                name: "IX_ProjectFilePackages_ProjectId_TenantId",
                table: "ProjectFilePackages",
                columns: new[] { "ProjectId", "TenantId" });

            migrationBuilder.CreateIndex(
                name: "IX_ProjectFilePackages_TenantId_CreatedByUserId",
                table: "ProjectFilePackages",
                columns: new[] { "TenantId", "CreatedByUserId" });

            migrationBuilder.CreateIndex(
                name: "IX_ProjectFilePackages_TenantId_OwnerId",
                table: "ProjectFilePackages",
                columns: new[] { "TenantId", "OwnerId" });

            migrationBuilder.CreateIndex(
                name: "IX_ProjectFilePackages_TenantId_ProjectId_OwnerId_Name",
                table: "ProjectFilePackages",
                columns: new[] { "TenantId", "ProjectId", "OwnerId", "Name" },
                unique: true,
                filter: "[IsDeleted] = 0 AND [ParentId] IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectFilePackages_TenantId_ProjectId_OwnerId_ParentId_Name",
                table: "ProjectFilePackages",
                columns: new[] { "TenantId", "ProjectId", "OwnerId", "ParentId", "Name" },
                unique: true,
                filter: "[IsDeleted] = 0 AND [ParentId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectFiles_CurrentVersionId",
                table: "ProjectFiles",
                column: "CurrentVersionId");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectFiles_OwnerId",
                table: "ProjectFiles",
                column: "OwnerId");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectFiles_ProjectFilePackageId",
                table: "ProjectFiles",
                column: "ProjectFilePackageId");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectFiles_ProjectId_IsDeleted",
                table: "ProjectFiles",
                columns: new[] { "ProjectId", "IsDeleted" });

            migrationBuilder.CreateIndex(
                name: "IX_ProjectFiles_ProjectId_TenantId",
                table: "ProjectFiles",
                columns: new[] { "ProjectId", "TenantId" });

            migrationBuilder.CreateIndex(
                name: "IX_ProjectFiles_TenantId_OwnerId",
                table: "ProjectFiles",
                columns: new[] { "TenantId", "OwnerId" });

            migrationBuilder.CreateIndex(
                name: "IX_ProjectFileVersionComments_CreatedAt",
                table: "ProjectFileVersionComments",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectFileVersionComments_ProjectFileVersionId_IsDeleted",
                table: "ProjectFileVersionComments",
                columns: new[] { "ProjectFileVersionId", "IsDeleted" });

            migrationBuilder.CreateIndex(
                name: "IX_ProjectFileVersionComments_UserId",
                table: "ProjectFileVersionComments",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectFileVersions_CreatedAt",
                table: "ProjectFileVersions",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectFileVersions_CreatedByUserId",
                table: "ProjectFileVersions",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectFileVersions_ProjectFileId_IsDeleted",
                table: "ProjectFileVersions",
                columns: new[] { "ProjectFileId", "IsDeleted" });

            migrationBuilder.CreateIndex(
                name: "IX_ProjectFileVersions_ProjectFileId_VersionNumber",
                table: "ProjectFileVersions",
                columns: new[] { "ProjectFileId", "VersionNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProjectFileVersions_TenantId_ProjectId",
                table: "ProjectFileVersions",
                columns: new[] { "TenantId", "ProjectId" });

            migrationBuilder.CreateIndex(
                name: "IX_ProjectMembers_ProjectId",
                table: "ProjectMembers",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectMembers_TenantId_UserId",
                table: "ProjectMembers",
                columns: new[] { "TenantId", "UserId" });

            migrationBuilder.CreateIndex(
                name: "IX_ProjectParams_ProjectId",
                table: "ProjectParams",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectParams_ProjectId_ParamType",
                table: "ProjectParams",
                columns: new[] { "ProjectId", "ParamType" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Projects_TenantId_CreatedByUserId",
                table: "Projects",
                columns: new[] { "TenantId", "CreatedByUserId" });

            migrationBuilder.CreateIndex(
                name: "IX_SharedCostEstimates_CostEstimateId",
                table: "SharedCostEstimates",
                column: "CostEstimateId");

            migrationBuilder.CreateIndex(
                name: "IX_SharedCostEstimates_CostEstimateId_SharedWithUserId",
                table: "SharedCostEstimates",
                columns: new[] { "CostEstimateId", "SharedWithUserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SharedCostEstimates_SharedByUserId",
                table: "SharedCostEstimates",
                column: "SharedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_SharedCostEstimates_SharedWithUserId_ProjectId",
                table: "SharedCostEstimates",
                columns: new[] { "SharedWithUserId", "ProjectId" });

            migrationBuilder.CreateIndex(
                name: "IX_SharedCostEstimates_TenantId_ProjectId_SharedByUserId",
                table: "SharedCostEstimates",
                columns: new[] { "TenantId", "ProjectId", "SharedByUserId" });

            migrationBuilder.CreateIndex(
                name: "IX_SharedCostEstimates_TenantId_ProjectId_SharedWithUserId",
                table: "SharedCostEstimates",
                columns: new[] { "TenantId", "ProjectId", "SharedWithUserId" });

            migrationBuilder.CreateIndex(
                name: "IX_SharedCostEstimates_TenantId_SharedByUserId",
                table: "SharedCostEstimates",
                columns: new[] { "TenantId", "SharedByUserId" });

            migrationBuilder.CreateIndex(
                name: "IX_SharedCostEstimates_TenantId_SharedWithUserId",
                table: "SharedCostEstimates",
                columns: new[] { "TenantId", "SharedWithUserId" });

            migrationBuilder.CreateIndex(
                name: "IX_SharedProjectFiles_ProjectFileId",
                table: "SharedProjectFiles",
                column: "ProjectFileId");

            migrationBuilder.CreateIndex(
                name: "IX_SharedProjectFiles_ProjectFilePackageId_ProjectFileId_SharedWithUserId",
                table: "SharedProjectFiles",
                columns: new[] { "ProjectFilePackageId", "ProjectFileId", "SharedWithUserId" },
                unique: true,
                filter: "[ProjectFileId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_SharedProjectFiles_ProjectFilePackageId_SharedWithUserId",
                table: "SharedProjectFiles",
                columns: new[] { "ProjectFilePackageId", "SharedWithUserId" });

            migrationBuilder.CreateIndex(
                name: "IX_SharedProjectFiles_ProjectId",
                table: "SharedProjectFiles",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_SharedProjectFiles_SharedByUserId_ProjectId",
                table: "SharedProjectFiles",
                columns: new[] { "SharedByUserId", "ProjectId" });

            migrationBuilder.CreateIndex(
                name: "IX_SharedProjectFiles_SharedWithUserId_ProjectId",
                table: "SharedProjectFiles",
                columns: new[] { "SharedWithUserId", "ProjectId" });

            migrationBuilder.CreateIndex(
                name: "IX_SharedProjectFiles_TenantId_SharedByUserId",
                table: "SharedProjectFiles",
                columns: new[] { "TenantId", "SharedByUserId" });

            migrationBuilder.CreateIndex(
                name: "IX_SharedProjectFiles_TenantId_SharedWithUserId",
                table: "SharedProjectFiles",
                columns: new[] { "TenantId", "SharedWithUserId" });

            migrationBuilder.CreateIndex(
                name: "IX_TenantInvitations_ExpiresAt",
                table: "TenantInvitations",
                column: "ExpiresAt");

            migrationBuilder.CreateIndex(
                name: "IX_TenantInvitations_InvitedByUserId",
                table: "TenantInvitations",
                column: "InvitedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_TenantInvitations_TenantId_Email",
                table: "TenantInvitations",
                columns: new[] { "TenantId", "Email" });

            migrationBuilder.CreateIndex(
                name: "IX_TenantInvitations_TenantId_Status",
                table: "TenantInvitations",
                columns: new[] { "TenantId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_TenantInvitations_Token",
                table: "TenantInvitations",
                column: "Token",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TenantMembers_UserId",
                table: "TenantMembers",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_UserProfiles_UserId",
                table: "UserProfiles",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Users_AzureAdB2CObjectId",
                table: "Users",
                column: "AzureAdB2CObjectId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_Email",
                table: "Users",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserSessions_UserId",
                table: "UserSessions",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkSchedules_CostEstimateId",
                table: "WorkSchedules",
                column: "CostEstimateId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkSchedules_ProjectId",
                table: "WorkSchedules",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkSchedules_TenantId_CreatedByUserId",
                table: "WorkSchedules",
                columns: new[] { "TenantId", "CreatedByUserId" });

            migrationBuilder.CreateIndex(
                name: "IX_WorkSchedules_TenantId_ProjectId",
                table: "WorkSchedules",
                columns: new[] { "TenantId", "ProjectId" });

            migrationBuilder.CreateIndex(
                name: "IX_WorkSchedules_TenantId_ProjectId_IsDeleted",
                table: "WorkSchedules",
                columns: new[] { "TenantId", "ProjectId", "IsDeleted" });

            migrationBuilder.CreateIndex(
                name: "IX_WorkScheduleStages_CostEstimateGroupId",
                table: "WorkScheduleStages",
                column: "CostEstimateGroupId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkScheduleStages_ParentStageId",
                table: "WorkScheduleStages",
                column: "ParentStageId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkScheduleStages_TenantId_ProjectId",
                table: "WorkScheduleStages",
                columns: new[] { "TenantId", "ProjectId" });

            migrationBuilder.CreateIndex(
                name: "IX_WorkScheduleStages_WorkScheduleId_IsDeleted",
                table: "WorkScheduleStages",
                columns: new[] { "WorkScheduleId", "IsDeleted" });

            migrationBuilder.CreateIndex(
                name: "IX_WorkScheduleStages_WorkScheduleId_Order",
                table: "WorkScheduleStages",
                columns: new[] { "WorkScheduleId", "Order" });

            migrationBuilder.CreateIndex(
                name: "IX_WorkScheduleStageWorkAssignments_ProjectId",
                table: "WorkScheduleStageWorkAssignments",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkScheduleStageWorkAssignments_TenantId_ProjectId_UserId",
                table: "WorkScheduleStageWorkAssignments",
                columns: new[] { "TenantId", "ProjectId", "UserId" });

            migrationBuilder.CreateIndex(
                name: "IX_WorkScheduleStageWorkAssignments_TenantId_UserId",
                table: "WorkScheduleStageWorkAssignments",
                columns: new[] { "TenantId", "UserId" });

            migrationBuilder.CreateIndex(
                name: "IX_WorkScheduleStageWorkComments_CreatedByUserId",
                table: "WorkScheduleStageWorkComments",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkScheduleStageWorkComments_TenantId_ProjectId",
                table: "WorkScheduleStageWorkComments",
                columns: new[] { "TenantId", "ProjectId" });

            migrationBuilder.CreateIndex(
                name: "IX_WorkScheduleStageWorkComments_WorkScheduleStageWorkId_CreatedAt",
                table: "WorkScheduleStageWorkComments",
                columns: new[] { "WorkScheduleStageWorkId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_WorkScheduleStageWorkDependencies_PredecessorWorkId",
                table: "WorkScheduleStageWorkDependencies",
                column: "PredecessorWorkId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkScheduleStageWorkDependencies_SuccessorWorkId",
                table: "WorkScheduleStageWorkDependencies",
                column: "SuccessorWorkId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkScheduleStageWorkDependencies_TenantId_ProjectId",
                table: "WorkScheduleStageWorkDependencies",
                columns: new[] { "TenantId", "ProjectId" });

            migrationBuilder.CreateIndex(
                name: "IX_WorkScheduleStageWorkDependencies_TenantId_WorkScheduleId",
                table: "WorkScheduleStageWorkDependencies",
                columns: new[] { "TenantId", "WorkScheduleId" });

            migrationBuilder.CreateIndex(
                name: "IX_WorkScheduleStageWorkDependencies_WorkScheduleId_PredecessorWorkId_SuccessorWorkId_DependencyType",
                table: "WorkScheduleStageWorkDependencies",
                columns: new[] { "WorkScheduleId", "PredecessorWorkId", "SuccessorWorkId", "DependencyType" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_WorkScheduleStageWorkPeriods_TenantId_ProjectId",
                table: "WorkScheduleStageWorkPeriods",
                columns: new[] { "TenantId", "ProjectId" });

            migrationBuilder.CreateIndex(
                name: "IX_WorkScheduleStageWorkPeriods_WorkScheduleStageWorkId_StartDate",
                table: "WorkScheduleStageWorkPeriods",
                columns: new[] { "WorkScheduleStageWorkId", "StartDate" });

            migrationBuilder.CreateIndex(
                name: "IX_WorkScheduleStageWorks_CostEstimateItemId",
                table: "WorkScheduleStageWorks",
                column: "CostEstimateItemId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkScheduleStageWorks_TenantId_ProjectId",
                table: "WorkScheduleStageWorks",
                columns: new[] { "TenantId", "ProjectId" });

            migrationBuilder.CreateIndex(
                name: "IX_WorkScheduleStageWorks_TenantId_ProjectId_IsDeleted",
                table: "WorkScheduleStageWorks",
                columns: new[] { "TenantId", "ProjectId", "IsDeleted" });

            migrationBuilder.CreateIndex(
                name: "IX_WorkScheduleStageWorks_WorkScheduleStageId_IsDeleted",
                table: "WorkScheduleStageWorks",
                columns: new[] { "WorkScheduleStageId", "IsDeleted" });

            migrationBuilder.CreateIndex(
                name: "IX_WorkScheduleStageWorks_WorkScheduleStageId_Order",
                table: "WorkScheduleStageWorks",
                columns: new[] { "WorkScheduleStageId", "Order" });

            migrationBuilder.AddForeignKey(
                name: "FK_ProjectFiles_ProjectFileVersions_CurrentVersionId",
                table: "ProjectFiles",
                column: "CurrentVersionId",
                principalTable: "ProjectFileVersions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Projects_Tenants_TenantId",
                table: "Projects");

            migrationBuilder.DropForeignKey(
                name: "FK_TenantMembers_Tenants_TenantId",
                table: "TenantMembers");

            migrationBuilder.DropForeignKey(
                name: "FK_ProjectFilePackages_Users_CreatedByUserId",
                table: "ProjectFilePackages");

            migrationBuilder.DropForeignKey(
                name: "FK_ProjectFilePackages_Users_OwnerId",
                table: "ProjectFilePackages");

            migrationBuilder.DropForeignKey(
                name: "FK_ProjectFiles_Users_OwnerId",
                table: "ProjectFiles");

            migrationBuilder.DropForeignKey(
                name: "FK_ProjectFileVersions_Users_CreatedByUserId",
                table: "ProjectFileVersions");

            migrationBuilder.DropForeignKey(
                name: "FK_TenantMembers_Users_UserId",
                table: "TenantMembers");

            migrationBuilder.DropForeignKey(
                name: "FK_ProjectFilePackages_Projects_ProjectId",
                table: "ProjectFilePackages");

            migrationBuilder.DropForeignKey(
                name: "FK_ProjectFiles_Projects_ProjectId",
                table: "ProjectFiles");

            migrationBuilder.DropForeignKey(
                name: "FK_ProjectFilePackages_TenantMembers_TenantId_CreatedByUserId",
                table: "ProjectFilePackages");

            migrationBuilder.DropForeignKey(
                name: "FK_ProjectFilePackages_TenantMembers_TenantId_OwnerId",
                table: "ProjectFilePackages");

            migrationBuilder.DropForeignKey(
                name: "FK_ProjectFiles_TenantMembers_TenantId_OwnerId",
                table: "ProjectFiles");

            migrationBuilder.DropForeignKey(
                name: "FK_ProjectFiles_ProjectFilePackages_ProjectFilePackageId",
                table: "ProjectFiles");

            migrationBuilder.DropForeignKey(
                name: "FK_ProjectFiles_ProjectFileVersions_CurrentVersionId",
                table: "ProjectFiles");

            migrationBuilder.DropTable(
                name: "ChatMembers");

            migrationBuilder.DropTable(
                name: "CostAttachments");

            migrationBuilder.DropTable(
                name: "CostEstimateAdditionalFieldValues");

            migrationBuilder.DropTable(
                name: "CostEstimateItemFiles");

            migrationBuilder.DropTable(
                name: "MessageHistories");

            migrationBuilder.DropTable(
                name: "Notifications");

            migrationBuilder.DropTable(
                name: "ProjectFileVersionComments");

            migrationBuilder.DropTable(
                name: "ProjectMemberModulePermissions");

            migrationBuilder.DropTable(
                name: "ProjectParams");

            migrationBuilder.DropTable(
                name: "SharedCostEstimates");

            migrationBuilder.DropTable(
                name: "SharedProjectFiles");

            migrationBuilder.DropTable(
                name: "TenantInvitations");

            migrationBuilder.DropTable(
                name: "UserProfiles");

            migrationBuilder.DropTable(
                name: "UserSessions");

            migrationBuilder.DropTable(
                name: "WorkScheduleStageWorkAssignments");

            migrationBuilder.DropTable(
                name: "WorkScheduleStageWorkComments");

            migrationBuilder.DropTable(
                name: "WorkScheduleStageWorkDependencies");

            migrationBuilder.DropTable(
                name: "WorkScheduleStageWorkPeriods");

            migrationBuilder.DropTable(
                name: "Costs");

            migrationBuilder.DropTable(
                name: "CostEstimateAdditionalFields");

            migrationBuilder.DropTable(
                name: "Chats");

            migrationBuilder.DropTable(
                name: "Contractors");

            migrationBuilder.DropTable(
                name: "ProjectMembers");

            migrationBuilder.DropTable(
                name: "WorkScheduleStageWorks");

            migrationBuilder.DropTable(
                name: "CostEstimateItems");

            migrationBuilder.DropTable(
                name: "WorkScheduleStages");

            migrationBuilder.DropTable(
                name: "CostEstimateGroups");

            migrationBuilder.DropTable(
                name: "WorkSchedules");

            migrationBuilder.DropTable(
                name: "CostEstimates");

            migrationBuilder.DropTable(
                name: "Tenants");

            migrationBuilder.DropTable(
                name: "Users");

            migrationBuilder.DropTable(
                name: "Projects");

            migrationBuilder.DropTable(
                name: "TenantMembers");

            migrationBuilder.DropTable(
                name: "ProjectFilePackages");

            migrationBuilder.DropTable(
                name: "ProjectFileVersions");

            migrationBuilder.DropTable(
                name: "ProjectFiles");
        }
    }
}
