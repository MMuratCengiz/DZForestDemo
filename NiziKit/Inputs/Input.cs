using System.Numerics;
using System.Runtime.CompilerServices;
using DenOfIz;

namespace NiziKit.Inputs;

public sealed class Input
{
    private static Input? _instance;
    private static Input Instance => _instance ?? throw new InvalidOperationException("Input not initialized");

    private readonly InputContext[] _contexts;
    private Vector2 _lastMousePosition;

    public static InputContext Player1
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => Instance._contexts[0];
    }

    public static InputContext Player2
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => Instance._contexts[1];
    }

    public static InputContext Player3
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => Instance._contexts[2];
    }

    public static InputContext Player4
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => Instance._contexts[3];
    }

    public static InputContext GetPlayer(int index)
    {
        if (index is < 0 or >= 4)
        {
            throw new ArgumentOutOfRangeException(nameof(index), "Player index must be 0-3");
        }

        return Instance._contexts[index];
    }

    public static Vector2 MousePosition
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => Player1.MousePosition;
    }

    public static Vector2 MouseDelta
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => Player1.MouseDelta;
    }

    public static bool GetKey(KeyCode key) => Player1.IsKeyPressed(key);

    public static bool GetKeyDown(KeyCode key) => Player1.IsKeyDown(key);

    public static bool GetKeyUp(KeyCode key) => Player1.IsKeyUp(key);

    public static bool GetMouseButton(MouseButton button) => Player1.IsMouseButtonPressed(button);

    public static bool GetMouseButtonDown(MouseButton button) => Player1.IsMouseButtonDown(button);

    public static bool GetMouseButtonUp(MouseButton button) => Player1.IsMouseButtonUp(button);

    [Obsolete("Use GetKey instead")]
    public static bool IsKeyPressed(KeyCode key) => Player1.IsKeyPressed(key);

    [Obsolete("Use GetMouseButton instead")]
    public static bool IsMouseButtonPressed(MouseButton button) => Player1.IsMouseButtonPressed(button);

    public Input()
    {
        _instance = this;
        _contexts = new InputContext[4];
        for (var i = 0; i < 4; i++)
        {
            _contexts[i] = new InputContext(i);
        }

        _contexts[0].IsEnabled = true;
        _contexts[1].IsEnabled = false;
        _contexts[2].IsEnabled = false;
        _contexts[3].IsEnabled = false;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void Update() => Instance._Update();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void ProcessEvent(ref Event ev) => Instance._ProcessEvent(ref ev);

    private void _Update()
    {
        foreach (var context in _contexts)
        {
            if (context.IsEnabled)
            {
                context.ResetFrameState();
            }
        }
    }

    private void _ProcessEvent(ref Event ev)
    {
        switch (ev.Type)
        {
            case EventType.KeyDown:
                foreach (var context in _contexts)
                {
                    if (context.IsEnabled)
                    {
                        context.OnKeyDown(ev.Key.KeyCode);
                        context.UpdateActions();
                    }
                }

                break;

            case EventType.KeyUp:
                foreach (var context in _contexts)
                {
                    if (context.IsEnabled)
                    {
                        context.OnKeyUp(ev.Key.KeyCode);
                        context.UpdateActions();
                    }
                }

                break;

            case EventType.MouseButtonDown:
                foreach (var context in _contexts)
                {
                    if (context.IsEnabled)
                    {
                        context.OnMouseButtonDown(ev.MouseButton.Button);
                        context.UpdateActions();
                    }
                }

                break;

            case EventType.MouseButtonUp:
                foreach (var context in _contexts)
                {
                    if (context.IsEnabled)
                    {
                        context.OnMouseButtonUp(ev.MouseButton.Button);
                        context.UpdateActions();
                    }
                }

                break;

            case EventType.MouseMotion:
                var currentPos = new Vector2(ev.MouseMotion.X, ev.MouseMotion.Y);
                var delta = currentPos - _lastMousePosition;
                _lastMousePosition = currentPos;

                foreach (var context in _contexts)
                {
                    if (context.IsEnabled)
                    {
                        context.OnMouseMove(ev.MouseMotion.X, ev.MouseMotion.Y, delta.X, delta.Y);
                        context.UpdateActions();
                    }
                }

                break;

            case EventType.MouseWheel:
                foreach (var context in _contexts)
                {
                    if (context.IsEnabled)
                    {
                        context.OnMouseScroll(ev.MouseWheel.Y);
                        context.UpdateActions();
                    }
                }

                break;

            case EventType.ControllerButtonDown:
                foreach (var context in _contexts)
                {
                    if (ShouldContextReceiveControllerInput(context, ev.ControllerButton.JoystickID))
                    {
                        context.OnControllerButtonDown((int)ev.ControllerButton.JoystickID, ev.ControllerButton.Button);
                        context.UpdateActions();
                    }
                }

                break;

            case EventType.ControllerButtonUp:
                foreach (var context in _contexts)
                {
                    if (ShouldContextReceiveControllerInput(context, ev.ControllerButton.JoystickID))
                    {
                        context.OnControllerButtonUp((int)ev.ControllerButton.JoystickID, ev.ControllerButton.Button);
                        context.UpdateActions();
                    }
                }

                break;

            case EventType.ControllerAxisMotion:
                foreach (var context in _contexts)
                {
                    if (ShouldContextReceiveControllerInput(context, ev.ControllerAxis.JoystickID))
                    {
                        // Normalize axis value from int16 range to -1..1
                        var normalizedValue = ev.ControllerAxis.Value / 32767f;
                        context.OnControllerAxis((int)ev.ControllerAxis.JoystickID, ev.ControllerAxis.Axis, normalizedValue);
                        context.UpdateActions();
                    }
                }

                break;
        }
    }

    private bool ShouldContextReceiveControllerInput(InputContext context, uint joystickId)
    {
        if (!context.IsEnabled)
        {
            return false;
        }

        // -1 means accept input from any controller
        return context.AssignedControllerId < 0 || context.AssignedControllerId == (int)joystickId;
    }
}
