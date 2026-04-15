using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace SuperVibe.Services;

public class HotkeyManager : IDisposable
{
    // Win32 constants
    private const int WH_KEYBOARD_LL = 13;
    private const int WM_KEYDOWN = 0x0100;
    private const int WM_KEYUP = 0x0101;
    private const int WM_SYSKEYDOWN = 0x0104;
    private const int WM_SYSKEYUP = 0x0105;

    private const int VK_F9 = 0x78;
    private const int VK_SHIFT = 0x10;
    private const int VK_ESCAPE = 0x1B;

    // P/Invoke declarations
    private delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool UnhookWindowsHookEx(IntPtr hhk);

    [DllImport("user32.dll")]
    private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

    [DllImport("kernel32.dll")]
    private static extern IntPtr GetModuleHandle(string? lpModuleName);

    [DllImport("user32.dll")]
    private static extern short GetKeyState(int nVirtKey);

    [StructLayout(LayoutKind.Sequential)]
    private struct KBDLLHOOKSTRUCT
    {
        public int vkCode;
        public int scanCode;
        public int flags;
        public int time;
        public IntPtr dwExtraInfo;
    }

    // Callbacks
    public Action? OnToggleRecord { get; set; }
    public Action? OnToggleTranslate { get; set; }
    public Action? OnCancel { get; set; }

    // State
    private IntPtr _hookId = IntPtr.Zero;
    private LowLevelKeyboardProc? _hookProc; // prevent GC collection of delegate

    public void Start()
    {
        if (_hookId != IntPtr.Zero) return; // already started
        _hookProc = HookCallback;
        using var curProcess = Process.GetCurrentProcess();
        using var curModule = curProcess.MainModule!;
        _hookId = SetWindowsHookEx(WH_KEYBOARD_LL, _hookProc, GetModuleHandle(curModule.ModuleName), 0);

        if (_hookId == IntPtr.Zero)
        {
            var error = Marshal.GetLastWin32Error();
            Debug.WriteLine($"[Hotkey] Failed to install hook, error={error}");
        }
        else
        {
            Debug.WriteLine("[Hotkey] Listening (F9 = transcribe, Shift+F9 = translate, ESC = cancel)");
        }
    }

    public void Stop()
    {
        if (_hookId != IntPtr.Zero)
        {
            UnhookWindowsHookEx(_hookId);
            _hookId = IntPtr.Zero;
            Debug.WriteLine("[Hotkey] Hook removed");
        }
    }

    private static bool IsShiftDown() => (GetKeyState(VK_SHIFT) & 0x8000) != 0;

    private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
    {
        if (nCode >= 0)
        {
            var hookStruct = Marshal.PtrToStructure<KBDLLHOOKSTRUCT>(lParam);
            int msg = wParam.ToInt32();
            bool isKeyDown = msg == WM_KEYDOWN || msg == WM_SYSKEYDOWN;

            if (hookStruct.vkCode == VK_F9 && isKeyDown)
            {
                if (IsShiftDown())
                {
                    Debug.WriteLine("[Hotkey] Shift+F9 -> toggle translate");
                    OnToggleTranslate?.Invoke();
                }
                else
                {
                    Debug.WriteLine("[Hotkey] F9 -> toggle record");
                    OnToggleRecord?.Invoke();
                }
                return (IntPtr)1; // suppress F9 from reaching other apps
            }
            else if (hookStruct.vkCode == VK_ESCAPE && isKeyDown)
            {
                Debug.WriteLine("[Hotkey] ESC -> cancel");
                OnCancel?.Invoke();
                // Let ESC propagate normally
            }
        }

        return CallNextHookEx(_hookId, nCode, wParam, lParam);
    }

    public void Dispose()
    {
        Stop();
    }
}
