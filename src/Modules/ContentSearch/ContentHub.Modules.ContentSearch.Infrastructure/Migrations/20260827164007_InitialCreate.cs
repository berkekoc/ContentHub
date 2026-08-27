using System;
using Microsoft.EntityFrameworkCore.Migrations;
using NpgsqlTypes;

#nullable disable

namespace ContentHub.Modules.ContentSearch.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "content_search");

            migrationBuilder.CreateTable(
                name: "content_items",
                schema: "content_search",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    provider_id = table.Column<Guid>(type: "uuid", nullable: false),
                    external_id = table.Column<string>(type: "text", nullable: false),
                    title = table.Column<string>(type: "text", nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    content_type = table.Column<short>(type: "smallint", nullable: false),
                    published_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    source_url = table.Column<string>(type: "text", nullable: true),
                    fingerprint = table.Column<string>(type: "text", nullable: false),
                    search_vector = table.Column<NpgsqlTsVector>(type: "tsvector", nullable: true, computedColumnSql: "to_tsvector('simple', coalesce(title,'') || ' ' || coalesce(description,''))", stored: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_content_items", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "provider_fetch_runs",
                schema: "content_search",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    provider_id = table.Column<Guid>(type: "uuid", nullable: false),
                    started_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    finished_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    incoming_count = table.Column<int>(type: "integer", nullable: false),
                    new_count = table.Column<int>(type: "integer", nullable: false),
                    updated_count = table.Column<int>(type: "integer", nullable: false),
                    status = table.Column<short>(type: "smallint", nullable: false),
                    error = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_provider_fetch_runs", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "providers",
                schema: "content_search",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    format = table.Column<short>(type: "smallint", nullable: false),
                    base_url = table.Column<string>(type: "text", nullable: false),
                    status = table.Column<short>(type: "smallint", nullable: false),
                    rate_limit_per_minute = table.Column<int>(type: "integer", nullable: false, defaultValue: 60),
                    overflow_behavior = table.Column<short>(type: "smallint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_providers", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "content_metrics",
                schema: "content_search",
                columns: table => new
                {
                    content_item_id = table.Column<Guid>(type: "uuid", nullable: false),
                    views = table.Column<long>(type: "bigint", nullable: true),
                    likes = table.Column<long>(type: "bigint", nullable: true),
                    reading_time = table.Column<int>(type: "integer", nullable: true),
                    reactions = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_content_metrics", x => x.content_item_id);
                    table.ForeignKey(
                        name: "FK_content_metrics_content_items_content_item_id",
                        column: x => x.content_item_id,
                        principalSchema: "content_search",
                        principalTable: "content_items",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "content_scores",
                schema: "content_search",
                columns: table => new
                {
                    content_item_id = table.Column<Guid>(type: "uuid", nullable: false),
                    base_score = table.Column<decimal>(type: "numeric", nullable: false),
                    type_coefficient = table.Column<decimal>(type: "numeric", nullable: false),
                    engagement_score = table.Column<decimal>(type: "numeric", nullable: false),
                    persistent_score = table.Column<decimal>(type: "numeric", nullable: false),
                    computed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_content_scores", x => x.content_item_id);
                    table.ForeignKey(
                        name: "FK_content_scores_content_items_content_item_id",
                        column: x => x.content_item_id,
                        principalSchema: "content_search",
                        principalTable: "content_items",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_content_items_content_type",
                schema: "content_search",
                table: "content_items",
                column: "content_type");

            migrationBuilder.CreateIndex(
                name: "ix_content_items_fingerprint",
                schema: "content_search",
                table: "content_items",
                column: "fingerprint");

            migrationBuilder.CreateIndex(
                name: "ix_content_items_search_vector",
                schema: "content_search",
                table: "content_items",
                column: "search_vector")
                .Annotation("Npgsql:IndexMethod", "gin");

            migrationBuilder.CreateIndex(
                name: "ux_content_items_provider_external",
                schema: "content_search",
                table: "content_items",
                columns: new[] { "provider_id", "external_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_content_scores_persistent_score",
                schema: "content_search",
                table: "content_scores",
                column: "persistent_score");

            migrationBuilder.CreateIndex(
                name: "ix_provider_fetch_runs_provider_started",
                schema: "content_search",
                table: "provider_fetch_runs",
                columns: new[] { "provider_id", "started_at" },
                descending: new[] { false, true });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "content_metrics",
                schema: "content_search");

            migrationBuilder.DropTable(
                name: "content_scores",
                schema: "content_search");

            migrationBuilder.DropTable(
                name: "provider_fetch_runs",
                schema: "content_search");

            migrationBuilder.DropTable(
                name: "providers",
                schema: "content_search");

            migrationBuilder.DropTable(
                name: "content_items",
                schema: "content_search");
        }
    }
}
