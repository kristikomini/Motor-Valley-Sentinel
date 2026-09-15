using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace MotorValley.Backend.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CriticalAlerts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    MachineId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Temperature = table.Column<double>(type: "double precision", nullable: false),
                    ConsecutiveCount = table.Column<int>(type: "integer", nullable: false),
                    Message = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    Timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CriticalAlerts", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CriticalAlerts_MachineId",
                table: "CriticalAlerts",
                column: "MachineId");

            migrationBuilder.CreateIndex(
                name: "IX_CriticalAlerts_Timestamp",
                table: "CriticalAlerts",
                column: "Timestamp");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CriticalAlerts");
        }
    }
}
