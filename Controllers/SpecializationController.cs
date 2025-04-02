using Microsoft.AspNetCore.Mvc;
using ClinicBookingSystem.Models;
using ClinicBookingSystem.Services;

namespace ClinicBookingSystem.Controllers
{
    [Route("/api/specializations")]
    [ApiController]
    public class SpecializationController : ControllerBase
    {
        private readonly SupabaseService _supabase;

        public SpecializationController(SupabaseService supabase)
        {
            _supabase = supabase;
        }

        [HttpGet]
        public async Task<IActionResult> GetAllSpecializations()
        {
            var result = await _supabase.Client.From<Specialization>().Get();
            var specializations = result.Models.Select(spec => new
            {
                spec.Id,
                spec.Name
            });

            return Ok(specializations);
        }
    }
}
