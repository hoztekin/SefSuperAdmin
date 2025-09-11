using RabbitMQ.Client;
using Serilog;

namespace App.Worker
{
    public class RabbitMQWorker : BackgroundService
    {
        private IConnection _connection;
        private IChannel _channel;
        private bool _initialized = false;

        private async Task InitializeRabbitMQAsync()
        {
            if (_initialized)
                return;

            try
            {
                var factory = new ConnectionFactory()
                {
                    HostName = Environment.GetEnvironmentVariable("RABBITMQ_HOSTNAME") ?? "rabbitmq",
                    UserName = Environment.GetEnvironmentVariable("RABBITMQ_USERNAME") ?? "ProjectUserName",
                    Password = Environment.GetEnvironmentVariable("RABBITMQ_PASSWORD") ?? "ProjectPassword",
                    ClientProvidedName = "my_worker_service"
                };

                Log.Information("RabbitMQ baðlantýsý oluþturuluyor: {HostName}", factory.HostName);
                _connection = await factory.CreateConnectionAsync();

                Log.Information("RabbitMQ kanalý oluþturuluyor");
                _channel = await _connection.CreateChannelAsync();

                // Exchange oluþturma
                await _channel.ExchangeDeclareAsync(
                    exchange: "project_exchange",
                    type: ExchangeType.Direct,
                    durable: true,
                    autoDelete: false);

                Log.Information("RabbitMQ exchange oluþturuldu: project_exchange");
                _initialized = true;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "RabbitMQ baðlantýsý oluþturulurken hata oluþtu.Yeniden denenecek");
                _initialized = false;
                await Task.Delay(5000);
            }
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            if (_channel == null || !_initialized)
                await InitializeRabbitMQAsync();


            while (!stoppingToken.IsCancellationRequested)
            {
                //Çalýþmasý istenen kodlar
                //Log.Information("Worker running at: {time}", DateTimeOffset.Now);
                await Task.Delay(1000000, stoppingToken);
            }
        }

        public override async Task StopAsync(CancellationToken stoppingToken)
        {
            Log.Information("Worker servis durduruluyor");

            if (_channel != null)
                await _channel.CloseAsync();


            if (_connection != null)
                await _connection.CloseAsync();

            await base.StopAsync(stoppingToken);
        }
    }
}
