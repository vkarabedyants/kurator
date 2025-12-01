using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

namespace Kurator.Infrastructure.Data.Migrations
{
    public partial class InitialCreate : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Create extension for cryptography (optional for future use)
            migrationBuilder.Sql("CREATE EXTENSION IF NOT EXISTS pgcrypto;");

            // Create enum types
            migrationBuilder.Sql(@"
                DO $$ BEGIN
                    CREATE TYPE user_role AS ENUM ('Admin', 'Curator', 'ThreatAnalyst');
                EXCEPTION
                    WHEN duplicate_object THEN null;
                END $$;
            ");

            migrationBuilder.Sql(@"
                DO $$ BEGIN
                    CREATE TYPE block_status AS ENUM ('Active', 'Archived');
                EXCEPTION
                    WHEN duplicate_object THEN null;
                END $$;
            ");

            migrationBuilder.Sql(@"
                DO $$ BEGIN
                    CREATE TYPE curator_type AS ENUM ('Primary', 'Backup');
                EXCEPTION
                    WHEN duplicate_object THEN null;
                END $$;
            ");

            migrationBuilder.Sql(@"
                DO $$ BEGIN
                    CREATE TYPE risk_level AS ENUM ('Low', 'Medium', 'High', 'Critical');
                EXCEPTION
                    WHEN duplicate_object THEN null;
                END $$;
            ");

            migrationBuilder.Sql(@"
                DO $$ BEGIN
                    CREATE TYPE monitoring_frequency AS ENUM ('Weekly', 'Monthly', 'Quarterly', 'AdHoc');
                EXCEPTION
                    WHEN duplicate_object THEN null;
                END $$;
            ");

            // Create users table
            migrationBuilder.CreateTable(
                name: "users",
                columns: table => new
                {
                    id = table.Column<int>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    login = table.Column<string>(maxLength: 100, nullable: false),
                    password_hash = table.Column<string>(nullable: false),
                    role = table.Column<string>(nullable: false),
                    public_key = table.Column<string>(maxLength: 4000, nullable: true),
                    mfa_enabled = table.Column<bool>(nullable: false, defaultValue: false),
                    mfa_secret = table.Column<string>(maxLength: 500, nullable: true),
                    is_first_login = table.Column<bool>(nullable: false, defaultValue: true),
                    last_login_at = table.Column<DateTime>(nullable: true),
                    is_active = table.Column<bool>(nullable: false, defaultValue: true),
                    created_at = table.Column<DateTime>(nullable: false),
                    created_by = table.Column<int>(nullable: true),
                    updated_at = table.Column<DateTime>(nullable: true),
                    updated_by = table.Column<int>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_users", x => x.id);
                });

            // Create blocks table
            migrationBuilder.CreateTable(
                name: "blocks",
                columns: table => new
                {
                    id = table.Column<int>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    name = table.Column<string>(maxLength: 200, nullable: false),
                    code = table.Column<string>(maxLength: 20, nullable: false),
                    description = table.Column<string>(nullable: true),
                    status = table.Column<string>(nullable: false, defaultValue: "Active"),
                    created_at = table.Column<DateTime>(nullable: false),
                    created_by = table.Column<int>(nullable: true),
                    updated_at = table.Column<DateTime>(nullable: true),
                    updated_by = table.Column<int>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_blocks", x => x.id);
                });

            // Create reference_values table
            migrationBuilder.CreateTable(
                name: "reference_values",
                columns: table => new
                {
                    id = table.Column<int>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    category = table.Column<string>(maxLength: 100, nullable: false),
                    code = table.Column<string>(maxLength: 50, nullable: false),
                    value = table.Column<string>(maxLength: 200, nullable: false),
                    description = table.Column<string>(nullable: true),
                    sort_order = table.Column<int>(nullable: false, defaultValue: 0),
                    is_active = table.Column<bool>(nullable: false, defaultValue: true),
                    created_at = table.Column<DateTime>(nullable: false),
                    created_by = table.Column<int>(nullable: true),
                    updated_at = table.Column<DateTime>(nullable: true),
                    updated_by = table.Column<int>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_reference_values", x => x.id);
                });

            // Create block_curator table
            migrationBuilder.CreateTable(
                name: "block_curator",
                columns: table => new
                {
                    id = table.Column<int>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    block_id = table.Column<int>(nullable: false),
                    user_id = table.Column<int>(nullable: false),
                    curator_type = table.Column<string>(nullable: false),
                    assigned_at = table.Column<DateTime>(nullable: false),
                    assigned_by = table.Column<int>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_block_curator", x => x.id);
                    table.ForeignKey(
                        name: "FK_block_curator_blocks_block_id",
                        column: x => x.block_id,
                        principalTable: "blocks",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_block_curator_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_block_curator_users_assigned_by",
                        column: x => x.assigned_by,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            // Create contacts table
            migrationBuilder.CreateTable(
                name: "contacts",
                columns: table => new
                {
                    id = table.Column<int>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    contact_id = table.Column<string>(maxLength: 50, nullable: false),
                    block_id = table.Column<int>(nullable: false),
                    full_name_encrypted = table.Column<string>(nullable: false),
                    organization_id = table.Column<string>(maxLength: 100, nullable: true),
                    position = table.Column<string>(maxLength: 200, nullable: true),
                    influence_status = table.Column<string>(maxLength: 1, nullable: true),
                    influence_type_id = table.Column<string>(maxLength: 50, nullable: true),
                    channel_id = table.Column<string>(maxLength: 50, nullable: true),
                    source_id = table.Column<string>(maxLength: 50, nullable: true),
                    last_touch_date = table.Column<DateTime>(nullable: true),
                    next_touch_date = table.Column<DateTime>(nullable: true),
                    responsible_curator_id = table.Column<int>(nullable: true),
                    notes_encrypted = table.Column<string>(nullable: true),
                    is_active = table.Column<bool>(nullable: false, defaultValue: true),
                    created_at = table.Column<DateTime>(nullable: false),
                    created_by = table.Column<int>(nullable: true),
                    updated_at = table.Column<DateTime>(nullable: true),
                    updated_by = table.Column<int>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_contacts", x => x.id);
                    table.ForeignKey(
                        name: "FK_contacts_blocks_block_id",
                        column: x => x.block_id,
                        principalTable: "blocks",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_contacts_users_responsible_curator_id",
                        column: x => x.responsible_curator_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            // Create interactions table
            migrationBuilder.CreateTable(
                name: "interactions",
                columns: table => new
                {
                    id = table.Column<int>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    contact_id = table.Column<int>(nullable: false),
                    interaction_date = table.Column<DateTime>(nullable: false),
                    interaction_type_id = table.Column<string>(maxLength: 50, nullable: true),
                    result_id = table.Column<string>(maxLength: 50, nullable: true),
                    comment_encrypted = table.Column<string>(nullable: true),
                    attachments = table.Column<string>(nullable: true),
                    next_touch_date = table.Column<DateTime>(nullable: true),
                    influence_status_change_from = table.Column<string>(maxLength: 1, nullable: true),
                    influence_status_change_to = table.Column<string>(maxLength: 1, nullable: true),
                    is_active = table.Column<bool>(nullable: false, defaultValue: true),
                    created_at = table.Column<DateTime>(nullable: false),
                    created_by = table.Column<int>(nullable: true),
                    updated_at = table.Column<DateTime>(nullable: true),
                    updated_by = table.Column<int>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_interactions", x => x.id);
                    table.ForeignKey(
                        name: "FK_interactions_contacts_contact_id",
                        column: x => x.contact_id,
                        principalTable: "contacts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            // Create influence_status_histories table
            migrationBuilder.CreateTable(
                name: "influence_status_histories",
                columns: table => new
                {
                    id = table.Column<int>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    contact_id = table.Column<int>(nullable: false),
                    from_status = table.Column<string>(maxLength: 1, nullable: true),
                    to_status = table.Column<string>(maxLength: 1, nullable: false),
                    changed_at = table.Column<DateTime>(nullable: false),
                    changed_by = table.Column<int>(nullable: false),
                    interaction_id = table.Column<int>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_influence_status_histories", x => x.id);
                    table.ForeignKey(
                        name: "FK_influence_status_histories_contacts_contact_id",
                        column: x => x.contact_id,
                        principalTable: "contacts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_influence_status_histories_users_changed_by",
                        column: x => x.changed_by,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_influence_status_histories_interactions_interaction_id",
                        column: x => x.interaction_id,
                        principalTable: "interactions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            // Create watchlists table
            migrationBuilder.CreateTable(
                name: "watchlists",
                columns: table => new
                {
                    id = table.Column<int>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    full_name = table.Column<string>(maxLength: 200, nullable: false),
                    role_status = table.Column<string>(maxLength: 200, nullable: true),
                    risk_area_id = table.Column<string>(maxLength: 100, nullable: true),
                    threat_source = table.Column<string>(nullable: true),
                    threat_date = table.Column<DateTime>(nullable: true),
                    risk_level = table.Column<string>(nullable: false, defaultValue: "Low"),
                    monitoring_frequency = table.Column<string>(nullable: false, defaultValue: "Monthly"),
                    last_check_date = table.Column<DateTime>(nullable: true),
                    next_check_date = table.Column<DateTime>(nullable: true),
                    dynamics = table.Column<string>(nullable: true),
                    responsible_analyst_id = table.Column<int>(nullable: true),
                    attachments = table.Column<string>(nullable: true),
                    created_at = table.Column<DateTime>(nullable: false),
                    created_by = table.Column<int>(nullable: true),
                    updated_at = table.Column<DateTime>(nullable: true),
                    updated_by = table.Column<int>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_watchlists", x => x.id);
                    table.ForeignKey(
                        name: "FK_watchlists_users_responsible_analyst_id",
                        column: x => x.responsible_analyst_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            // Create watchlist_histories table
            migrationBuilder.CreateTable(
                name: "watchlist_histories",
                columns: table => new
                {
                    id = table.Column<int>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    watchlist_id = table.Column<int>(nullable: false),
                    change_type = table.Column<string>(maxLength: 50, nullable: false),
                    field_name = table.Column<string>(maxLength: 50, nullable: true),
                    old_value = table.Column<string>(nullable: true),
                    new_value = table.Column<string>(nullable: true),
                    changed_at = table.Column<DateTime>(nullable: false),
                    changed_by = table.Column<int>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_watchlist_histories", x => x.id);
                    table.ForeignKey(
                        name: "FK_watchlist_histories_watchlists_watchlist_id",
                        column: x => x.watchlist_id,
                        principalTable: "watchlists",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_watchlist_histories_users_changed_by",
                        column: x => x.changed_by,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            // Create faqs table
            migrationBuilder.CreateTable(
                name: "faqs",
                columns: table => new
                {
                    id = table.Column<int>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    category = table.Column<string>(maxLength: 100, nullable: false),
                    question = table.Column<string>(nullable: false),
                    answer = table.Column<string>(nullable: false),
                    sort_order = table.Column<int>(nullable: false, defaultValue: 0),
                    is_active = table.Column<bool>(nullable: false, defaultValue: true),
                    created_at = table.Column<DateTime>(nullable: false),
                    created_by = table.Column<int>(nullable: true),
                    updated_at = table.Column<DateTime>(nullable: true),
                    updated_by = table.Column<int>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_faqs", x => x.id);
                });

            // Create audit_logs table
            migrationBuilder.CreateTable(
                name: "audit_logs",
                columns: table => new
                {
                    id = table.Column<int>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    user_id = table.Column<int>(nullable: false),
                    action_type = table.Column<string>(maxLength: 50, nullable: false),
                    entity_type = table.Column<string>(maxLength: 50, nullable: false),
                    entity_id = table.Column<int>(nullable: false),
                    old_value_encrypted = table.Column<string>(nullable: true),
                    new_value_encrypted = table.Column<string>(nullable: true),
                    ip_address = table.Column<string>(maxLength: 45, nullable: true),
                    user_agent = table.Column<string>(nullable: true),
                    timestamp = table.Column<DateTime>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_audit_logs", x => x.id);
                    table.ForeignKey(
                        name: "FK_audit_logs_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            // Create indexes
            migrationBuilder.CreateIndex(
                name: "IX_users_login",
                table: "users",
                column: "login",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_blocks_code",
                table: "blocks",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_block_curator_block_id_user_id",
                table: "block_curator",
                columns: new[] { "block_id", "user_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_block_curator_block_id_curator_type",
                table: "block_curator",
                columns: new[] { "block_id", "curator_type" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_block_curator_user_id",
                table: "block_curator",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_block_curator_assigned_by",
                table: "block_curator",
                column: "assigned_by");

            migrationBuilder.CreateIndex(
                name: "IX_contacts_contact_id",
                table: "contacts",
                column: "contact_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_contacts_block_id",
                table: "contacts",
                column: "block_id");

            migrationBuilder.CreateIndex(
                name: "IX_contacts_next_touch_date",
                table: "contacts",
                column: "next_touch_date");

            migrationBuilder.CreateIndex(
                name: "IX_contacts_responsible_curator_id",
                table: "contacts",
                column: "responsible_curator_id");

            migrationBuilder.CreateIndex(
                name: "IX_interactions_contact_id",
                table: "interactions",
                column: "contact_id");

            migrationBuilder.CreateIndex(
                name: "IX_interactions_interaction_date",
                table: "interactions",
                column: "interaction_date",
                descending: new[] { true });

            migrationBuilder.CreateIndex(
                name: "IX_influence_status_histories_contact_id",
                table: "influence_status_histories",
                column: "contact_id");

            migrationBuilder.CreateIndex(
                name: "IX_influence_status_histories_changed_by",
                table: "influence_status_histories",
                column: "changed_by");

            migrationBuilder.CreateIndex(
                name: "IX_influence_status_histories_interaction_id",
                table: "influence_status_histories",
                column: "interaction_id");

            migrationBuilder.CreateIndex(
                name: "IX_watchlists_responsible_analyst_id",
                table: "watchlists",
                column: "responsible_analyst_id");

            migrationBuilder.CreateIndex(
                name: "IX_watchlist_histories_watchlist_id",
                table: "watchlist_histories",
                column: "watchlist_id");

            migrationBuilder.CreateIndex(
                name: "IX_watchlist_histories_changed_by",
                table: "watchlist_histories",
                column: "changed_by");

            migrationBuilder.CreateIndex(
                name: "IX_audit_logs_user_id",
                table: "audit_logs",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_audit_logs_timestamp",
                table: "audit_logs",
                column: "timestamp",
                descending: new[] { true });

            migrationBuilder.CreateIndex(
                name: "IX_reference_values_category_code",
                table: "reference_values",
                columns: new[] { "category", "code" },
                unique: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "audit_logs");
            migrationBuilder.DropTable(name: "faqs");
            migrationBuilder.DropTable(name: "watchlist_histories");
            migrationBuilder.DropTable(name: "watchlists");
            migrationBuilder.DropTable(name: "influence_status_histories");
            migrationBuilder.DropTable(name: "interactions");
            migrationBuilder.DropTable(name: "contacts");
            migrationBuilder.DropTable(name: "block_curator");
            migrationBuilder.DropTable(name: "reference_values");
            migrationBuilder.DropTable(name: "blocks");
            migrationBuilder.DropTable(name: "users");

            // Drop enum types
            migrationBuilder.Sql("DROP TYPE IF EXISTS user_role;");
            migrationBuilder.Sql("DROP TYPE IF EXISTS block_status;");
            migrationBuilder.Sql("DROP TYPE IF EXISTS curator_type;");
            migrationBuilder.Sql("DROP TYPE IF EXISTS risk_level;");
            migrationBuilder.Sql("DROP TYPE IF EXISTS monitoring_frequency;");
        }
    }
}