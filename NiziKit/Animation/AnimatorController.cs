namespace NiziKit.Animation;

public class AnimatorController
{
    public string Name { get; set; } = string.Empty;
    public List<AnimatorLayer> Layers { get; } = [];
    public Dictionary<string, AnimatorParameter> Parameters { get; } = [];

    public AnimatorController()
    {
        Layers.Add(new AnimatorLayer());
    }

    public AnimatorLayer BaseLayer => Layers[0];

    public AnimatorParameter AddFloat(string name, float defaultValue = 0)
    {
        var param = new AnimatorParameter(name, AnimatorParameterType.Float)
        {
            FloatValue = defaultValue
        };
        Parameters[name] = param;
        return param;
    }

    public AnimatorParameter AddInt(string name, int defaultValue = 0)
    {
        var param = new AnimatorParameter(name, AnimatorParameterType.Int)
        {
            IntValue = defaultValue
        };
        Parameters[name] = param;
        return param;
    }

    public AnimatorParameter AddBool(string name, bool defaultValue = false)
    {
        var param = new AnimatorParameter(name, AnimatorParameterType.Bool)
        {
            BoolValue = defaultValue
        };
        Parameters[name] = param;
        return param;
    }

    public AnimatorParameter AddTrigger(string name)
    {
        var param = new AnimatorParameter(name, AnimatorParameterType.Trigger);
        Parameters[name] = param;
        return param;
    }

    public AnimatorState AddState(string name, int layerIndex = 0)
    {
        while (Layers.Count <= layerIndex)
        {
            Layers.Add(new AnimatorLayer { Name = $"Layer {Layers.Count}" });
        }

        return Layers[layerIndex].AddState(name);
    }

    public AnimatorLayer AddLayer(string name)
    {
        var layer = new AnimatorLayer { Name = name };
        Layers.Add(layer);
        return layer;
    }

    public Dictionary<string, AnimatorParameter> CloneParameters()
    {
        var clone = new Dictionary<string, AnimatorParameter>();
        foreach (var (name, param) in Parameters)
        {
            clone[name] = param.Clone();
        }
        return clone;
    }
}
