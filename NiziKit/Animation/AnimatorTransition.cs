namespace NiziKit.Animation;

public enum TransitionCurve
{
    Linear,
    EaseIn,
    EaseOut,
    EaseInOut,
    EaseInQuad,
    EaseOutQuad,
    EaseInOutQuad,
    EaseInCubic,
    EaseOutCubic,
    EaseInOutCubic
}

public class AnimatorTransition
{
    public AnimatorState? DestinationState { get; set; }
    public List<AnimatorCondition> Conditions { get; } = [];
    public float Duration { get; set; } = 0.25f;
    public bool HasExitTime { get; set; }
    public float ExitTime { get; set; } = 1.0f;
    public float Offset { get; set; }
    public TransitionCurve Curve { get; set; } = TransitionCurve.Linear;
    public bool CanInterruptSource { get; set; }
    public int Priority { get; set; }

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

    public float ApplyCurve(float t)
    {
        t = Math.Clamp(t, 0f, 1f);

        return Curve switch
        {
            TransitionCurve.Linear => t,
            TransitionCurve.EaseIn => EaseInSine(t),
            TransitionCurve.EaseOut => EaseOutSine(t),
            TransitionCurve.EaseInOut => EaseInOutSine(t),
            TransitionCurve.EaseInQuad => t * t,
            TransitionCurve.EaseOutQuad => 1f - (1f - t) * (1f - t),
            TransitionCurve.EaseInOutQuad => t < 0.5f ? 2f * t * t : 1f - MathF.Pow(-2f * t + 2f, 2f) / 2f,
            TransitionCurve.EaseInCubic => t * t * t,
            TransitionCurve.EaseOutCubic => 1f - MathF.Pow(1f - t, 3f),
            TransitionCurve.EaseInOutCubic => t < 0.5f ? 4f * t * t * t : 1f - MathF.Pow(-2f * t + 2f, 3f) / 2f,
            _ => t
        };
    }

    private static float EaseInSine(float t) => 1f - MathF.Cos(t * MathF.PI / 2f);
    private static float EaseOutSine(float t) => MathF.Sin(t * MathF.PI / 2f);
    private static float EaseInOutSine(float t) => -(MathF.Cos(MathF.PI * t) - 1f) / 2f;
}
