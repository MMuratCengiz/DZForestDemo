using DenOfIz;
using ECS;

namespace Application;

public sealed class AppBuilder
{
    private readonly ApplicationOptions _options = new();
    private readonly List<Func<App, ISystem>> _systemFactories = new();

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

    public AppBuilder AddSystem(ISystem system)
    {
        _systemFactories.Add(_ => system);
        return this;
    }

    public AppBuilder AddSystem(Func<App, ISystem> factory)
    {
        _systemFactories.Add(factory);
        return this;
    }

    public App Build()
    {
        var app = new App(_options);

        foreach (var factory in _systemFactories)
        {
            app.AddSystem(factory(app));
        }

        return app;
    }

    public void Run()
    {
        using var app = Build();
        app.Run();
    }
}
