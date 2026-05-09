using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VinhKhanhFood.API.Migrations
{
    /// <inheritdoc />
    public partial class AddAudioListenLog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AudioListenLogs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PoiId = table.Column<int>(type: "int", nullable: false),
                    VisitorId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ListenedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AudioListenLogs", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AudioListenLogs");
        }
    }
}
