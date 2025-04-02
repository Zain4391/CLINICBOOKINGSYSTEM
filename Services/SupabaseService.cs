using Supabase;

namespace ClinicBookingSystem.Services
{
    public class SupabaseService
    {
        public Supabase.Client Client { get; private set; }

        public SupabaseService(IConfiguration config)
        {
            var url = config["Supabase:Url"];
            var key = config["Supabase:Key"];

            var options = new SupabaseOptions
            {
                AutoConnectRealtime = false
            };

            Client = new Supabase.Client(url, key, options);
            Client.InitializeAsync().Wait();
        }
    }
}
