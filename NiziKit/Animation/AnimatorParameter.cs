namespace NiziKit.Animation;

public enum AnimatorParameterType
{
    Float,
    Int,
    Bool,
    Trigger
}

public class AnimatorParameter(string name, AnimatorParameterType type)
{
    public string Name { get; } = name;
    public AnimatorParameterType Type { get; } = type;

    private float _floatValue;
    private int _intValue;
    private bool _boolValue;

    public float FloatValue
    {
        get => _floatValue;
        set => _floatValue = value;
    }

    public int IntValue
    {
        get => _intValue;
        set => _intValue = value;
    }

    public bool BoolValue
    {
        get => _boolValue;
        set => _boolValue = value;
    }

    public void SetTrigger() => _boolValue = true;
    public void ResetTrigger() => _boolValue = false;

    public AnimatorParameter Clone()
    {
        var clone = new AnimatorParameter(Name, Type)
        {
            _floatValue = _floatValue,
            _intValue = _intValue,
            _boolValue = _boolValue
        };
        return clone;
    }
}
