using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QuanLyCuaHangMyPham.Migrations
{
    /// <inheritdoc />
    public partial class FixCa0316 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK__Categorie__paren__5070F446",
                table: "Categories");

            migrationBuilder.DropForeignKey(
                name: "FK__ProductCa__categ__5FB337D6",
                table: "ProductCategories");

            migrationBuilder.DropForeignKey(
                name: "FK__ProductCa__produ__5EBF139D",
                table: "ProductCategories");

            migrationBuilder.DropPrimaryKey(
                name: "PK__Categori__3213E83F14804A61",
                table: "Categories");

            migrationBuilder.DropPrimaryKey(
                name: "PK__ProductC__1A56936EC24E0177",
                table: "ProductCategories");

            migrationBuilder.RenameTable(
                name: "ProductCategories",
                newName: "ProductCategory");

            migrationBuilder.RenameColumn(
                name: "DiscountRate",
                table: "MembershipLevels",
                newName: "discount_rate");

            migrationBuilder.RenameIndex(
                name: "IX_ProductCategories_category_id",
                table: "ProductCategory",
                newName: "IX_ProductCategory_category_id");

            migrationBuilder.AlterColumn<decimal>(
                name: "discount_rate",
                table: "MembershipLevels",
                type: "decimal(10,2)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Categories",
                table: "Categories",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_ProductCategory",
                table: "ProductCategory",
                columns: new[] { "product_id", "category_id" });

            migrationBuilder.CreateTable(
                name: "CategoryProduct",
                columns: table => new
                {
                    CategoriesId = table.Column<int>(type: "int", nullable: false),
                    ProductsId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CategoryProduct", x => new { x.CategoriesId, x.ProductsId });
                    table.ForeignKey(
                        name: "FK_CategoryProduct_Categories_CategoriesId",
                        column: x => x.CategoriesId,
                        principalTable: "Categories",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CategoryProduct_Products_ProductsId",
                        column: x => x.ProductsId,
                        principalTable: "Products",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CategoryProduct_ProductsId",
                table: "CategoryProduct",
                column: "ProductsId");

            migrationBuilder.AddForeignKey(
                name: "FK_Categories_Parent",
                table: "Categories",
                column: "parent_id",
                principalTable: "Categories",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "FK_ProductCategory_Categories_category_id",
                table: "ProductCategory",
                column: "category_id",
                principalTable: "Categories",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ProductCategory_Products_product_id",
                table: "ProductCategory",
                column: "product_id",
                principalTable: "Products",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Categories_Parent",
                table: "Categories");

            migrationBuilder.DropForeignKey(
                name: "FK_ProductCategory_Categories_category_id",
                table: "ProductCategory");

            migrationBuilder.DropForeignKey(
                name: "FK_ProductCategory_Products_product_id",
                table: "ProductCategory");

            migrationBuilder.DropTable(
                name: "CategoryProduct");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Categories",
                table: "Categories");

            migrationBuilder.DropPrimaryKey(
                name: "PK_ProductCategory",
                table: "ProductCategory");

            migrationBuilder.RenameTable(
                name: "ProductCategory",
                newName: "ProductCategories");

            migrationBuilder.RenameColumn(
                name: "discount_rate",
                table: "MembershipLevels",
                newName: "DiscountRate");

            migrationBuilder.RenameIndex(
                name: "IX_ProductCategory_category_id",
                table: "ProductCategories",
                newName: "IX_ProductCategories_category_id");

            migrationBuilder.AlterColumn<decimal>(
                name: "DiscountRate",
                table: "MembershipLevels",
                type: "decimal(18,2)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(10,2)");

            migrationBuilder.AddPrimaryKey(
                name: "PK__Categori__3213E83F14804A61",
                table: "Categories",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK__ProductC__1A56936EC24E0177",
                table: "ProductCategories",
                columns: new[] { "product_id", "category_id" });

            migrationBuilder.AddForeignKey(
                name: "FK__Categorie__paren__5070F446",
                table: "Categories",
                column: "parent_id",
                principalTable: "Categories",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK__ProductCa__categ__5FB337D6",
                table: "ProductCategories",
                column: "category_id",
                principalTable: "Categories",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK__ProductCa__produ__5EBF139D",
                table: "ProductCategories",
                column: "product_id",
                principalTable: "Products",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
