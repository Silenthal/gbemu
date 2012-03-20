using System;
using System.Runtime.InteropServices;
using System.Security;

namespace GBEmu
{
	[StructLayout(LayoutKind.Sequential)]
	public struct NativeMessage
	{
		public IntPtr hWnd;
		public uint msg;
		public IntPtr wParam;
		public IntPtr lParam;
		public uint time;
		public System.Drawing.Point p;
	}

	static class NativeMethods
	{
		[DllImport("user32.dll")]
		public static extern short GetAsyncKeyState(int vKey);

		[DllImport("user32.dll")]
		public static extern short GetKeyState(int vKey);
	}

	static class UnsafeNativeMethods
	{
		[SuppressUnmanagedCodeSecurity]
		[return: MarshalAs(UnmanagedType.Bool)]
		[DllImport("User32.dll", CharSet = CharSet.Auto, SetLastError = true)]
		public static extern bool PeekMessage(out NativeMessage lpMsg, IntPtr hWnd, uint wMsgFilterMin, uint wMsgFilterMax, uint wRemoveMsg);

		[SuppressUnmanagedCodeSecurity]
		[return: MarshalAs(UnmanagedType.Bool)]
		[DllImport("kernel32.dll")]
		public static extern bool QueryPerformanceCounter(out long lpPerformanceCount);

		[SuppressUnmanagedCodeSecurity]
		[return: MarshalAs(UnmanagedType.Bool)]
		[DllImport("kernel32.dll")]
		public static extern bool QueryPerformanceFrequency(out long lpFrequency);
	}
}
