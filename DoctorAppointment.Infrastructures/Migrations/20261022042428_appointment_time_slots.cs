using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DoctorAppointment.Infrastructures.Migrations
{
    /// <inheritdoc />
    public partial class appointment_time_slots : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CancelledById",
                table: "Appointments",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<TimeOnly>(
                name: "StartTime",
                table: "Appointments",
                type: "time",
                nullable: false,
                defaultValue: new TimeOnly(0, 0, 0));

            // Existing appointments were queue places: give each the start of its slot
            // (day start + (serial - 1) x appointment length).
            migrationBuilder.Sql(@"
UPDATE a
SET a.StartTime = DATEADD(minute, (a.SerialNumber - 1) * s.AverageAppointmentMinutes, d.StartTime)
FROM Appointments a
JOIN DoctorScheduleDays d ON d.Id = a.ScheduleDayId
JOIN DoctorSchedules s ON s.Id = d.DoctorScheduleId;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CancelledById",
                table: "Appointments");

            migrationBuilder.DropColumn(
                name: "StartTime",
                table: "Appointments");
        }
    }
}
