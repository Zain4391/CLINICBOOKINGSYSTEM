using Microsoft.AspNetCore.Mvc;
using ClinicBookingSystem.Models;
using ClinicBookingSystem.DTOs;
using ClinicBookingSystem.Services;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace ClinicBookingSystem.Controllers
{
    [Route("/api/patients")]
    [ApiController]
    
    public class PatientController : ControllerBase
    {
        private readonly SupabaseService _supabase;
        private readonly EmailService _emailService;

        public PatientController(SupabaseService supabase, EmailService emailService) {
            _supabase = supabase;
            _emailService = emailService;
        }

        [Authorize(Roles = "patient")]
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] PatientRegisterDto model) {
            // 1. get email from JWT token
            var email = User.FindFirst(ClaimTypes.Name)?.Value;

            // 2. check if patient exists
            var result = await _supabase.Client.From<User>().Filter("email", Supabase.Postgrest.Constants.Operator.Equals, email).Get();
            var user = result.Models.FirstOrDefault();
            if(user == null) {
                return NotFound(new {message = $"Patient with email {email} not found"});
            }

            var patient = new Patient {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                Gender = model.Gender,
                Dob = model.Dob
            };

            var inserted = await _supabase.Client.From<Patient>().Insert(patient);
            await _emailService.SendEmailAsync(
                user.Email,
                "Patient Registration Successful",
                $"Hi Mr/Ms.Mrs. {user.FullName}, your profile has been registered successfully. Let's start booking!"
            );
            return Ok(new {
                message = "Patient registered successfully",
                Patient_Id = patient.Id,
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
                return NotFound(new {message = $"Patient with email {email} not found"});
            }

            return Ok(new {
                email,
                user.FullName,
                user.Role
            });
        }
    }
}