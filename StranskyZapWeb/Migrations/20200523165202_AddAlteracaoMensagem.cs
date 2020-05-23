using Microsoft.EntityFrameworkCore.Migrations;

namespace StranskyZapWeb.Migrations
{
    public partial class AddAlteracaoMensagem : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "UsuarioId",
                table: "Mensagens",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "TEXT",
                oldNullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "UsuarioId",
                table: "Mensagens",
                type: "TEXT",
                nullable: true,
                oldClrType: typeof(int));
        }
    }
}
