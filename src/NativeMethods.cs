namespace Talkeo.Windows;

using System.Runtime.InteropServices;

internal static class NativeMethods
{
    public const int InputSize = 28;

    [StructLayout(LayoutKind.Sequential)]
    public struct INPUT { public uint type; public KEYBDINPUT ki; [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)] public byte[] padding; }

    [StructLayout(LayoutKind.Sequential)]
    public struct KEYBDINPUT { public ushort wVk; public ushort wScan; public uint dwFlags; public uint time; public IntPtr dwExtraInfo; }

    [DllImport("user32.dll")] public static extern uint SendInput(uint n, INPUT[] inputs, int cbSize);

    public static INPUT KeyInput(ushort vk, bool down) => new()
    {
        type = 1,
        ki   = new KEYBDINPUT { wVk = vk, dwFlags = down ? 0u : 0x0002u },
    };
}
