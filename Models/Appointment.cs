using System;
using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace ClinicBookingSystem.Models
{
    [Table("appointments")]
    public class Appointment : BaseModel
    {
        [PrimaryKey("id", false)]
        [Column("id")]
        public Guid Id { get; set; }

        [Column("patient_id")]
        public Guid PatientId { get; set; }

        [Column("doctor_id")]
        public Guid DoctorId { get; set; }

        [Column("slot_id")]
        public Guid SlotId { get; set; }

        [Column("status")]
        public string Status { get; set; } = "scheduled";

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
