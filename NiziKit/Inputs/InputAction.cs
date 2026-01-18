using System.Numerics;

namespace NiziKit.Inputs;

public class InputAction
{
    private readonly List<InputBinding> _bindings = new();
    private readonly InputContext _context;

    private bool _wasPressed;
    private float _previousValue;
    private Vector2 _previousVector2Value;

    public string Name { get; }
    public InputActionType Type { get; }
    public IReadOnlyList<InputBinding> Bindings => _bindings;

    public bool IsPressed { get; internal set; }
    public bool WasPressedThisFrame { get; internal set; }
    public bool WasReleasedThisFrame { get; internal set; }
    public float Value { get; internal set; }
    public Vector2 Vector2Value { get; internal set; }

    public event Action<InputAction>? Started;
    public event Action<InputAction>? Performed;
    public event Action<InputAction>? Canceled;

    internal InputAction(InputContext context, string name, InputActionType type = InputActionType.Button)
    {
        _context = context;
        Name = name;
        Type = type;
    }

    public InputAction AddBinding(InputBindingSource source)
    {
        _bindings.Add(InputBinding.Single(source));
        return this;
    }

    public InputAction AddCompositeBinding(InputBindingSource negative, InputBindingSource positive)
    {
        _bindings.Add(InputBinding.Composite1D(negative, positive));
        return this;
    }

    public InputAction AddVector2Composite(
        InputBindingSource up,
        InputBindingSource down,
        InputBindingSource left,
        InputBindingSource right)
    {
        _bindings.Add(InputBinding.Composite2D(up, down, left, right));
        return this;
    }

    internal void ResetFrameState()
    {
        _wasPressed = IsPressed;
        _previousValue = Value;
        _previousVector2Value = Vector2Value;

        WasPressedThisFrame = false;
        WasReleasedThisFrame = false;
    }

    internal void UpdateState()
    {
        var newPressed = false;
        var newValue = 0f;
        var newVector2 = Vector2.Zero;

        foreach (var binding in _bindings)
        {
            switch (binding.BindingType)
            {
                case InputBindingType.Single:
                    if (binding.Source.HasValue)
                    {
                        var (pressed, value) = _context.GetSourceState(binding.Source.Value);
                        if (pressed)
                        {
                            newPressed = true;
                        }

                        if (MathF.Abs(value) > MathF.Abs(newValue))
                        {
                            newValue = value;
                        }
                    }

                    break;

                case InputBindingType.Composite1D:
                    {
                        var negValue = 0f;
                        var posValue = 0f;

                        if (binding.Negative.HasValue)
                        {
                            var (_, val) = _context.GetSourceState(binding.Negative.Value);
                            negValue = val;
                        }

                        if (binding.Positive.HasValue)
                        {
                            var (_, val) = _context.GetSourceState(binding.Positive.Value);
                            posValue = val;
                        }

                        var compositeValue = posValue - negValue;
                        if (MathF.Abs(compositeValue) > MathF.Abs(newValue))
                        {
                            newValue = compositeValue;
                        }

                        if (MathF.Abs(compositeValue) > 0.001f)
                        {
                            newPressed = true;
                        }
                    }
                    break;

                case InputBindingType.Composite2D:
                    {
                        var upValue = 0f;
                        var downValue = 0f;
                        var leftValue = 0f;
                        var rightValue = 0f;

                        if (binding.Up.HasValue)
                        {
                            var (_, val) = _context.GetSourceState(binding.Up.Value);
                            upValue = val;
                        }

                        if (binding.Down.HasValue)
                        {
                            var (_, val) = _context.GetSourceState(binding.Down.Value);
                            downValue = val;
                        }

                        if (binding.Left.HasValue)
                        {
                            var (_, val) = _context.GetSourceState(binding.Left.Value);
                            leftValue = val;
                        }

                        if (binding.Right.HasValue)
                        {
                            var (_, val) = _context.GetSourceState(binding.Right.Value);
                            rightValue = val;
                        }

                        var vec = new Vector2(rightValue - leftValue, upValue - downValue);
                        if (vec.LengthSquared() > newVector2.LengthSquared())
                        {
                            newVector2 = vec;
                        }

                        if (vec.LengthSquared() > 0.001f)
                        {
                            newPressed = true;
                        }
                    }
                    break;
            }
        }

        if (newVector2.LengthSquared() > 1f)
        {
            newVector2 = Vector2.Normalize(newVector2);
        }

        IsPressed = newPressed;
        Value = newValue;
        Vector2Value = newVector2;

        WasPressedThisFrame = IsPressed && !_wasPressed;
        WasReleasedThisFrame = !IsPressed && _wasPressed;
        if (WasPressedThisFrame)
        {
            Started?.Invoke(this);
        }

        var valueChanged = Type switch
        {
            InputActionType.Button => IsPressed != _wasPressed,
            InputActionType.Axis1D => MathF.Abs(Value - _previousValue) > 0.001f,
            InputActionType.Axis2D => Vector2.DistanceSquared(Vector2Value, _previousVector2Value) > 0.000001f,
            _ => false
        };

        if (valueChanged && IsPressed)
        {
            Performed?.Invoke(this);
        }

        if (WasReleasedThisFrame)
        {
            Canceled?.Invoke(this);
        }
    }
}
