namespace NiziKit.Animation;

public class AnimatorTransition
{
    public AnimatorState? DestinationState { get; set; }
    public List<AnimatorCondition> Conditions { get; } = [];
    public float Duration { get; set; } = 0.25f;
    public bool HasExitTime { get; set; }
    public float ExitTime { get; set; } = 1.0f;
    public float Offset { get; set; }

    public AnimatorTransition AddCondition(string parameterName, AnimatorConditionMode mode, float threshold = 0)
    {
        Conditions.Add(new AnimatorCondition
        {
            ParameterName = parameterName,
            Mode = mode,
            Threshold = threshold
        });
        return this;
    }

    public bool CanTransition(Dictionary<string, AnimatorParameter> parameters, float normalizedTime)
    {
        if (HasExitTime && normalizedTime < ExitTime)
        {
            return false;
        }

        foreach (var condition in Conditions)
        {
            if (!condition.Evaluate(parameters))
            {
                return false;
            }
        }

        return true;
    }
}
