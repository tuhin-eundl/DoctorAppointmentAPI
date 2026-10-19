using DoctorAppointment.Application.DTOs.Hospital;
using DoctorAppointment.Domain;

namespace DoctorAppointment.Application.Profiles.Hospitals;

    public static class HospitalProfiles
    {
        public static Hospital DtoToEntity(HospitalDto dto)
        {
            return new Hospital
            {
                Id = dto.Id ?? 0,
                Name = dto.Name,
                Address = dto.Address,
                Longitude = dto.Longitude,
                Latitude = dto.Latitude
            };
        }

        public static HospitalDto EntityToDto(Hospital entity)
        {
            return new HospitalDto(
                entity.Id,
                entity.Name,
                entity.Address,
                entity.Longitude,
                entity.Latitude);
        }
    }

