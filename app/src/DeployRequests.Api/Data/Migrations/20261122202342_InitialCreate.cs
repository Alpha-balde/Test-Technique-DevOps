using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DeployRequests.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "deployment_requests",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    application = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    application_normalized = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    environment = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    version = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_deployment_requests", x => x.id);
                    table.CheckConstraint("ck_deployment_requests_environment", "environment IN ('staging', 'production')");
                });

            migrationBuilder.CreateIndex(
                name: "ux_deployment_requests_application_environment_version",
                table: "deployment_requests",
                columns: new[] { "application_normalized", "environment", "version" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "deployment_requests");
        }
    }
}
