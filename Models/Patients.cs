using System;
using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace ClinicBookingSystem.Models
{
    [Table("patients")]
    public class Patient : BaseModel
    {
        [PrimaryKey("id", false)]
        [Column("id")]
        public Guid Id { get; set; }

        [Column("user_id")]
        public Guid UserId { get; set; }

        [Column("dob")]
        public DateTime? Dob { get; set; }

        [Column("gender")]
        public string? Gender { get; set; }
    }
}
