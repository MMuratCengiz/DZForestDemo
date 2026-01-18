using DenOfIz;

namespace NiziKit.Inputs;

public readonly struct InputBindingSource
{
    public InputSourceType SourceType { get; }
    public KeyCode KeyCode { get; }
    public MouseButton MouseButton { get; }
    public int MouseAxis { get; }
    public ControllerButton ControllerButton { get; }
    public ControllerAxis ControllerAxis { get; }
    public int ControllerId { get; }

    private InputBindingSource(
        InputSourceType sourceType,
        KeyCode keyCode = default,
        MouseButton mouseButton = default,
        int mouseAxis = 0,
        ControllerButton controllerButton = default,
        ControllerAxis controllerAxis = default,
        int controllerId = -1)
    {
        SourceType = sourceType;
        KeyCode = keyCode;
        MouseButton = mouseButton;
        MouseAxis = mouseAxis;
        ControllerButton = controllerButton;
        ControllerAxis = controllerAxis;
        ControllerId = controllerId;
    }

    public static InputBindingSource FromKey(KeyCode keyCode)
        => new(InputSourceType.Keyboard, keyCode: keyCode);

    public static InputBindingSource FromMouseButton(MouseButton button)
        => new(InputSourceType.MouseButton, mouseButton: button);

    public static InputBindingSource FromMouseAxis(int axis)
        => new(InputSourceType.MouseAxis, mouseAxis: axis);

    public static InputBindingSource FromControllerButton(ControllerButton button, int controllerId = -1)
        => new(InputSourceType.ControllerButton, controllerButton: button, controllerId: controllerId);

    public static InputBindingSource FromControllerAxis(ControllerAxis axis, int controllerId = -1)
        => new(InputSourceType.ControllerAxis, controllerAxis: axis, controllerId: controllerId);
}
