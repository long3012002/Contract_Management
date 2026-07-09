using System;
using System.IO;
using Microsoft.Extensions.Logging;

namespace demo1.Logging
{
    public class FileLogger : ILogger
    {
        private readonly string _directoryPath;
        private readonly string _filePrefix;
        private static readonly object _lock = new object();

        public FileLogger(string filePath)
        {
            _directoryPath = Path.GetDirectoryName(filePath) ?? AppDomain.CurrentDomain.BaseDirectory;
            _filePrefix = Path.GetFileNameWithoutExtension(filePath) ?? "debug";
        }

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel)
        {
            // Only write Warning, Error, and Critical logs to file
            return logLevel >= LogLevel.Warning;
        }

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            if (formatter == null) return;

            var message = formatter(state, exception);
            if (string.IsNullOrEmpty(message) && exception == null) return;

            var logLine = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [{logLevel}] {message}";
            if (exception != null)
            {
                logLine += Environment.NewLine + exception.ToString();
            }

            var today = DateTime.Today.ToString("yyyy-MM-dd");
            var currentFilePath = Path.Combine(_directoryPath, $"{_filePrefix}_{today}.log");

            try
            {
                lock (_lock)
                {
                    File.AppendAllText(currentFilePath, logLine + Environment.NewLine);
                }
            }
            catch
            {
                // Fail silently to prevent crashing the application if the file is locked
            }
        }
    }

    public class FileLoggerProvider : ILoggerProvider
    {
        private readonly string _filePath;

        public FileLoggerProvider(string filePath)
        {
            _filePath = filePath;
        }

        public ILogger CreateLogger(string categoryName)
        {
            return new FileLogger(_filePath);
        }

        public void Dispose() { }
    }

    public static class FileLoggerExtensions
    {
        public static ILoggingBuilder AddFile(this ILoggingBuilder builder, string filePath)
        {
            builder.AddProvider(new FileLoggerProvider(filePath));
            return builder;
        }
    }
}
