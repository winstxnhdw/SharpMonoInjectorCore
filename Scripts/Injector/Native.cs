using System;
using System.Text;
using System.Runtime.InteropServices;

namespace SharpMonoInjector;

[StructLayout(LayoutKind.Sequential)]
public struct MODULEINFO {
    public IntPtr lpBaseOfDll;
    public IntPtr entryPoint;
    public int sizeOfImage;
}

public static class Native {
    [DllImport("kernel32.dll", SetLastError = true)]
    public static extern IntPtr OpenProcess(uint dwDesiredAccess, bool bInheritHandle, int processId);

    [DllImport("kernel32.dll", SetLastError = true)]
    public static extern bool CloseHandle(IntPtr handle);

    [DllImport("psapi.dll", SetLastError = true)]
    public static extern bool EnumProcessModulesEx(IntPtr hProcess, [In][Out] IntPtr[] lphModule, int cb, out int lpcbNeeded, uint dwFilterFlag);

    [DllImport("psapi.dll", SetLastError = true)]
    public static extern uint GetModuleFileNameEx(IntPtr hProcess, IntPtr hModule, [Out] StringBuilder lpBaseName, uint nSize);

    [DllImport("psapi.dll", SetLastError = true)]
    public static extern bool GetModuleInformation(IntPtr hProcess, IntPtr hModule, out MODULEINFO lpmodinfo, uint cb);

    [DllImport("kernel32.dll", SetLastError = true)]
    public static extern bool WriteProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, byte[] lpBuffer, int nSize, int lpNumberOfBytesWritten = 0);

    [DllImport("kernel32.dll", SetLastError = true)]
    public static extern bool ReadProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, byte[] lpBuffer, int nSize, int lpNumberOfBytesRead = 0);

    [DllImport("kernel32.dll", SetLastError = true)]
    public static extern IntPtr VirtualAllocEx(IntPtr hProcess, IntPtr lpAddress, int dwSize, uint flAllocationType, uint flProtect);

    [DllImport("kernel32.dll", SetLastError = true)]
    public static extern bool VirtualFreeEx(IntPtr hProcess, IntPtr lpAddress, int dwSize, uint dwFreeType);

    [DllImport("kernel32.dll", SetLastError = true)]
    public static extern IntPtr CreateRemoteThread(IntPtr hProcess, IntPtr lpThreadAttributes, int dwStackSize, IntPtr lpStartAddress, IntPtr lpParameter, uint dwCreationFlags, out int lpThreadId);

    [DllImport("kernel32.dll", SetLastError = true)]
    public static extern uint WaitForSingleObject(IntPtr hHandle, int dwMilliseconds);

    public const uint PROCESS_ALL_ACCESS = 0x1FFFFF;
    public const uint LIST_MODULES_ALL = 0x03;
    public const uint MEM_COMMIT = 0x00001000;
    public const uint PAGE_EXECUTE_READWRITE = 0x40;
    public const uint MEM_RELEASE = 0x8000;
    public const uint WAIT_FAILED = 0xFFFFFFFF;
}
