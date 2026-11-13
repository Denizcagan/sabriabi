using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace ClickerApp;

internal sealed class GlobalInputHook : IDisposable
{
    private const int WhKeyboardLl = 13;
    private const int WhMouseLl = 14;
    private const int WmKeyDown = 0x0100;
    private const int WmKeyUp = 0x0101;
    private const int WmSysKeyDown = 0x0104;
    private const int WmSysKeyUp = 0x0105;
    private const int WmLButtonDown = 0x0201;
    private const int WmLButtonUp = 0x0202;
    private const int WmRButtonDown = 0x0204;
    private const int WmRButtonUp = 0x0205;
    private const int WmMButtonDown = 0x0207;
    private const int WmMButtonUp = 0x0208;
    private const int WmXButtonDown = 0x020B;
    private const int WmXButtonUp = 0x020C;
    private const uint LlkhfInjected = 0x10;
    private const uint LlmhfInjected = 0x01;

    private readonly HookProcedure _keyboardCallback;
    private readonly HookProcedure _mouseCallback;
    private IntPtr _keyboardHandle;
    private IntPtr _mouseHandle;
    private readonly HashSet<InputBinding> _inputsDown = [];

    public GlobalInputHook(InputBinding activationBinding)
    {
        ActivationBinding = activationBinding;
        _keyboardCallback = KeyboardHookCallback;
        _mouseCallback = MouseHookCallback;

        _keyboardHandle = SetWindowsHookEx(WhKeyboardLl, _keyboardCallback, GetModuleHandle(null), 0);
        if (_keyboardHandle == IntPtr.Zero)
        {
            throw new Win32Exception(Marshal.GetLastWin32Error());
        }

        _mouseHandle = SetWindowsHookEx(WhMouseLl, _mouseCallback, GetModuleHandle(null), 0);
        if (_mouseHandle == IntPtr.Zero)
        {
            int error = Marshal.GetLastWin32Error();
            UnhookWindowsHookEx(_keyboardHandle);
            _keyboardHandle = IntPtr.Zero;
            throw new Win32Exception(error);
        }
    }

    public event Action<InputBinding, bool, bool>? InputChanged;

    public InputBinding ActivationBinding { get; set; }

    public bool IsCapturing { get; set; }

    public void ResetActivationState()
    {
        _inputsDown.Clear();
    }

    public void Dispose()
    {
        List<int> errors = [];
        if (_mouseHandle != IntPtr.Zero)
        {
            if (!UnhookWindowsHookEx(_mouseHandle))
            {
                errors.Add(Marshal.GetLastWin32Error());
            }
            else
            {
                _mouseHandle = IntPtr.Zero;
            }
        }

        if (_keyboardHandle != IntPtr.Zero)
        {
            if (!UnhookWindowsHookEx(_keyboardHandle))
            {
                errors.Add(Marshal.GetLastWin32Error());
            }
            else
            {
                _keyboardHandle = IntPtr.Zero;
            }
        }

        if (errors.Count > 0)
        {
            throw new Win32Exception(errors[0]);
        }
    }

    private IntPtr KeyboardHookCallback(int code, IntPtr message, IntPtr dataPointer)
    {
        if (code >= 0)
        {
            int messageId = message.ToInt32();
            bool isDown = messageId is WmKeyDown or WmSysKeyDown;
            bool isUp = messageId is WmKeyUp or WmSysKeyUp;
            if (isDown || isUp)
            {
                var data = Marshal.PtrToStructure<KeyboardHookData>(dataPointer);
                bool injected = (data.Flags & LlkhfInjected) != 0;
                ProcessInput(new InputBinding(InputKind.Keyboard, (int)data.VirtualKeyCode), isDown, injected);
            }
        }

        return CallNextHookEx(_keyboardHandle, code, message, dataPointer);
    }

    private IntPtr MouseHookCallback(int code, IntPtr message, IntPtr dataPointer)
    {
        if (code >= 0)
        {
            int messageId = message.ToInt32();
            bool isDown = messageId is WmLButtonDown or WmRButtonDown or WmMButtonDown or WmXButtonDown;
            bool isUp = messageId is WmLButtonUp or WmRButtonUp or WmMButtonUp or WmXButtonUp;
            if (isDown || isUp)
            {
                var data = Marshal.PtrToStructure<MouseHookData>(dataPointer);
                int button = messageId switch
                {
                    WmLButtonDown or WmLButtonUp => (int)MouseButtonChoice.Left,
                    WmRButtonDown or WmRButtonUp => (int)MouseButtonChoice.Right,
                    WmMButtonDown or WmMButtonUp => (int)MouseButtonChoice.Middle,
                    WmXButtonDown or WmXButtonUp => (int)((data.MouseData >> 16) & 0xFFFF) == 1
                        ? (int)MouseButtonChoice.XButton1
                        : (int)MouseButtonChoice.XButton2,
                    _ => -1
                };
                bool injected = (data.Flags & LlmhfInjected) != 0;
                ProcessInput(new InputBinding(InputKind.Mouse, button), isDown, injected);
            }
        }

        return CallNextHookEx(_mouseHandle, code, message, dataPointer);
    }

    private void ProcessInput(InputBinding binding, bool isDown, bool injected)
    {
        if (injected || binding.Code < 0)
        {
            return;
        }

        if (IsCapturing && isDown)
        {
            InputChanged?.Invoke(binding, true, true);
            return;
        }

        bool changed = isDown ? _inputsDown.Add(binding) : _inputsDown.Remove(binding);
        if (!changed)
        {
            return;
        }

        InputChanged?.Invoke(binding, isDown, false);
    }

    private delegate IntPtr HookProcedure(int code, IntPtr message, IntPtr dataPointer);

    [StructLayout(LayoutKind.Sequential)]
    private struct KeyboardHookData
    {
        public uint VirtualKeyCode;
        public uint ScanCode;
        public uint Flags;
        public uint Time;
        public UIntPtr ExtraInfo;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct MouseHookData
    {
        public PointData Point;
        public uint MouseData;
        public uint Flags;
        public uint Time;
        public UIntPtr ExtraInfo;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct PointData
    {
        public int X;
        public int Y;
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr SetWindowsHookEx(
        int hookType,
        HookProcedure callback,
        IntPtr moduleHandle,
        uint threadId);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool UnhookWindowsHookEx(IntPtr hookHandle);

    [DllImport("user32.dll")]
    private static extern IntPtr CallNextHookEx(
        IntPtr hookHandle,
        int code,
        IntPtr message,
        IntPtr dataPointer);

    [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern IntPtr GetModuleHandle(string? moduleName);
}
