using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Kurator.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class FixedSchemaWithBlockCurator : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "blocks",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Code = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    Status = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_blocks", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "reference_values",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Category = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_reference_values", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "users",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Login = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    PasswordHash = table.Column<string>(type: "text", nullable: false),
                    Role = table.Column<string>(type: "text", nullable: false),
                    IsFirstLogin = table.Column<bool>(type: "boolean", nullable: false),
                    PublicKey = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    MfaSecret = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    MfaEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastLoginAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_users", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "audit_logs",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<int>(type: "integer", nullable: false),
                    Action = table.Column<string>(type: "text", nullable: false),
                    EntityType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    EntityId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    OldValuesJson = table.Column<string>(type: "text", nullable: true),
                    NewValuesJson = table.Column<string>(type: "text", nullable: true),
                    Timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_audit_logs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_audit_logs_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "block_curator",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    BlockId = table.Column<int>(type: "integer", nullable: false),
                    UserId = table.Column<int>(type: "integer", nullable: false),
                    CuratorType = table.Column<string>(type: "text", nullable: false),
                    AssignedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    AssignedBy = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_block_curator", x => x.Id);
                    table.ForeignKey(
                        name: "FK_block_curator_blocks_BlockId",
                        column: x => x.BlockId,
                        principalTable: "blocks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_block_curator_users_AssignedBy",
                        column: x => x.AssignedBy,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_block_curator_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "contacts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ContactId = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    BlockId = table.Column<int>(type: "integer", nullable: false),
                    FullNameEncrypted = table.Column<string>(type: "text", nullable: false),
                    OrganizationId = table.Column<int>(type: "integer", nullable: true),
                    Position = table.Column<string>(type: "text", nullable: true),
                    InfluenceStatusId = table.Column<int>(type: "integer", nullable: true),
                    InfluenceTypeId = table.Column<int>(type: "integer", nullable: true),
                    UsefulnessDescription = table.Column<string>(type: "text", nullable: true),
                    CommunicationChannelId = table.Column<int>(type: "integer", nullable: true),
                    ContactSourceId = table.Column<int>(type: "integer", nullable: true),
                    LastInteractionDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    NextTouchDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    NotesEncrypted = table.Column<string>(type: "text", nullable: true),
                    ResponsibleCuratorId = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_contacts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_contacts_blocks_BlockId",
                        column: x => x.BlockId,
                        principalTable: "blocks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_contacts_reference_values_CommunicationChannelId",
                        column: x => x.CommunicationChannelId,
                        principalTable: "reference_values",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_contacts_reference_values_ContactSourceId",
                        column: x => x.ContactSourceId,
                        principalTable: "reference_values",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_contacts_reference_values_InfluenceStatusId",
                        column: x => x.InfluenceStatusId,
                        principalTable: "reference_values",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_contacts_reference_values_InfluenceTypeId",
                        column: x => x.InfluenceTypeId,
                        principalTable: "reference_values",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_contacts_reference_values_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "reference_values",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_contacts_users_ResponsibleCuratorId",
                        column: x => x.ResponsibleCuratorId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_contacts_users_UpdatedBy",
                        column: x => x.UpdatedBy,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "faqs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Title = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Content = table.Column<string>(type: "text", nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_faqs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_faqs_users_UpdatedBy",
                        column: x => x.UpdatedBy,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "watchlist",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    FullName = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    RoleStatus = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    RiskSphereId = table.Column<int>(type: "integer", nullable: true),
                    ThreatSource = table.Column<string>(type: "text", nullable: true),
                    ConflictDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    RiskLevel = table.Column<string>(type: "text", nullable: false),
                    MonitoringFrequency = table.Column<string>(type: "text", nullable: false),
                    LastCheckDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    NextCheckDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DynamicsDescription = table.Column<string>(type: "text", nullable: true),
                    WatchOwnerId = table.Column<int>(type: "integer", nullable: true),
                    AttachmentsJson = table.Column<string>(type: "text", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_watchlist", x => x.Id);
                    table.ForeignKey(
                        name: "FK_watchlist_reference_values_RiskSphereId",
                        column: x => x.RiskSphereId,
                        principalTable: "reference_values",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_watchlist_users_UpdatedBy",
                        column: x => x.UpdatedBy,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_watchlist_users_WatchOwnerId",
                        column: x => x.WatchOwnerId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "influence_status_history",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ContactId = table.Column<int>(type: "integer", nullable: false),
                    PreviousStatus = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    NewStatus = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    ChangedByUserId = table.Column<int>(type: "integer", nullable: false),
                    ChangedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_influence_status_history", x => x.Id);
                    table.ForeignKey(
                        name: "FK_influence_status_history_contacts_ContactId",
                        column: x => x.ContactId,
                        principalTable: "contacts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_influence_status_history_users_ChangedByUserId",
                        column: x => x.ChangedByUserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "interactions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ContactId = table.Column<int>(type: "integer", nullable: false),
                    InteractionDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    InteractionTypeId = table.Column<int>(type: "integer", nullable: true),
                    CuratorId = table.Column<int>(type: "integer", nullable: false),
                    ResultId = table.Column<int>(type: "integer", nullable: true),
                    CommentEncrypted = table.Column<string>(type: "text", nullable: true),
                    StatusChangeJson = table.Column<string>(type: "text", nullable: true),
                    AttachmentsJson = table.Column<string>(type: "text", nullable: true),
                    NextTouchDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_interactions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_interactions_contacts_ContactId",
                        column: x => x.ContactId,
                        principalTable: "contacts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_interactions_reference_values_InteractionTypeId",
                        column: x => x.InteractionTypeId,
                        principalTable: "reference_values",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_interactions_reference_values_ResultId",
                        column: x => x.ResultId,
                        principalTable: "reference_values",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_interactions_users_CuratorId",
                        column: x => x.CuratorId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_interactions_users_UpdatedBy",
                        column: x => x.UpdatedBy,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "watchlist_history",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    WatchlistId = table.Column<int>(type: "integer", nullable: false),
                    OldRiskLevel = table.Column<string>(type: "text", nullable: true),
                    NewRiskLevel = table.Column<string>(type: "text", nullable: true),
                    ChangedBy = table.Column<int>(type: "integer", nullable: false),
                    ChangedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Comment = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_watchlist_history", x => x.Id);
                    table.ForeignKey(
                        name: "FK_watchlist_history_users_ChangedBy",
                        column: x => x.ChangedBy,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_watchlist_history_watchlist_WatchlistId",
                        column: x => x.WatchlistId,
                        principalTable: "watchlist",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_audit_logs_Timestamp",
                table: "audit_logs",
                column: "Timestamp");

            migrationBuilder.CreateIndex(
                name: "IX_audit_logs_UserId",
                table: "audit_logs",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_block_curator_AssignedBy",
                table: "block_curator",
                column: "AssignedBy");

            migrationBuilder.CreateIndex(
                name: "IX_block_curator_BlockId_CuratorType",
                table: "block_curator",
                columns: new[] { "BlockId", "CuratorType" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_block_curator_BlockId_UserId",
                table: "block_curator",
                columns: new[] { "BlockId", "UserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_block_curator_UserId",
                table: "block_curator",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_blocks_Code",
                table: "blocks",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_contacts_BlockId",
                table: "contacts",
                column: "BlockId");

            migrationBuilder.CreateIndex(
                name: "IX_contacts_CommunicationChannelId",
                table: "contacts",
                column: "CommunicationChannelId");

            migrationBuilder.CreateIndex(
                name: "IX_contacts_ContactId",
                table: "contacts",
                column: "ContactId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_contacts_ContactSourceId",
                table: "contacts",
                column: "ContactSourceId");

            migrationBuilder.CreateIndex(
                name: "IX_contacts_InfluenceStatusId",
                table: "contacts",
                column: "InfluenceStatusId");

            migrationBuilder.CreateIndex(
                name: "IX_contacts_InfluenceTypeId",
                table: "contacts",
                column: "InfluenceTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_contacts_IsActive",
                table: "contacts",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_contacts_NextTouchDate",
                table: "contacts",
                column: "NextTouchDate");

            migrationBuilder.CreateIndex(
                name: "IX_contacts_OrganizationId",
                table: "contacts",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_contacts_ResponsibleCuratorId",
                table: "contacts",
                column: "ResponsibleCuratorId");

            migrationBuilder.CreateIndex(
                name: "IX_contacts_UpdatedBy",
                table: "contacts",
                column: "UpdatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_faqs_IsActive",
                table: "faqs",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_faqs_SortOrder",
                table: "faqs",
                column: "SortOrder");

            migrationBuilder.CreateIndex(
                name: "IX_faqs_UpdatedBy",
                table: "faqs",
                column: "UpdatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_influence_status_history_ChangedAt",
                table: "influence_status_history",
                column: "ChangedAt");

            migrationBuilder.CreateIndex(
                name: "IX_influence_status_history_ChangedByUserId",
                table: "influence_status_history",
                column: "ChangedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_influence_status_history_ContactId",
                table: "influence_status_history",
                column: "ContactId");

            migrationBuilder.CreateIndex(
                name: "IX_interactions_ContactId",
                table: "interactions",
                column: "ContactId");

            migrationBuilder.CreateIndex(
                name: "IX_interactions_CuratorId",
                table: "interactions",
                column: "CuratorId");

            migrationBuilder.CreateIndex(
                name: "IX_interactions_InteractionDate",
                table: "interactions",
                column: "InteractionDate");

            migrationBuilder.CreateIndex(
                name: "IX_interactions_InteractionTypeId",
                table: "interactions",
                column: "InteractionTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_interactions_IsActive",
                table: "interactions",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_interactions_ResultId",
                table: "interactions",
                column: "ResultId");

            migrationBuilder.CreateIndex(
                name: "IX_interactions_UpdatedBy",
                table: "interactions",
                column: "UpdatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_reference_values_Category_Code",
                table: "reference_values",
                columns: new[] { "Category", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_reference_values_Category_IsActive",
                table: "reference_values",
                columns: new[] { "Category", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_reference_values_SortOrder",
                table: "reference_values",
                column: "SortOrder");

            migrationBuilder.CreateIndex(
                name: "IX_users_Login",
                table: "users",
                column: "Login",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_watchlist_IsActive",
                table: "watchlist",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_watchlist_NextCheckDate",
                table: "watchlist",
                column: "NextCheckDate");

            migrationBuilder.CreateIndex(
                name: "IX_watchlist_RiskSphereId",
                table: "watchlist",
                column: "RiskSphereId");

            migrationBuilder.CreateIndex(
                name: "IX_watchlist_UpdatedBy",
                table: "watchlist",
                column: "UpdatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_watchlist_WatchOwnerId",
                table: "watchlist",
                column: "WatchOwnerId");

            migrationBuilder.CreateIndex(
                name: "IX_watchlist_history_ChangedAt",
                table: "watchlist_history",
                column: "ChangedAt");

            migrationBuilder.CreateIndex(
                name: "IX_watchlist_history_ChangedBy",
                table: "watchlist_history",
                column: "ChangedBy");

            migrationBuilder.CreateIndex(
                name: "IX_watchlist_history_WatchlistId",
                table: "watchlist_history",
                column: "WatchlistId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "audit_logs");

            migrationBuilder.DropTable(
                name: "block_curator");

            migrationBuilder.DropTable(
                name: "faqs");

            migrationBuilder.DropTable(
                name: "influence_status_history");

            migrationBuilder.DropTable(
                name: "interactions");

            migrationBuilder.DropTable(
                name: "watchlist_history");

            migrationBuilder.DropTable(
                name: "contacts");

            migrationBuilder.DropTable(
                name: "watchlist");

            migrationBuilder.DropTable(
                name: "blocks");

            migrationBuilder.DropTable(
                name: "reference_values");

            migrationBuilder.DropTable(
                name: "users");
        }
    }
}
