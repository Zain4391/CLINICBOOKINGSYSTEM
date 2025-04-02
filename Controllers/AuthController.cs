using BCrypt.Net;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using ClinicBookingSystem.Models;
using ClinicBookingSystem.Services;
using ClinicBookingSystem.DTOs;

namespace ClinicBookingSystem.Controllers
{
    [Route("/api/auth")]
    [ApiController]
    public class AuthController: ControllerBase
    {
        private readonly SupabaseService _supabase;
        private readonly IConfiguration _config;

        private readonly EmailService _emailService;

        public AuthController(SupabaseService supabase, IConfiguration config, EmailService emailService) {
            _supabase = supabase;
            _config = config;
            _emailService = emailService;
        }

        private async Task<string>  GenerateJwtToken(string email) {
            var result = await _supabase.Client.From<User>().Filter("email", Supabase.Postgrest.Constants.Operator.Equals, email).Get();
            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"]));
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

            var user = result.Models.FirstOrDefault();
            var claims = new[] 
            {
                new Claim(ClaimTypes.Name, user.Email),
                new Claim(ClaimTypes.Role, user.Role)
            };

            var token = new JwtSecurityToken(
                _config["Jwt:Issuer"],
                _config["Jwt:Issuer"],
                claims,
                expires: DateTime.Now.AddHours(1),
                signingCredentials: credentials
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterDto model) {
            if(string.IsNullOrEmpty(model.Email) || string.IsNullOrEmpty(model.FullName) || string.IsNullOrEmpty(model.Password) || string.IsNullOrEmpty(model.Role)) {
                return BadRequest(new {message = "All fields required"});
            }

            var email_res = await _supabase.Client.From<User>().Filter("email", Supabase.Postgrest.Constants.Operator.Equals, model.Email).Get();
            var email = email_res.Models.FirstOrDefault();
            if(email != null) {
                return BadRequest(new {message= "Email already in use"});
            }

            string hashpass = BCrypt.Net.BCrypt.HashPassword(model.Password);
            
            // create DB entry
            var user = new User {
                Id = Guid.NewGuid(),
                FullName = model.FullName,
                Email = model.Email,
                PasswordHash = hashpass,
                Role = model.Role.ToLower()
            };

            //Create response obj
            var ret_user = new RegisterDto {
                FullName = model.FullName,
                Email = model.Email,
                Role = model.Role.ToLower()
            };

            var result = await _supabase.Client.From<User>().Insert(user);
            return Ok(ret_user);
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDto model) {

            if (string.IsNullOrEmpty(model.Email) || string.IsNullOrEmpty(model.password)) {
                return BadRequest(new {message = "Email and password are required"});
            }
            // find the user in the db
            var result = await _supabase.Client.From<User>().Filter("email", Supabase.Postgrest.Constants.Operator.Equals, model.Email).Get();

            var user = result.Models.FirstOrDefault();

            if(user == null) {
                return NotFound(new {message = $"No user with email: {model.Email} found"});
            }

            if(!BCrypt.Net.BCrypt.Verify(model.password, user.PasswordHash)) {
                return Unauthorized(new {message = "Invalid credentials"});
            }

            var token = await GenerateJwtToken(model.Email);
            return Ok(new {token});
        }

        [HttpPost("forgot-password")]
        public async Task<IActionResult> ForgotPAssword([FromBody] ForgotPasswordDto model) {
            var result = await _supabase.Client.From<User>().Filter("email", Supabase.Postgrest.Constants.Operator.Equals, model.Email).Get();
            var user = result.Models.FirstOrDefault();

            if(user == null) {
                return Ok(new {message = "If email exists, a reset link has been sent!"});
            }

            string resetToken = Guid.NewGuid().ToString();
            user.ResetToken = resetToken;
            user.ResetTokenExpiresAt = DateTime.UtcNow.AddHours(1); // expires in 1 hour

            await _supabase.Client.From<User>().Update(user);
            var resetLink = $"http://localhost:3000/debug-reset?token={resetToken}";

             await _emailService.SendEmailAsync(
                user.Email,
                "Password Reset Link",
                $"Click the link to reset your password:\n\n{resetLink}\n\nThis link will expire in 1 hour."
            );

            return Ok(new { message = "If this email exists, a reset link has been sent." });
        }

        [HttpPost("reset-password")]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordDto model)
        {
            var result = await _supabase.Client
                .From<User>()
                .Filter("reset_token", Supabase.Postgrest.Constants.Operator.Equals, model.Token)
                .Get();

            var user = result.Models.FirstOrDefault();

            if (user == null || user.ResetTokenExpiresAt == null || user.ResetTokenExpiresAt < DateTime.UtcNow)
            {
                return BadRequest(new { message = "Invalid or expired token." });
            }

            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(model.NewPassword);
            user.ResetToken = "";
            user.ResetTokenExpiresAt = null;

            await _supabase.Client.From<User>().Update(user);

            return Ok(new { message = "Password has been reset successfully." });
        }


        [Authorize]
        [HttpGet("me")]
        public async Task<IActionResult> GetcurrentUser() {
            var email = User.FindFirst(ClaimTypes.Name)?.Value;

            var result = await _supabase.Client.From<User>().Filter("email", Supabase.Postgrest.Constants.Operator.Equals, email).Get();
            var user = result.Models.FirstOrDefault();
            if(user == null) {
                return NotFound(new{message = $"No user with email: {email} found"});
            }

            return Ok(new {
                user.Email,
                user.Role
            });
        }
    }
}
