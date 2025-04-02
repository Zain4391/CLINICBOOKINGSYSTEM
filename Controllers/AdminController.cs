using Microsoft.AspNetCore.Mvc;
using ClinicBookingSystem.Models;
using ClinicBookingSystem.DTOs;
using ClinicBookingSystem.Services;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace ClinicBookingSystem.Controllers
{
    [Route("api/admin")]
    [ApiController]
    public class AdminController : ControllerBase
    {
        private readonly SupabaseService _supabase;

        public AdminController(SupabaseService supabase)
        {
            _supabase = supabase;
        }

        [Authorize(Roles = "admin")]
        [HttpGet("appointments")]
        public async Task<IActionResult> GetAllAppointments()
        {
            var appointmentResult = await _supabase.Client.From<Appointment>()
                .Order("created_at", Supabase.Postgrest.Constants.Ordering.Descending)
                .Get();

            var enrichedAppointments = new List<object>();

            foreach (var appt in appointmentResult.Models)
            {
                // Get doctor
                var docResult = await _supabase.Client.From<Doctor>()
                    .Filter("id", Supabase.Postgrest.Constants.Operator.Equals, appt.DoctorId.ToString())
                    .Get();
                var doctor = docResult.Models.FirstOrDefault();

                // Get doctor user info
                User doctorUser = null;
                if (doctor != null)
                {
                    var userResult = await _supabase.Client.From<User>()
                        .Filter("id", Supabase.Postgrest.Constants.Operator.Equals, doctor.UserId.ToString())
                        .Get();
                    doctorUser = userResult.Models.FirstOrDefault();
                }

                // Get slot
                var slotResult = await _supabase.Client.From<AvailabilitySlot>()
                    .Filter("id", Supabase.Postgrest.Constants.Operator.Equals, appt.SlotId.ToString())
                    .Get();
                var slot = slotResult.Models.FirstOrDefault();

                enrichedAppointments.Add(new
                {
                    AppointmentId = appt.Id,
                    Status = appt.Status,
                    CreatedAt = appt.CreatedAt,
                    DoctorName = doctorUser?.FullName,
                    DoctorEmail = doctorUser?.Email,
                    SlotStart = slot?.StartTime,
                    SlotEnd = slot?.EndTime
                });
            }

            return Ok(enrichedAppointments);
        }

        [Authorize(Roles = "admin")]
        [HttpGet("doctors")]
        public async Task<IActionResult> GetAllDoctors()
        {
            var result = await _supabase.Client.From<Doctor>().Get();

            var cleanDoctors = new List<object>();

            foreach (var doctor in result.Models)
            {
                var userResult = await _supabase.Client.From<User>()
                    .Filter("id", Supabase.Postgrest.Constants.Operator.Equals, doctor.UserId.ToString())
                    .Get();

                var user = userResult.Models.FirstOrDefault();

                cleanDoctors.Add(new
                {
                    DoctorId = doctor.Id,
                    Name = user?.FullName,
                    Email = user?.Email,
                    SpecializationId = doctor.SpecializationId,
                    Bio = doctor.Bio
                });
            }

            return Ok(cleanDoctors);
        }

        [Authorize(Roles = "admin")]
        [HttpGet("patients")]
        public async Task<IActionResult> GetAllPatients()
        {
            var result = await _supabase.Client.From<Patient>().Get();

            var cleanPatients = new List<object>();

            foreach (var patient in result.Models)
            {
                var userResult = await _supabase.Client.From<User>()
                    .Filter("id", Supabase.Postgrest.Constants.Operator.Equals, patient.UserId.ToString())
                    .Get();

                var user = userResult.Models.FirstOrDefault();

                cleanPatients.Add(new
                {
                    PatientId = patient.Id,
                    Name = user?.FullName,
                    Email = user?.Email,
                    Gender = patient.Gender,
                    Dob = patient.Dob
                });
            }

            return Ok(cleanPatients);
        }



        // 4️⃣ Basic stats
        [Authorize(Roles = "admin")]
        [HttpGet("stats")]
        public async Task<IActionResult> GetStats()
        {
            var appointments = await _supabase.Client.From<Appointment>().Get();
            var doctors = await _supabase.Client.From<Doctor>().Get();
            var patients = await _supabase.Client.From<Patient>().Get();
            var slots = await _supabase.Client.From<AvailabilitySlot>().Get();

            return Ok(new
            {
                TotalAppointments = appointments.Models.Count,
                TotalDoctors = doctors.Models.Count,
                TotalPatients = patients.Models.Count,
                TotalSlots = slots.Models.Count
            });
        }
    }

}