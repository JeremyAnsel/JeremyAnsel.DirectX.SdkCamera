using JeremyAnsel.DirectX.DXMath;
using System;
using System.Runtime.InteropServices;
using System.Security;

namespace JeremyAnsel.DirectX.SdkCamera
{
    [SecurityCritical, SuppressUnmanagedCodeSecurity]
    internal static class NativeMethods
    {
        [DllImport("user32.dll", EntryPoint = "GetClientRect")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool GetClientRect(IntPtr handle, out XMInt4 lpRect);

        [DllImport("user32.dll", EntryPoint = "GetClipCursor")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool GetClipCursor(out XMInt4 lpRect);

        [DllImport("user32.dll", EntryPoint = "GetCursorPos")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool GetCursorPos(out XMInt2 lpPoint);

        [DllImport("user32.dll", EntryPoint = "GetForegroundWindow")]
        public static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll", EntryPoint = "PtInRect")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool PtInRect(ref XMInt4 lprc, XMInt2 pt);

        [DllImport("user32.dll", EntryPoint = "ReleaseCapture")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool ReleaseCapture();

        [DllImport("user32.dll", EntryPoint = "SetCapture")]
        public static extern IntPtr SetCapture(IntPtr hWnd);

        [DllImport("user32.dll", EntryPoint = "SetCursorPos")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool SetCursorPos(int x, int y);
    }
}
