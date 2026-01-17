namespace NiziKit.Animation;

public enum AnimationLoopMode
{
    Once,
    Loop,
    PingPong
}

public class AnimatorState
{
    public string Name { get; set; } = string.Empty;
    public Assets.Animation? Clip { get; set; }
    public float Speed { get; set; } = 1.0f;
    public string? SpeedParameterName { get; set; }
    public AnimationLoopMode LoopMode { get; set; } = AnimationLoopMode.Loop;
    public List<AnimatorTransition> Transitions { get; } = [];

    public bool Loop
    {
        get => LoopMode == AnimationLoopMode.Loop;
        set => LoopMode = value ? AnimationLoopMode.Loop : AnimationLoopMode.Once;
    }

    public AnimatorTransition AddTransition(AnimatorState destination)
    {
        var transition = new AnimatorTransition
        {
            DestinationState = destination
        };
        Transitions.Add(transition);
        return transition;
    }

    public IEnumerable<AnimatorTransition> GetOrderedTransitions()
    {
        return Transitions.OrderByDescending(t => t.Priority);
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
