using Avalonia.Input;
using DenOfIz;
using AvaloniaKey = Avalonia.Input.Key;
using AvaloniaMouseButton = Avalonia.Input.MouseButton;
using DenOfIzMouseButton = DenOfIz.MouseButton;

namespace NiziKit.Skia.Avalonia;

internal static class DenOfIzKeyMapper
{
    public static AvaloniaKey ToAvaloniaKey(KeyCode keyCode)
    {
        return keyCode switch
        {
            KeyCode.Return => AvaloniaKey.Return,
            KeyCode.Escape => AvaloniaKey.Escape,
            KeyCode.Backspace => AvaloniaKey.Back,
            KeyCode.Tab => AvaloniaKey.Tab,
            KeyCode.Space => AvaloniaKey.Space,
            KeyCode.Delete => AvaloniaKey.Delete,
            KeyCode.Insert => AvaloniaKey.Insert,
            KeyCode.Left => AvaloniaKey.Left,
            KeyCode.Right => AvaloniaKey.Right,
            KeyCode.Up => AvaloniaKey.Up,
            KeyCode.Down => AvaloniaKey.Down,
            KeyCode.Home => AvaloniaKey.Home,
            KeyCode.End => AvaloniaKey.End,
            KeyCode.Pageup => AvaloniaKey.PageUp,
            KeyCode.Pagedown => AvaloniaKey.PageDown,
            KeyCode.Capslock => AvaloniaKey.CapsLock,
            KeyCode.Lshift => AvaloniaKey.LeftShift,
            KeyCode.Rshift => AvaloniaKey.RightShift,
            KeyCode.Lctrl => AvaloniaKey.LeftCtrl,
            KeyCode.Rctrl => AvaloniaKey.RightCtrl,
            KeyCode.Lalt => AvaloniaKey.LeftAlt,
            KeyCode.Ralt => AvaloniaKey.RightAlt,
            KeyCode.Lgui => AvaloniaKey.LWin,
            KeyCode.Rgui => AvaloniaKey.RWin,
            KeyCode.A => AvaloniaKey.A,
            KeyCode.B => AvaloniaKey.B,
            KeyCode.C => AvaloniaKey.C,
            KeyCode.D => AvaloniaKey.D,
            KeyCode.E => AvaloniaKey.E,
            KeyCode.F => AvaloniaKey.F,
            KeyCode.G => AvaloniaKey.G,
            KeyCode.H => AvaloniaKey.H,
            KeyCode.I => AvaloniaKey.I,
            KeyCode.J => AvaloniaKey.J,
            KeyCode.K => AvaloniaKey.K,
            KeyCode.L => AvaloniaKey.L,
            KeyCode.M => AvaloniaKey.M,
            KeyCode.N => AvaloniaKey.N,
            KeyCode.O => AvaloniaKey.O,
            KeyCode.P => AvaloniaKey.P,
            KeyCode.Q => AvaloniaKey.Q,
            KeyCode.R => AvaloniaKey.R,
            KeyCode.S => AvaloniaKey.S,
            KeyCode.T => AvaloniaKey.T,
            KeyCode.U => AvaloniaKey.U,
            KeyCode.V => AvaloniaKey.V,
            KeyCode.W => AvaloniaKey.W,
            KeyCode.X => AvaloniaKey.X,
            KeyCode.Y => AvaloniaKey.Y,
            KeyCode.Z => AvaloniaKey.Z,
            KeyCode.Num0 => AvaloniaKey.D0,
            KeyCode.Num1 => AvaloniaKey.D1,
            KeyCode.Num2 => AvaloniaKey.D2,
            KeyCode.Num3 => AvaloniaKey.D3,
            KeyCode.Num4 => AvaloniaKey.D4,
            KeyCode.Num5 => AvaloniaKey.D5,
            KeyCode.Num6 => AvaloniaKey.D6,
            KeyCode.Num7 => AvaloniaKey.D7,
            KeyCode.Num8 => AvaloniaKey.D8,
            KeyCode.Num9 => AvaloniaKey.D9,
            KeyCode.Keypad0 => AvaloniaKey.NumPad0,
            KeyCode.Keypad1 => AvaloniaKey.NumPad1,
            KeyCode.Keypad2 => AvaloniaKey.NumPad2,
            KeyCode.Keypad3 => AvaloniaKey.NumPad3,
            KeyCode.Keypad4 => AvaloniaKey.NumPad4,
            KeyCode.Keypad5 => AvaloniaKey.NumPad5,
            KeyCode.Keypad6 => AvaloniaKey.NumPad6,
            KeyCode.Keypad7 => AvaloniaKey.NumPad7,
            KeyCode.Keypad8 => AvaloniaKey.NumPad8,
            KeyCode.Keypad9 => AvaloniaKey.NumPad9,
            KeyCode.KeypadEnter => AvaloniaKey.Return,
            KeyCode.KeypadPlus => AvaloniaKey.Add,
            KeyCode.KeypadMinus => AvaloniaKey.Subtract,
            KeyCode.KeypadMultiply => AvaloniaKey.Multiply,
            KeyCode.KeypadDivide => AvaloniaKey.Divide,
            KeyCode.F1 => AvaloniaKey.F1,
            KeyCode.F2 => AvaloniaKey.F2,
            KeyCode.F3 => AvaloniaKey.F3,
            KeyCode.F4 => AvaloniaKey.F4,
            KeyCode.F5 => AvaloniaKey.F5,
            KeyCode.F6 => AvaloniaKey.F6,
            KeyCode.F7 => AvaloniaKey.F7,
            KeyCode.F8 => AvaloniaKey.F8,
            KeyCode.F9 => AvaloniaKey.F9,
            KeyCode.F10 => AvaloniaKey.F10,
            KeyCode.F11 => AvaloniaKey.F11,
            KeyCode.F12 => AvaloniaKey.F12,
            KeyCode.Printscreen => AvaloniaKey.PrintScreen,
            KeyCode.Scrolllock => AvaloniaKey.Scroll,
            KeyCode.Pause => AvaloniaKey.Pause,
            KeyCode.Minus => AvaloniaKey.OemMinus,
            KeyCode.Equals => AvaloniaKey.OemPlus,
            KeyCode.Leftbracket => AvaloniaKey.OemOpenBrackets,
            KeyCode.Rightbracket => AvaloniaKey.OemCloseBrackets,
            KeyCode.Backslash => AvaloniaKey.OemBackslash,
            KeyCode.Semicolon => AvaloniaKey.OemSemicolon,
            KeyCode.Quote => AvaloniaKey.OemQuotes,
            KeyCode.Comma => AvaloniaKey.OemComma,
            KeyCode.Period => AvaloniaKey.OemPeriod,
            KeyCode.Slash => AvaloniaKey.OemQuestion,
            KeyCode.Backquote => AvaloniaKey.OemTilde,
            _ => AvaloniaKey.None,
        };
    }

    public static RawInputModifiers ToModifiers(bool shift, bool ctrl, bool alt)
    {
        var modifiers = RawInputModifiers.None;
        if (shift)
        {
            modifiers |= RawInputModifiers.Shift;
        }

        if (ctrl)
        {
            modifiers |= RawInputModifiers.Control;
        }

        if (alt)
        {
            modifiers |= RawInputModifiers.Alt;
        }

        return modifiers;
    }

    public static AvaloniaMouseButton ToAvaloniaMouseButton(DenOfIzMouseButton button)
    {
        return button switch
        {
            DenOfIzMouseButton.Left => AvaloniaMouseButton.Left,
            DenOfIzMouseButton.Right => AvaloniaMouseButton.Right,
            DenOfIzMouseButton.Middle => AvaloniaMouseButton.Middle,
            _ => AvaloniaMouseButton.None,
        };
    }
}
