namespace NiziKit.Inputs;

public enum InputBindingType
{
    Single,
    Composite1D,
    Composite2D
}

public class InputBinding
{
    public InputBindingType BindingType { get; }

    public InputBindingSource? Source { get; private init; }

    public InputBindingSource? Negative { get; private init; }
    public InputBindingSource? Positive { get; private init; }

    public InputBindingSource? Up { get; private init; }
    public InputBindingSource? Down { get; private init; }
    public InputBindingSource? Left { get; private init; }
    public InputBindingSource? Right { get; private init; }

    private InputBinding(InputBindingType type)
    {
        BindingType = type;
    }

    public static InputBinding Single(InputBindingSource source)
    {
        return new InputBinding(InputBindingType.Single)
        {
            Source = source
        };
    }

    public static InputBinding Composite1D(InputBindingSource negative, InputBindingSource positive)
    {
        return new InputBinding(InputBindingType.Composite1D)
        {
            Negative = negative,
            Positive = positive
        };
    }

    public static InputBinding Composite2D(
        InputBindingSource up,
        InputBindingSource down,
        InputBindingSource left,
        InputBindingSource right)
    {
        return new InputBinding(InputBindingType.Composite2D)
        {
            Up = up,
            Down = down,
            Left = left,
            Right = right
        };
    }
}
