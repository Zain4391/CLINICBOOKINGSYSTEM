using Microsoft.AspNetCore.Mvc;
using ClinicBookingSystem.Models;
using ClinicBookingSystem.DTOs;
using ClinicBookingSystem.Services;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace ClinicBookingSystem.Controllers
{
    [Route("/api/appointments")]
    [ApiController]
    public class AppointmentController : ControllerBase
    {
        private readonly SupabaseService _supabase;
        private readonly EmailService _emailService;

        public AppointmentController(SupabaseService supabase, EmailService emailService) {
            _supabase = supabase;
            _emailService = emailService;
        }

        [Authorize(Roles = "patient")]
        [HttpPost("book")]
        public async Task<IActionResult> BookAppointment([FromBody] AppointmentBookingDto model)
        {
            var email = User.FindFirst(ClaimTypes.Name)?.Value;

            // Get the user
            var userResult = await _supabase.Client.From<User>()
                .Filter("email", Supabase.Postgrest.Constants.Operator.Equals, email).Get();
            var user = userResult.Models.FirstOrDefault();
            if (user == null)
                return Unauthorized(new { message = "Invalid user" });

            // Get the patient
            var patientResult = await _supabase.Client.From<Patient>()
                .Filter("user_id", Supabase.Postgrest.Constants.Operator.Equals, user.Id.ToString()).Get();
            var patient = patientResult.Models.FirstOrDefault();
            if (patient == null)
                return NotFound(new { message = "Patient profile not found" });

            // Get the slot
            var slotResult = await _supabase.Client.From<AvailabilitySlot>()
                .Filter("id", Supabase.Postgrest.Constants.Operator.Equals, model.SlotId.ToString()).Get();
            var slot = slotResult.Models.FirstOrDefault();
            if (slot == null)
                return NotFound(new { message = "Slot not found" });
            if (slot.IsBooked)
                return BadRequest(new { message = "Slot already booked" });

            // Get the doctor
            var docResult = await _supabase.Client.From<Doctor>()
                .Filter("id", Supabase.Postgrest.Constants.Operator.Equals, slot.DoctorId.ToString()).Get();
            var doctor = docResult.Models.FirstOrDefault();
            if (doctor == null)
                return NotFound(new { message = "Doctor not found" });
            User doctorUser = null;
            if (doctor != null)
            {
                var docUserResult = await _supabase.Client.From<User>()
                    .Filter("id", Supabase.Postgrest.Constants.Operator.Equals, doctor.UserId.ToString())
                    .Get();
                doctorUser = docUserResult.Models.FirstOrDefault();
            }

            // Create appointment
            var appointment = new Appointment
            {
                Id = Guid.NewGuid(),
                DoctorId = doctor.Id,
                PatientId = patient.Id,
                SlotId = slot.Id,
                Status = "confirmed",
                CreatedAt = DateTime.UtcNow
            };

            await _supabase.Client.From<Appointment>().Insert(appointment);

            // Mark slot as booked
            slot.IsBooked = true;
            await _supabase.Client.From<AvailabilitySlot>().Update(slot);
            await _emailService.SendEmailAsync(
            toEmail: user.Email,
            subject: "Appointment Confirmed - Clinic Booking System",
            body: $@"
                <h3>Appointment Confirmed!</h3>
                <p>Hi {user.FullName},</p>
                <p>Your appointment with <strong>Dr. {doctorUser?.FullName}</strong> has been successfully booked.</p>
                <ul>
                    <li><strong>Date:</strong> {slot.StartTime.ToString("dddd, MMMM dd yyyy")}</li>
                    <li><strong>Time:</strong> {slot.StartTime.ToShortTimeString()} - {slot.EndTime.ToShortTimeString()}</li>
                    <li><strong>Fee:</strong> $50</li>
                </ul>
                <p>Thank you for choosing our clinic.</p>
            "
            );

            return Ok(new
            {
                message = "Appointment booked successfully",
                appointmentId = appointment.Id,
                doctor = user.FullName,
                slot = new { slot.StartTime, slot.EndTime }
            });
        }

            [Authorize(Roles = "patient")]
            [HttpGet("my")]
            public async Task<IActionResult> GetMyAppointments()
            {
                var email = User.FindFirst(ClaimTypes.Name)?.Value;

                // Get user
                var userResult = await _supabase.Client.From<User>()
                    .Filter("email", Supabase.Postgrest.Constants.Operator.Equals, email)
                    .Get();
                var user = userResult.Models.FirstOrDefault();
                if (user == null)
                    return Unauthorized(new { message = "Invalid user" });

                // Get patient
                var patientResult = await _supabase.Client.From<Patient>()
                    .Filter("user_id", Supabase.Postgrest.Constants.Operator.Equals, user.Id.ToString())
                    .Get();
                var patient = patientResult.Models.FirstOrDefault();
                if (patient == null)
                    return NotFound(new { message = "Patient profile not found" });

                // Get appointments for this patient
                var appointmentsResult = await _supabase.Client.From<Appointment>()
                    .Filter("patient_id", Supabase.Postgrest.Constants.Operator.Equals, patient.Id.ToString())
                    .Order("created_at", Supabase.Postgrest.Constants.Ordering.Descending)
                    .Get();

                var enrichedAppointments = new List<object>();

                foreach (var appt in appointmentsResult.Models)
                {
                    // Fetch doctor
                    var docResult = await _supabase.Client.From<Doctor>()
                        .Filter("id", Supabase.Postgrest.Constants.Operator.Equals, appt.DoctorId.ToString())
                        .Get();
                    var doctor = docResult.Models.FirstOrDefault();

                    // Fetch doctor user to get name
                    User doctorUser = null;
                    if (doctor != null)
                    {
                        var docUserResult = await _supabase.Client.From<User>()
                            .Filter("id", Supabase.Postgrest.Constants.Operator.Equals, doctor.UserId.ToString())
                            .Get();
                        doctorUser = docUserResult.Models.FirstOrDefault();
                    }

                    // Fetch slot
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
                        SlotStart = slot?.StartTime,
                        SlotEnd = slot?.EndTime
                    });
                }

                return Ok(enrichedAppointments);
            }

    }
}