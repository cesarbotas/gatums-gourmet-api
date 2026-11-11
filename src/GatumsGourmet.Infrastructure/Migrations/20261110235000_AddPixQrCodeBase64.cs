using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GatumsGourmet.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPixQrCodeBase64 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PixQrCodeBase64",
                table: "TransacoesPagamento",
                type: "text",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_TransacoesPagamento_OperadoraRef",
                table: "TransacoesPagamento",
                column: "OperadoraRef");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_TransacoesPagamento_OperadoraRef",
                table: "TransacoesPagamento");

            migrationBuilder.DropColumn(
                name: "PixQrCodeBase64",
                table: "TransacoesPagamento");
        }
    }
}

