using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;

namespace NiziKit.Core;

public static class Log
{
    private static ILoggerFactory? _factory;

    public static ILoggerFactory Factory
    {
        get => _factory ?? throw new InvalidOperationException("Log.Initialize() must be called before accessing loggers");
        set => _factory = value;
    }

    public static void Initialize(Action<ILoggingBuilder>? configure = null)
    {
        _factory = LoggerFactory.Create(builder =>
        {
            builder.AddConsole(options =>
            {
                options.FormatterName = ConsoleFormatterNames.Simple;
            });
            builder.AddSimpleConsole(options =>
            {
                options.SingleLine = true;
                options.TimestampFormat = "[HH:mm:ss] ";
            });
            builder.SetMinimumLevel(LogLevel.Debug);
            configure?.Invoke(builder);
        });
    }

    public static ILogger<T> Get<T>() => Factory.CreateLogger<T>();

    public static ILogger Get(string categoryName) => Factory.CreateLogger(categoryName);

    public static ILogger Get(Type type) => Factory.CreateLogger(type);
}
