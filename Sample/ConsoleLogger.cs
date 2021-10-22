using System;
using Microsoft.Extensions.Logging;

namespace Sample
{
    public class ConsoleLogger : ILogger, ILoggerProvider, ILoggerFactory
    {
        private static readonly object ThreadLock = new object();
        
        private readonly string _scope;

        public ConsoleLogger()
        {
            _scope = "";
        }
        
        private ConsoleLogger(string scope)
        {
            _scope = scope ?? "";
        }
        
        public void Dispose()
        {
        }

        public ILogger CreateLogger(string categoryName) => new ConsoleLogger(categoryName);
        
        public void AddProvider(ILoggerProvider provider)
        {
            // not used.
        }

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            var msg = formatter(state, exception);
            if (!string.IsNullOrEmpty(_scope)) msg = $"{_scope}: {msg}";
            lock (ThreadLock)
            {
                var fg = Console.ForegroundColor;
                switch (logLevel)
                {
                    case LogLevel.Trace:
                    case LogLevel.Debug:
                    case LogLevel.None:
                        break;
                    case LogLevel.Information:
                        Console.ForegroundColor = ConsoleColor.White;
                        break;
                    case LogLevel.Warning:
                        Console.ForegroundColor = ConsoleColor.Yellow;
                        break;
                    case LogLevel.Error:
                    case LogLevel.Critical:
                        Console.ForegroundColor = ConsoleColor.Red;
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(logLevel), logLevel, null);
                }

                Console.WriteLine(msg);
                Console.ForegroundColor = fg;
            }
        }

        public bool IsEnabled(LogLevel logLevel) => true;

        public IDisposable BeginScope<TState>(TState state)
        {
            var scope = string.IsNullOrEmpty(_scope) ? $"{state}" : $"{_scope} - {state}";
            return new ConsoleLogger(scope);
        }
    }
}
