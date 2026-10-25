using DoctorAppointment.Application.Contracts;
using DoctorAppointment.Application.DTOs.Hospital;
using DoctorAppointment.Application.Profiles.Hospitals;
using Microsoft.AspNetCore.Mvc;

namespace DoctorAppointment.Service.Controllers;

[Route("api/[controller]")]
[ApiController]
public class HospitalController : ControllerBase
{
    private readonly IHospitalService _hospitalService;

    public HospitalController(IHospitalService hospitalService)
    {
        _hospitalService = hospitalService;
    }

    [HttpGet]
    public async Task<IActionResult> GetHospitals()
    {
        var hospitals = await _hospitalService.ListAsync();
        return Ok(hospitals.Select(HospitalProfiles.EntityToDto));
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetHospital(int id)
    {
        var hospital = await _hospitalService.GetAsync(id);
        if (hospital == null)
        {
            return NotFound();
        }

        return Ok(HospitalProfiles.EntityToDto(hospital));
    }

    [HttpPost]
    public async Task<IActionResult> CreateHospital([FromBody] HospitalDto dto)
    {
        var hospital = HospitalProfiles.DtoToEntity(dto);
        await _hospitalService.AddAsync(hospital);
        return CreatedAtAction(nameof(GetHospital), new { id = hospital.Id }, HospitalProfiles.EntityToDto(hospital));
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateHospital(int id, [FromBody] HospitalDto dto)
    {
        var hospital = await _hospitalService.GetAsync(id);
        if (hospital == null)
        {
            return NotFound();
        }

        hospital.Name = dto.Name;
        hospital.Address = dto.Address;
        hospital.Longitude = dto.Longitude;
        hospital.Latitude = dto.Latitude;

        await _hospitalService.UpdateAsync(hospital);
        return Ok(HospitalProfiles.EntityToDto(hospital));
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteHospital(int id)
    {
        var hospital = await _hospitalService.GetAsync(id);
        if (hospital == null)
        {
            return NotFound();
        }

        await _hospitalService.DeleteAsync(hospital);
        return Ok(HospitalProfiles.EntityToDto(hospital));
    }
}
