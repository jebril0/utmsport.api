using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace api.Migrations
{
    /// <inheritdoc />
    public partial class CreateTimeSlotTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TimeSlots_Venues_VenueID",
                table: "TimeSlots");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Venues",
                table: "Venues");

            migrationBuilder.DropIndex(
                name: "IX_TimeSlots_VenueID",
                table: "TimeSlots");

            migrationBuilder.DropColumn(
                name: "ID",
                table: "Venues");

            migrationBuilder.DropColumn(
                name: "VenueID",
                table: "TimeSlots");

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "Venues",
                type: "nvarchar(450)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AddColumn<bool>(
                name: "IsConfirmed",
                table: "TimeSlots",
                type: "bit",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "VenueName",
                table: "TimeSlots",
                type: "nvarchar(450)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Venues",
                table: "Venues",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_TimeSlots_VenueName",
                table: "TimeSlots",
                column: "VenueName");

            migrationBuilder.AddForeignKey(
                name: "FK_TimeSlots_Venues_VenueName",
                table: "TimeSlots",
                column: "VenueName",
                principalTable: "Venues",
                principalColumn: "Name",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TimeSlots_Venues_VenueName",
                table: "TimeSlots");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Venues",
                table: "Venues");

            migrationBuilder.DropIndex(
                name: "IX_TimeSlots_VenueName",
                table: "TimeSlots");

            migrationBuilder.DropColumn(
                name: "IsConfirmed",
                table: "TimeSlots");

            migrationBuilder.DropColumn(
                name: "VenueName",
                table: "TimeSlots");

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "Venues",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");

            migrationBuilder.AddColumn<int>(
                name: "ID",
                table: "Venues",
                type: "int",
                nullable: false,
                defaultValue: 0)
                .Annotation("SqlServer:Identity", "1, 1");

            migrationBuilder.AddColumn<int>(
                name: "VenueID",
                table: "TimeSlots",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddPrimaryKey(
                name: "PK_Venues",
                table: "Venues",
                column: "ID");

            migrationBuilder.CreateIndex(
                name: "IX_TimeSlots_VenueID",
                table: "TimeSlots",
                column: "VenueID");

            migrationBuilder.AddForeignKey(
                name: "FK_TimeSlots_Venues_VenueID",
                table: "TimeSlots",
                column: "VenueID",
                principalTable: "Venues",
                principalColumn: "ID",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
