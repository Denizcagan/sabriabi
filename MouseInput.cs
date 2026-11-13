using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Collections.Generic;

namespace ClickerApp;

internal static class MouseInput
{
    private const uint LeftDown = 0x0002;
    private const uint LeftUp = 0x0004;
    private const uint RightDown = 0x0008;
    private const uint RightUp = 0x0010;
    private const uint MiddleDown = 0x0020;
    private const uint MiddleUp = 0x0040;
    private const uint XButtonDown = 0x0080;
    private const uint XButtonUp = 0x0100;
    private const uint Wheel = 0x0800;
    private static readonly object ButtonLock = new();
    private static readonly Dictionary<MouseButtonChoice, int> ButtonReferences = [];

    public static void MoveTo(int x, int y)
    {
        if (!SetCursorPos(x, y))
        {
            throw new Win32Exception(Marshal.GetLastWin32Error());
        }
    }

    public static Point GetCursorPosition()
    {
        if (!GetCursorPos(out Point point))
        {
            throw new Win32Exception(Marshal.GetLastWin32Error());
        }

        return point;
    }

    public static void Scroll(int delta)
    {
        var input = new NativeInput
        {
            Type = 0,
            Data = new InputUnion
            {
                Mouse = new NativeMouseInput
                {
                    MouseData = unchecked((uint)delta),
                    Flags = Wheel
                }
            }
        };

        uint sent = SendInput(1, [input], Marshal.SizeOf<NativeInput>());
        if (sent != 1)
        {
            throw new Win32Exception(Marshal.GetLastWin32Error());
        }
    }

    public static void SetButton(MouseButtonChoice button, bool isDown)
    {
        lock (ButtonLock)
        {
            int current = ButtonReferences.GetValueOrDefault(button);
            if (isDown)
            {
                ButtonReferences[button] = current + 1;
                if (current > 0)
                {
                    return;
                }
            }
            else
            {
                if (current <= 0)
                {
                    return;
                }

                if (current > 1)
                {
                    ButtonReferences[button] = current - 1;
                    return;
                }

                ButtonReferences.Remove(button);
            }

            try
            {
                SendButtonInput(button, isDown);
            }
            catch (Win32Exception)
            {
                if (isDown)
                {
                    ButtonReferences.Remove(button);
                }
                else
                {
                    ButtonReferences[button] = 1;
                }

                throw;
            }
        }
    }

    private static void SendButtonInput(MouseButtonChoice button, bool isDown)
    {
        (uint flags, uint mouseData) = (button, isDown) switch
        {
            (MouseButtonChoice.Left, true) => (LeftDown, 0u),
            (MouseButtonChoice.Left, false) => (LeftUp, 0u),
            (MouseButtonChoice.Right, true) => (RightDown, 0u),
            (MouseButtonChoice.Right, false) => (RightUp, 0u),
            (MouseButtonChoice.Middle, true) => (MiddleDown, 0u),
            (MouseButtonChoice.Middle, false) => (MiddleUp, 0u),
            (MouseButtonChoice.XButton1, true) => (XButtonDown, 1u),
            (MouseButtonChoice.XButton1, false) => (XButtonUp, 1u),
            (MouseButtonChoice.XButton2, true) => (XButtonDown, 2u),
            (MouseButtonChoice.XButton2, false) => (XButtonUp, 2u),
            _ => throw new ArgumentOutOfRangeException(nameof(button))
        };

        var input = new NativeInput
        {
            Type = 0,
            Data = new InputUnion
            {
                Mouse = new NativeMouseInput { Flags = flags, MouseData = mouseData }
            }
        };

        uint sent = SendInput(1, [input], Marshal.SizeOf<NativeInput>());
        if (sent != 1)
        {
            throw new Win32Exception(Marshal.GetLastWin32Error());
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct NativeInput
    {
        public uint Type;
        public InputUnion Data;
    }

    [StructLayout(LayoutKind.Explicit)]
    private struct InputUnion
    {
        [FieldOffset(0)]
        public NativeMouseInput Mouse;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct NativeMouseInput
    {
        public int Dx;
        public int Dy;
        public uint MouseData;
        public uint Flags;
        public uint Time;
        public UIntPtr ExtraInfo;
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern uint SendInput(uint inputCount, [In] NativeInput[] inputs, int inputSize);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool SetCursorPos(int x, int y);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetCursorPos(out Point point);
}
