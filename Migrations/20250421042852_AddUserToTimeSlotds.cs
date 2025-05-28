using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace api.Migrations
{
    /// <inheritdoc />
    public partial class AddUserToTimeSlotds : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "UserID",
                table: "TimeSlots",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_TimeSlots_UserID",
                table: "TimeSlots",
                column: "UserID");

            migrationBuilder.AddForeignKey(
                name: "FK_TimeSlots_Users_UserID",
                table: "TimeSlots",
                column: "UserID",
                principalTable: "Users",
                principalColumn: "ID");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TimeSlots_Users_UserID",
                table: "TimeSlots");

            migrationBuilder.DropIndex(
                name: "IX_TimeSlots_UserID",
                table: "TimeSlots");

            migrationBuilder.DropColumn(
                name: "UserID",
                table: "TimeSlots");
        }
    }
}
