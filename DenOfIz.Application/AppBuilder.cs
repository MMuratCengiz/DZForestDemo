using DenOfIz;
using ECS;

namespace Application;

public sealed class AppBuilder
{
    private readonly ApplicationOptions _options = new();
    private readonly List<Action<App>> _plugins = new();
    private readonly List<Action<App>> _systemRegistrations = new();

    public static AppBuilder Create() => new();

    public AppBuilder WithTitle(string title)
    {
        _options.Title = title;
        return this;
    }

    public AppBuilder WithSize(uint width, uint height)
    {
        _options.Width = width;
        _options.Height = height;
        return this;
    }

    public AppBuilder WithNumFrames(uint numFrames)
    {
        _options.NumFrames = numFrames;
        return this;
    }

    public AppBuilder WithFixedUpdateRate(double hz)
    {
        _options.FixedUpdateRate = hz;
        return this;
    }

    public AppBuilder WithBackBufferFormat(Format format)
    {
        _options.BackBufferFormat = format;
        return this;
    }

    public AppBuilder WithDepthBufferFormat(Format format)
    {
        _options.DepthBufferFormat = format;
        return this;
    }

    public AppBuilder WithTearing(bool allow)
    {
        _options.AllowTearing = allow;
        return this;
    }

    public AppBuilder WithApiPreference(APIPreference preference)
    {
        _options.ApiPreference = preference;
        return this;
    }

    public AppBuilder AddPlugin(Action<App> plugin)
    {
        _plugins.Add(plugin);
        return this;
    }

    public AppBuilder AddSystem(ISystem system, Schedule schedule)
    {
        _systemRegistrations.Add(app => app.AddSystem(system, schedule));
        return this;
    }

    public AppBuilder AddSystem(Func<App, ISystem> factory, Schedule schedule)
    {
        _systemRegistrations.Add(app => app.AddSystem(factory(app), schedule));
        return this;
    }

    public App Build()
    {
        var app = new App(_options);

        foreach (var plugin in _plugins)
        {
            plugin(app);
        }

        foreach (var registration in _systemRegistrations)
        {
            registration(app);
        }

        return app;
    }

    public void Run()
    {
        using var app = Build();
        app.Run();
    }
}
