using NiziKit.Assets;

namespace NiziKit.Animation;

public class AnimatorState
{
    public string Name { get; set; } = string.Empty;
    public Assets.Animation? Clip { get; set; }
    public float Speed { get; set; } = 1.0f;
    public string? SpeedParameterName { get; set; }
    public bool Loop { get; set; } = true;
    public List<AnimatorTransition> Transitions { get; } = [];

    public AnimatorTransition AddTransition(AnimatorState destination)
    {
        var transition = new AnimatorTransition
        {
            DestinationState = destination
        };
        Transitions.Add(transition);
        return transition;
    }

    public float GetSpeed(Dictionary<string, AnimatorParameter> parameters)
    {
        if (string.IsNullOrEmpty(SpeedParameterName))
        {
            return Speed;
        }

        if (parameters.TryGetValue(SpeedParameterName, out var param))
        {
            return Speed * param.FloatValue;
        }

        return Speed;
    }
}
