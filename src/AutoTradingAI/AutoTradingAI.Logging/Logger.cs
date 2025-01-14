using Microsoft.Extensions.Configuration;
using Serilog;

namespace AutoTradingAI.Logging
{
    public static class Logger
    {
        public static void ConfigureLogging(IConfiguration configuration)
        {
            Log.Logger = new LoggerConfiguration()
                .ReadFrom.Configuration(configuration)
                .Enrich.FromLogContext()
                .WriteTo.Console()
                .CreateLogger();
        }
    }
}
