using NuGet.Common;
using System;
using System.Threading.Tasks;

namespace nugex
{
    partial class Program
    {
        private class myLogger : ILogger
        {
            public void Log(LogLevel level, string data)
            {
                Console.WriteLine(data);
            }

            public void Log(ILogMessage message)
            {
                Log(message.Level, message.Message);
            }

            public async Task LogAsync(LogLevel level, string data)
            {
                await Task.Run(() => Console.WriteLine(data));
            }

            public async Task LogAsync(ILogMessage message)
            {
                await LogAsync(message.Level, message.Message);
            }

            public void LogDebug(string data)
            {
                Log(LogLevel.Debug, data);
            }

            public void LogError(string data)
            {
                Log(LogLevel.Error, data);
            }

            public void LogInformation(string data)
            {
                Log(LogLevel.Information, data);
            }

            public void LogInformationSummary(string data)
            {
                Log(LogLevel.Information, data);
            }

            public void LogMinimal(string data)
            {
                Log(LogLevel.Minimal, data);
            }

            public void LogVerbose(string data)
            {
                Log(LogLevel.Verbose, data);
            }

            public void LogWarning(string data)
            {
                Log(LogLevel.Warning, data);
            }
        }

    }
}
