using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DoctorAppointment.Infrastructures.Migrations
{
    /// <inheritdoc />
    public partial class manytomanyhospitaluser : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Hospitals_Users_UserId",
                table: "Hospitals");

            migrationBuilder.DropIndex(
                name: "IX_Hospitals_UserId",
                table: "Hospitals");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "Hospitals");

            migrationBuilder.CreateTable(
                name: "HospitalUser",
                columns: table => new
                {
                    HospitalsId = table.Column<int>(type: "int", nullable: false),
                    UsersId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HospitalUser", x => new { x.HospitalsId, x.UsersId });
                    table.ForeignKey(
                        name: "FK_HospitalUser_Hospitals_HospitalsId",
                        column: x => x.HospitalsId,
                        principalTable: "Hospitals",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_HospitalUser_Users_UsersId",
                        column: x => x.UsersId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_HospitalUser_UsersId",
                table: "HospitalUser",
                column: "UsersId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "HospitalUser");

            migrationBuilder.AddColumn<int>(
                name: "UserId",
                table: "Hospitals",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Hospitals_UserId",
                table: "Hospitals",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_Hospitals_Users_UserId",
                table: "Hospitals",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id");
        }
    }
}
