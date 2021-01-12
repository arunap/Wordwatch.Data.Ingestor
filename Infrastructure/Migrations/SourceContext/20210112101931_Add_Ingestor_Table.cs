using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Wordwatch.Data.Ingestor.Infrastructure.Migrations.SourceContext
{
    public partial class Add_Ingestor_Table : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "dbo");

            migrationBuilder.CreateTable(
                name: "IngestorInfo",
                schema: "dbo",
                columns: table => new
                {
                    id = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    call_id = table.Column<Guid>(nullable: false),
                    channel_key = table.Column<string>(nullable: true),
                    call_type = table.Column<short>(nullable: false),
                    start_datetime = table.Column<DateTimeOffset>(nullable: false),
                    stop_datetime = table.Column<DateTimeOffset>(nullable: false),
                    DataIngestStatus = table.Column<int>(nullable: false, defaultValue: 0),
                    SyncedToElastic = table.Column<bool>(nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IngestorInfo", x => x.id);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "IngestorInfo",
                schema: "dbo");
        }
    }
}
