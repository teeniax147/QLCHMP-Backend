using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QuanLyCuaHangMyPham.Migrations
{
    /// <inheritdoc />
    public partial class Lan8 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK__Admins__user_id__0D7A0286",
                table: "Admins");

            migrationBuilder.AddForeignKey(
                name: "FK_Admins_AspNetUsers_user_id",
                table: "Admins",
                column: "user_id",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Admins_AspNetUsers_user_id",
                table: "Admins");

            migrationBuilder.AddForeignKey(
                name: "FK__Admins__user_id__0D7A0286",
                table: "Admins",
                column: "user_id",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
