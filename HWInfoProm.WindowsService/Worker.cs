namespace HWInfoProm.WindowsService
{

    public class Worker(ILogger<Worker> _logger) : BackgroundService, IDisposable
    {
        private readonly int port = 1234;
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            
            try
            {

                PromServer.Start(port);

                if (_logger.IsEnabled(LogLevel.Information))
                {
                    _logger.LogInformation("Worker starting at: {time}", DateTimeOffset.Now);
                    _logger.LogInformation("Metrics are available under http://127.0.0.1:{port}/metrics", port);
                }

                while (!stoppingToken.IsCancellationRequested)
                {
                    PromServer.Update();
                    await Task.Delay(1000, stoppingToken);
                }
            }
            catch (OperationCanceledException)
            {

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "{Message}", ex.Message);
                Environment.Exit(1);
            }
        }

        public override void Dispose()
        {
            PromServer.Stop();
        }
    }
}
