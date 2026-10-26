using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DoctorAppointment.Infrastructures.Migrations
{
    /// <inheritdoc />
    public partial class approved_only_slot_lock : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Appointments_ScheduleDayId_SerialNumber",
                table: "Appointments");

            migrationBuilder.CreateIndex(
                name: "IX_Appointments_ScheduleDayId_SerialNumber",
                table: "Appointments",
                columns: new[] { "ScheduleDayId", "SerialNumber" },
                unique: true,
                filter: "[Status] = 1 AND [IsDeleted] = 0");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Appointments_ScheduleDayId_SerialNumber",
                table: "Appointments");

            migrationBuilder.CreateIndex(
                name: "IX_Appointments_ScheduleDayId_SerialNumber",
                table: "Appointments",
                columns: new[] { "ScheduleDayId", "SerialNumber" },
                unique: true,
                filter: "[Status] IN (0, 1) AND [IsDeleted] = 0");
        }
    }
}
