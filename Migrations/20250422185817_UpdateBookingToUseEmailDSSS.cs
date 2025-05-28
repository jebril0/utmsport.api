using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace api.Migrations
{
    /// <inheritdoc />
    public partial class UpdateBookingToUseEmailDSSS : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TimeSlots_Users_UserID",
                table: "TimeSlots");

            migrationBuilder.DropColumn(
                name: "IsConfirmed",
                table: "TimeSlots");

            migrationBuilder.DropColumn(
                name: "PaymentScreenshot",
                table: "TimeSlots");

            migrationBuilder.RenameColumn(
                name: "UserID",
                table: "TimeSlots",
                newName: "UsersID");

            migrationBuilder.RenameIndex(
                name: "IX_TimeSlots_UserID",
                table: "TimeSlots",
                newName: "IX_TimeSlots_UsersID");

            migrationBuilder.AlterColumn<byte[]>(
                name: "PaymentScreenshot",
                table: "Bookings",
                type: "varbinary(max)",
                nullable: true,
                oldClrType: typeof(byte[]),
                oldType: "varbinary(max)");

            migrationBuilder.AddForeignKey(
                name: "FK_TimeSlots_Users_UsersID",
                table: "TimeSlots",
                column: "UsersID",
                principalTable: "Users",
                principalColumn: "ID");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TimeSlots_Users_UsersID",
                table: "TimeSlots");

            migrationBuilder.RenameColumn(
                name: "UsersID",
                table: "TimeSlots",
                newName: "UserID");

            migrationBuilder.RenameIndex(
                name: "IX_TimeSlots_UsersID",
                table: "TimeSlots",
                newName: "IX_TimeSlots_UserID");

            migrationBuilder.AddColumn<bool>(
                name: "IsConfirmed",
                table: "TimeSlots",
                type: "bit",
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "PaymentScreenshot",
                table: "TimeSlots",
                type: "varbinary(max)",
                nullable: true);

            migrationBuilder.AlterColumn<byte[]>(
                name: "PaymentScreenshot",
                table: "Bookings",
                type: "varbinary(max)",
                nullable: false,
                defaultValue: new byte[0],
                oldClrType: typeof(byte[]),
                oldType: "varbinary(max)",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_TimeSlots_Users_UserID",
                table: "TimeSlots",
                column: "UserID",
                principalTable: "Users",
                principalColumn: "ID");
        }
    }
}
