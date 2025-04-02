using System;
using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace ClinicBookingSystem.Models
{
    [Table("specializations")]
    public class Specialization : BaseModel
    {
        [PrimaryKey("id", false)]
        [Column("id")]
        public Guid Id { get; set; }

        [Column("name")]
        public string Name { get; set; }
    }
}
