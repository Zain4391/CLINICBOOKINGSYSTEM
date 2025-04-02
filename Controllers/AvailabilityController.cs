using Microsoft.AspNetCore.Mvc;
using ClinicBookingSystem.Models;
using ClinicBookingSystem.DTOs;
using ClinicBookingSystem.Services;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace ClinicBookingSystem.Controllers
{
    [Route("/api/availability")]
    [ApiController]

    public class AvailabilityController: ControllerBase
    {
        private readonly SupabaseService _supabase;

        public AvailabilityController(SupabaseService supabase) {
            _supabase = supabase;
        }

        [HttpGet("all")]
        public async Task<IActionResult> GetAllAvailableSlots()
        {
            var slotResult = await _supabase.Client.From<AvailabilitySlot>()
                .Filter("is_booked", Supabase.Postgrest.Constants.Operator.Equals, "false")
                .Order("start_time", Supabase.Postgrest.Constants.Ordering.Ascending)
                .Get();

            var enrichedSlots = new List<object>();

            foreach (var slot in slotResult.Models)
            {
                // Get doctor
                var docResult = await _supabase.Client.From<Doctor>()
                    .Filter("id", Supabase.Postgrest.Constants.Operator.Equals, slot.DoctorId.ToString())
                    .Get();
                var doctor = docResult.Models.FirstOrDefault();

                // Get doctor's user record
                User doctorUser = null;
                if (doctor != null)
                {
                    var userResult = await _supabase.Client.From<User>()
                        .Filter("id", Supabase.Postgrest.Constants.Operator.Equals, doctor.UserId.ToString())
                        .Get();
                    doctorUser = userResult.Models.FirstOrDefault();
                }

                enrichedSlots.Add(new
                {
                    slot.Id,
                    slot.DoctorId,
                    DoctorName = doctorUser?.FullName,
                    DoctorEmail = doctorUser?.Email,
                    slot.StartTime,
                    slot.EndTime,
                    slot.IsBooked
                });
            }

            return Ok(enrichedSlots);
        }


        [Authorize(Roles = "doctor")]
        [HttpPost]
        public async Task<IActionResult> CreateSlot([FromBody] CreateSlotDto model) {

            // get the email through jwt token
            var email = User.FindFirst(ClaimTypes.Name)?.Value;

            // get doctor user from "users" table
            var result = await _supabase.Client.From<User>().Filter("email", Supabase.Postgrest.Constants.Operator.Equals, email).Get();
            var user = result.Models.FirstOrDefault();

            if(user  == null) {
                return Unauthorized(new {message= "Unauthorized user"});
            }

            // get doctor from table
            var docResult = await _supabase.Client.From<Doctor>().Filter("user_id", Supabase.Postgrest.Constants.Operator.Equals, user.Id.ToString()).Get();
            var doctor = docResult.Models.FirstOrDefault();

            if(doctor == null) {
                return NotFound(new {message = "Doctor profile not found"});
            }

            var slot = new AvailabilitySlot {
                Id = Guid.NewGuid(),
                DoctorId = doctor.Id,
                StartTime = model.StartTime,
                EndTime = model.EndTime
            };

            // save the slot entry
            await _supabase.Client.From<AvailabilitySlot>().Insert(slot);

            return Ok(new
            {
                message = "Availability slot created",
                slotId = slot.Id,
                from = slot.StartTime,
                to = slot.EndTime
            });
        }

        [Authorize(Roles = "doctor")]
        [HttpGet("my")]
        public async Task<IActionResult> GetMySlots()
        {
            var email = User.FindFirst(ClaimTypes.Name)?.Value;

            // Get user
            var result = await _supabase.Client.From<User>()
                .Filter("email", Supabase.Postgrest.Constants.Operator.Equals, email)
                .Get();

            var user = result.Models.FirstOrDefault();
            if (user == null)
                return Unauthorized(new { message = "Invalid user" });

            // Get doctor record
            var docResult = await _supabase.Client.From<Doctor>()
                .Filter("user_id", Supabase.Postgrest.Constants.Operator.Equals, user.Id.ToString())
                .Get();

            var doctor = docResult.Models.FirstOrDefault();
            if (doctor == null)
                return NotFound(new { message = "Doctor profile not found" });

            // Fetch all availability slots for doctor
            var slotResult = await _supabase.Client.From<AvailabilitySlot>()
                .Filter("doctor_id", Supabase.Postgrest.Constants.Operator.Equals, doctor.Id.ToString())
                .Order("start_time", Supabase.Postgrest.Constants.Ordering.Ascending)
                .Get();
            var ret = slotResult.Models.FirstOrDefault();

            return Ok(new {
                user.FullName,
                doctor.Id,
                doctor.Bio,
                ret?.StartTime,
                ret?.EndTime
            });
        }

    }
}