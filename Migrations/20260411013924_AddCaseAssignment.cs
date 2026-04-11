using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace secure_workflow_system.Migrations
{
    /// <inheritdoc />
    public partial class AddCaseAssignment : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AssignedToUserId",
                table: "Cases",
                type: "text",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Cases_AssignedToUserId",
                table: "Cases",
                column: "AssignedToUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_Cases_AspNetUsers_AssignedToUserId",
                table: "Cases",
                column: "AssignedToUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Cases_AspNetUsers_AssignedToUserId",
                table: "Cases");

            migrationBuilder.DropIndex(
                name: "IX_Cases_AssignedToUserId",
                table: "Cases");

            migrationBuilder.DropColumn(
                name: "AssignedToUserId",
                table: "Cases");
        }
    }
}
