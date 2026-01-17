namespace NiziKit.Animation;

public enum AnimatorConditionMode
{
    Greater,
    Less,
    GreaterOrEqual,
    LessOrEqual,
    Equals,
    NotEquals,
    If,
    IfNot
}

public class AnimatorCondition
{
    private const float FloatEpsilon = 1e-6f;

    public string ParameterName { get; set; } = string.Empty;
    public AnimatorConditionMode Mode { get; set; }
    public float Threshold { get; set; }

    public bool Evaluate(Dictionary<string, AnimatorParameter> parameters)
    {
        if (!parameters.TryGetValue(ParameterName, out var param))
        {
            return false;
        }

        return param.Type switch
        {
            AnimatorParameterType.Float => EvaluateFloat(param.FloatValue),
            AnimatorParameterType.Int => EvaluateInt(param.IntValue),
            AnimatorParameterType.Bool => EvaluateBool(param.BoolValue),
            AnimatorParameterType.Trigger => EvaluateBool(param.BoolValue),
            _ => false
        };
    }

    private bool EvaluateFloat(float value)
    {
        return Mode switch
        {
            AnimatorConditionMode.Greater => value > Threshold + FloatEpsilon,
            AnimatorConditionMode.Less => value < Threshold - FloatEpsilon,
            AnimatorConditionMode.GreaterOrEqual => value >= Threshold - FloatEpsilon,
            AnimatorConditionMode.LessOrEqual => value <= Threshold + FloatEpsilon,
            AnimatorConditionMode.Equals => Math.Abs(value - Threshold) < FloatEpsilon,
            AnimatorConditionMode.NotEquals => Math.Abs(value - Threshold) >= FloatEpsilon,
            _ => false
        };
    }

    private bool EvaluateInt(int value)
    {
        var threshold = (int)Threshold;
        return Mode switch
        {
            AnimatorConditionMode.Greater => value > threshold,
            AnimatorConditionMode.Less => value < threshold,
            AnimatorConditionMode.GreaterOrEqual => value >= threshold,
            AnimatorConditionMode.LessOrEqual => value <= threshold,
            AnimatorConditionMode.Equals => value == threshold,
            AnimatorConditionMode.NotEquals => value != threshold,
            _ => false
        };
    }

    private bool EvaluateBool(bool value)
    {
        return Mode switch
        {
            AnimatorConditionMode.If => value,
            AnimatorConditionMode.IfNot => !value,
            _ => false
        };
    }
}
