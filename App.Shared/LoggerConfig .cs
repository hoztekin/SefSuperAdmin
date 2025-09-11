using Serilog;

namespace App.Shared
{
    public static class LoggerConfig
    {
        public static void ConfigureLogger(string applicationName)
        {
            Log.Logger = new LoggerConfiguration()
                .Enrich.FromLogContext()
                .Enrich.WithProperty("Application", applicationName)
                .WriteTo.Console()
                .WriteTo.Seq(serverUrl: "http://seq:5341", apiKey: Environment.GetEnvironmentVariable("SEQ_API_KEY"))
                //.WriteTo.File("serilogs/app-log.txt", // Dosyaya yazdırma
                //             rollingInterval: RollingInterval.Day, // Günlük dosya oluşturma
                //             outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff} [{Level:u3}] {Message:lj}{NewLine}{Exception}")
                .CreateLogger();

            Log.Information("Application {ApplicationName} starting up", applicationName);
        }

        public static void CloseAndFlush()
        {
            Log.Information("Application shutting down");
            Log.CloseAndFlush();
        }
    }
}
