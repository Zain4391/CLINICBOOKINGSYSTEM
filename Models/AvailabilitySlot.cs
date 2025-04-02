using System;
using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace ClinicBookingSystem.Models
{
    [Table("availability_slots")]
    public class AvailabilitySlot : BaseModel
    {
        [PrimaryKey("id", false)]
        [Column("id")]
        public Guid Id { get; set; }

        [Column("doctor_id")]
        public Guid DoctorId { get; set; }

        [Column("start_time")]
        public DateTime StartTime { get; set; }

        [Column("end_time")]
        public DateTime EndTime { get; set; }
        [Column("is_booked")]
        public bool IsBooked { get; set; }

    }
}
