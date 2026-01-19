using System.Numerics;
using DenOfIz;

namespace NiziKit.Inputs;

public class InputContext
{
    private readonly Dictionary<string, InputAction> _actions = new();
    private readonly HashSet<KeyCode> _pressedKeys = [];
    private readonly HashSet<KeyCode> _keysDownThisFrame = [];
    private readonly HashSet<KeyCode> _keysUpThisFrame = [];
    private readonly HashSet<MouseButton> _pressedMouseButtons = [];
    private readonly HashSet<MouseButton> _mouseButtonsDownThisFrame = [];
    private readonly HashSet<MouseButton> _mouseButtonsUpThisFrame = [];
    private readonly Dictionary<int, HashSet<ControllerButton>> _pressedControllerButtons = new();
    private readonly Dictionary<int, Dictionary<ControllerAxis, float>> _controllerAxisValues = new();

    public int PlayerId { get; }
    public bool IsEnabled { get; set; } = true;
    public int AssignedControllerId { get; set; } = -1;

    public Vector2 MousePosition { get; internal set; }
    public Vector2 MouseDelta { get; internal set; }
    public float MouseScrollDelta { get; internal set; }

    internal InputContext(int playerId)
    {
        PlayerId = playerId;
    }

    public InputAction CreateAction(string name, InputActionType type = InputActionType.Button)
    {
        if (_actions.TryGetValue(name, out var existing))
        {
            return existing;
        }

        var action = new InputAction(this, name, type);
        _actions[name] = action;
        return action;
    }

    public InputAction? GetAction(string name)
    {
        return _actions.GetValueOrDefault(name);
    }

    public bool IsKeyPressed(KeyCode key) => _pressedKeys.Contains(key);

    public bool IsKeyDown(KeyCode key) => _keysDownThisFrame.Contains(key);

    public bool IsKeyUp(KeyCode key) => _keysUpThisFrame.Contains(key);

    public bool IsMouseButtonPressed(MouseButton button) => _pressedMouseButtons.Contains(button);

    public bool IsMouseButtonDown(MouseButton button) => _mouseButtonsDownThisFrame.Contains(button);

    public bool IsMouseButtonUp(MouseButton button) => _mouseButtonsUpThisFrame.Contains(button);

    public bool IsControllerButtonPressed(ControllerButton button, int controllerId = -1)
    {
        if (controllerId < 0)
        {
            foreach (var (_, buttons) in _pressedControllerButtons)
            {
                if (buttons.Contains(button))
                {
                    return true;
                }
            }

            return false;
        }

        return _pressedControllerButtons.TryGetValue(controllerId, out var buttons2) &&
               buttons2.Contains(button);
    }

    public float GetAxisValue(ControllerAxis axis, int controllerId = -1)
    {
        if (controllerId < 0)
        {
            foreach (var (_, axes) in _controllerAxisValues)
            {
                if (axes.TryGetValue(axis, out var value) && MathF.Abs(value) > 0.001f)
                {
                    return value;
                }
            }

            return 0f;
        }

        if (_controllerAxisValues.TryGetValue(controllerId, out var controllerAxes) &&
            controllerAxes.TryGetValue(axis, out var val))
        {
            return val;
        }

        return 0f;
    }

    internal (bool pressed, float value) GetSourceState(InputBindingSource source)
    {
        switch (source.SourceType)
        {
            case InputSourceType.Keyboard:
                var keyPressed = _pressedKeys.Contains(source.KeyCode);
                return (keyPressed, keyPressed ? 1f : 0f);

            case InputSourceType.MouseButton:
                var mousePressed = _pressedMouseButtons.Contains(source.MouseButton);
                return (mousePressed, mousePressed ? 1f : 0f);

            case InputSourceType.MouseAxis:
                return source.MouseAxis switch
                {
                    0 => (MathF.Abs(MouseDelta.X) > 0.001f, MouseDelta.X),
                    1 => (MathF.Abs(MouseDelta.Y) > 0.001f, MouseDelta.Y),
                    2 => (MathF.Abs(MouseScrollDelta) > 0.001f, MouseScrollDelta),
                    _ => (false, 0f)
                };

            case InputSourceType.ControllerButton:
                var ctrlPressed = IsControllerButtonPressed(source.ControllerButton,
                    source.ControllerId >= 0 ? source.ControllerId : AssignedControllerId);
                return (ctrlPressed, ctrlPressed ? 1f : 0f);

            case InputSourceType.ControllerAxis:
                var axisValue = GetAxisValue(source.ControllerAxis,
                    source.ControllerId >= 0 ? source.ControllerId : AssignedControllerId);
                return (MathF.Abs(axisValue) > 0.001f, axisValue);

            default:
                return (false, 0f);
        }
    }

    internal void ResetFrameState()
    {
        _keysDownThisFrame.Clear();
        _keysUpThisFrame.Clear();
        _mouseButtonsDownThisFrame.Clear();
        _mouseButtonsUpThisFrame.Clear();
        MouseDelta = Vector2.Zero;
        MouseScrollDelta = 0f;

        foreach (var action in _actions.Values)
        {
            action.ResetFrameState();
        }
    }

    internal void UpdateActions()
    {
        foreach (var action in _actions.Values)
        {
            action.UpdateState();
        }
    }

    internal void OnKeyDown(KeyCode key)
    {
        if (_pressedKeys.Add(key))
        {
            _keysDownThisFrame.Add(key);
        }
    }

    internal void OnKeyUp(KeyCode key)
    {
        if (_pressedKeys.Remove(key))
        {
            _keysUpThisFrame.Add(key);
        }
    }

    internal void OnMouseButtonDown(MouseButton button)
    {
        if (_pressedMouseButtons.Add(button))
        {
            _mouseButtonsDownThisFrame.Add(button);
        }
    }

    internal void OnMouseButtonUp(MouseButton button)
    {
        if (_pressedMouseButtons.Remove(button))
        {
            _mouseButtonsUpThisFrame.Add(button);
        }
    }

    internal void OnMouseMove(float x, float y, float deltaX, float deltaY)
    {
        MousePosition = new Vector2(x, y);
        MouseDelta = new Vector2(deltaX, deltaY);
    }

    internal void OnMouseScroll(float delta)
    {
        MouseScrollDelta = delta;
    }

    internal void OnControllerButtonDown(int controllerId, ControllerButton button)
    {
        if (!_pressedControllerButtons.TryGetValue(controllerId, out var buttons))
        {
            buttons = [];
            _pressedControllerButtons[controllerId] = buttons;
        }

        buttons.Add(button);
    }

    internal void OnControllerButtonUp(int controllerId, ControllerButton button)
    {
        if (_pressedControllerButtons.TryGetValue(controllerId, out var buttons))
        {
            buttons.Remove(button);
        }
    }

    internal void OnControllerAxis(int controllerId, ControllerAxis axis, float value)
    {
        if (!_controllerAxisValues.TryGetValue(controllerId, out var axes))
        {
            axes = new Dictionary<ControllerAxis, float>();
            _controllerAxisValues[controllerId] = axes;
        }

        axes[axis] = value;
    }
}
