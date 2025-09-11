using App.Shared;

namespace App.Worker
{
    public class Program
    {
        public static void Main(string[] args)
        {
#if DEBUG
            // Debug modunda 2 saniye bekle
            System.Threading.Thread.Sleep(2000);
#endif
            var builder = Host.CreateApplicationBuilder(args);

            LoggerConfig.ConfigureLogger("App.Worker");

            builder.Services.AddHostedService<RabbitMQWorker>();

            var host = builder.Build();
            host.Run();
        }
    }
}