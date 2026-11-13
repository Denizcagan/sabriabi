using System.ComponentModel;
using System.Runtime.InteropServices;

namespace ClickerApp;

internal static class KeyboardInput
{
    private const uint KeyUp = 0x0002;
    private static readonly object KeyLock = new();
    private static readonly Dictionary<int, int> KeyReferences = [];

    public static void SetKeys(IEnumerable<int> keys, bool isDown)
    {
        int[] uniqueKeys = keys.Distinct().ToArray();
        lock (KeyLock)
        {
            var transitions = new List<(int Key, int NextCount)>();
            var inputs = new List<NativeInput>();

            foreach (int key in uniqueKeys)
            {
                int current = KeyReferences.GetValueOrDefault(key);
                if (isDown)
                {
                    if (current == 0)
                    {
                        inputs.Add(CreateInput(key, keyUp: false));
                    }

                    transitions.Add((key, current + 1));
                }
                else if (current > 1)
                {
                    transitions.Add((key, current - 1));
                }
                else if (current == 1)
                {
                    inputs.Add(CreateInput(key, keyUp: true));
                    transitions.Add((key, 0));
                }
            }

            if (inputs.Count > 0)
            {
                uint sent = SendInput((uint)inputs.Count, inputs.ToArray(), Marshal.SizeOf<NativeInput>());
                if (sent != inputs.Count)
                {
                    throw new Win32Exception(Marshal.GetLastWin32Error());
                }
            }

            foreach ((int key, int nextCount) in transitions)
            {
                if (nextCount == 0)
                {
                    KeyReferences.Remove(key);
                }
                else
                {
                    KeyReferences[key] = nextCount;
                }
            }
        }
    }

    private static NativeInput CreateInput(int key, bool keyUp) => new()
    {
        Type = 1,
        Data = new InputUnion
        {
            Keyboard = new NativeKeyboardInput
            {
                VirtualKey = (ushort)key,
                Flags = keyUp ? KeyUp : 0
            }
        }
    };

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
        public NativeKeyboardInput Keyboard;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct NativeKeyboardInput
    {
        public ushort VirtualKey;
        public ushort ScanCode;
        public uint Flags;
        public uint Time;
        public UIntPtr ExtraInfo;
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern uint SendInput(uint inputCount, [In] NativeInput[] inputs, int inputSize);
}
