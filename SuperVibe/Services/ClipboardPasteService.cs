using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows;

namespace SuperVibe.Services;

public static class ClipboardPasteService
{
    // SendInput P/Invoke
    [DllImport("user32.dll", SetLastError = true)]
    private static extern uint SendInput(uint nInputs, INPUT[] pInputs, int cbSize);

    private const int INPUT_KEYBOARD = 1;
    private const int KEYEVENTF_KEYUP = 0x0002;

    private const ushort VK_CONTROL = 0x11;
    private const ushort VK_V = 0x56;
    private const ushort VK_RETURN = 0x0D;

    [StructLayout(LayoutKind.Sequential)]
    private struct INPUT
    {
        public int type;
        public INPUTUNION u;
    }

    // Union must be sized to largest member (MOUSEINPUT = 32 bytes on x64)
    [StructLayout(LayoutKind.Explicit, Size = 32)]
    private struct INPUTUNION
    {
        [FieldOffset(0)] public KEYBDINPUT ki;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct KEYBDINPUT
    {
        public ushort wVk;
        public ushort wScan;
        public uint dwFlags;
        public uint time;
        public IntPtr dwExtraInfo;
    }

    /// <summary>
    /// Copy text to clipboard and simulate Ctrl+V to paste into the foreground app.
    /// </summary>
    public static void Paste(string text, bool pressEnter = false)
    {
        // Clipboard must be accessed from an STA thread
        Clipboard.SetText(text, TextDataFormat.UnicodeText);

        // Simulate Ctrl+V
        var inputs = new INPUT[]
        {
            // Ctrl down
            MakeKeyInput(VK_CONTROL, false),
            // V down
            MakeKeyInput(VK_V, false),
            // V up
            MakeKeyInput(VK_V, true),
            // Ctrl up
            MakeKeyInput(VK_CONTROL, true),
        };
        SendInput((uint)inputs.Length, inputs, Marshal.SizeOf<INPUT>());

        if (pressEnter)
        {
            Thread.Sleep(100);
            var enterInputs = new INPUT[]
            {
                MakeKeyInput(VK_RETURN, false),
                MakeKeyInput(VK_RETURN, true),
            };
            SendInput((uint)enterInputs.Length, enterInputs, Marshal.SizeOf<INPUT>());
        }
    }

    private static INPUT MakeKeyInput(ushort vk, bool keyUp)
    {
        return new INPUT
        {
            type = INPUT_KEYBOARD,
            u = new INPUTUNION
            {
                ki = new KEYBDINPUT
                {
                    wVk = vk,
                    wScan = 0,
                    dwFlags = keyUp ? KEYEVENTF_KEYUP : 0u,
                    time = 0,
                    dwExtraInfo = IntPtr.Zero,
                }
            }
        };
    }
}
