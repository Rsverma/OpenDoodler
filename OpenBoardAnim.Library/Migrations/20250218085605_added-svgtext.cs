using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OpenBoardAnim.Library.Migrations
{
    /// <inheritdoc />
    public partial class addedsvgtext : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "FilePath",
                table: "Graphics",
                newName: "SVGText");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "SVGText",
                table: "Graphics",
                newName: "FilePath");
        }
    }
}
