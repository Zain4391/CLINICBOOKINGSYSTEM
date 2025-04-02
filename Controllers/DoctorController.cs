using Microsoft.AspNetCore.Mvc;
using ClinicBookingSystem.Models;
using ClinicBookingSystem.DTOs;
using ClinicBookingSystem.Services;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace ClinicBookingSystem.Controllers
{
    [Route("/api/doctors")]
    [ApiController]
    public class DoctorController: ControllerBase
    {
        private readonly SupabaseService _supabase;
        private readonly EmailService _emailService;

        public DoctorController(SupabaseService supabase, EmailService emailService) {
            _supabase = supabase;
            _emailService = emailService;
        }

        [Authorize(Roles = "doctor")]
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] DoctorRegisterDto model) {
            // 1. get the email from the token
            var email = User.FindFirst(ClaimTypes.Name)?.Value;

            // 2. check if the dcotor exist in the users table
            var result = await _supabase.Client.From<User>().Filter("email", Supabase.Postgrest.Constants.Operator.Equals, email).Get();

            var user = result.Models.FirstOrDefault();
            if(user == null) {
                return NotFound(new {message = $"Doctor with email {email} not found"});
            }

            var specResult = await _supabase.Client.From<Specialization>().Filter("id", Supabase.Postgrest.Constants.Operator.Equals, model.SpecializationId.ToString()).Get();

            if (!specResult.Models.Any())
            {
                return BadRequest(new { message = "Invalid specialization ID." });
            }

            // create DB entry
            var doctor = new Doctor {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                Bio = model.Bio,
                SpecializationId = model.SpecializationId
            };

            var instertedValue = await _supabase.Client.From<Doctor>().Insert(doctor);
            await _emailService.SendEmailAsync(
                user.Email,
                "Doctor Registration Successful",
                $"Hi Dr. {user.FullName}, your profile has been registered successfully. Welcome aboard!"
            );
            return Ok(new {
                message = "Doctor registered successfully",
                doctorID = doctor.Id,
                name = user.FullName
            });
        }

        [Authorize]
        [HttpGet("me")]
        public async Task<IActionResult> GetcurrentDoctor() {
            var email = User.FindFirst(ClaimTypes.Name)?.Value;
            if(string.IsNullOrEmpty(email)) {
                return Unauthorized(new {message = "Missing or invalid token"});
            }

            var result = await _supabase.Client.From<User>().Filter("email", Supabase.Postgrest.Constants.Operator.Equals, email).Get();

            var user = result.Models.FirstOrDefault();
            if(user == null) {
                return NotFound(new {message = $"Doctor with email {email} not found"});
            }

            return Ok(new {
                email,
                user.FullName,
                user.Role
            });
        }
    }
}