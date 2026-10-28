using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EverestRemit.Api.Migrations
{
    /// <inheritdoc />
    public partial class sendMoney : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Transfers_Deposits_DepositId",
                table: "Transfers");

            migrationBuilder.DropIndex(
                name: "IX_Transfers_DepositId",
                table: "Transfers");

            migrationBuilder.DropColumn(
                name: "DepositId",
                table: "Transfers");

            migrationBuilder.AddColumn<string>(
                name: "ProofUrl",
                table: "Transfers",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ProofUrl",
                table: "Transfers");

            migrationBuilder.AddColumn<int>(
                name: "DepositId",
                table: "Transfers",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Transfers_DepositId",
                table: "Transfers",
                column: "DepositId");

            migrationBuilder.AddForeignKey(
                name: "FK_Transfers_Deposits_DepositId",
                table: "Transfers",
                column: "DepositId",
                principalTable: "Deposits",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
