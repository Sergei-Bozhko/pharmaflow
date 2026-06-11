using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PharmaFlow.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddKnownStudies : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "known_studies",
                columns: table => new
                {
                    study_id = table.Column<Guid>(type: "uuid", nullable: false),
                    registered_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_known_studies", x => x.study_id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "known_studies");
        }
    }
}
