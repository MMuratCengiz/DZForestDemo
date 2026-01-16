using System.Collections.Concurrent;

namespace NiziKit.Graphics.Binding;

public sealed class GpuBinding : IDisposable
{
    private static GpuBinding? _instance;
    public static GpuBinding Instance => _instance ?? throw new InvalidOperationException("GpuBinding not initialized");

    private readonly ConcurrentDictionary<Type, ConcurrentDictionary<object, IShaderBinding>> _bindings = new();

    public GpuBinding()
    {
        _instance = this;
    }

    public static TBinding Get<TBinding>(object target) where TBinding : IShaderBinding, new()
    {
        return Instance._Get<TBinding>(target);
    }

    private TBinding _Get<TBinding>(object target) where TBinding : IShaderBinding, new()
    {
        var bindingType = typeof(TBinding);
        var targetBindings = _bindings.GetOrAdd(bindingType, _ => new ConcurrentDictionary<object, IShaderBinding>());

        if (targetBindings.TryGetValue(target, out var existing))
        {
            return (TBinding)existing;
        }

        var binding = new TBinding();
        return (TBinding)targetBindings.GetOrAdd(target, binding);
    }

    public static void Remove<TBinding>(object target) where TBinding : IShaderBinding
    {
        Instance._Remove<TBinding>(target);
    }

    private void _Remove<TBinding>(object target) where TBinding : IShaderBinding
    {
        var bindingType = typeof(TBinding);
        if (!_bindings.TryGetValue(bindingType, out var targetBindings))
        {
            return;
        }

        if (targetBindings.TryRemove(target, out var binding))
        {
            binding.Dispose();
        }
    }

    public void Dispose()
    {
        foreach (var targetBindings in _bindings.Values)
        {
            foreach (var binding in targetBindings.Values)
            {
                binding.Dispose();
            }

            targetBindings.Clear();
        }

        _bindings.Clear();
        _instance = null;
    }
}
