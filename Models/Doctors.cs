using System;
using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace ClinicBookingSystem.Models
{
    [Table("doctors")]
    public class Doctor : BaseModel
    {
        [PrimaryKey("id", false)]
        [Column("id")]
        public Guid Id { get; set; }

        [Column("user_id")]
        public Guid UserId { get; set; }

        [Column("specialization_id")]
        public Guid SpecializationId { get; set; }

        [Column("bio")]
        public string? Bio { get; set; }
    }
}