using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QuanLyCuaHangMyPham.Migrations
{
    /// <inheritdoc />
    public partial class UpdateOrderNotesColumnType : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
    name: "order_notes",
    table: "Orders",
    type: "nvarchar(max)",
    nullable: true,
    oldClrType: typeof(string),
    oldType: "text",
    oldNullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
