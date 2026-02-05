using System;
using System.Linq;
using System.Text;
using System.ComponentModel;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace SharpMonoInjector;

public static class ProcessUtils {
    // PE format offsets
    const int PE_HEADER_OFFSET = 0x3C;
    const int OPTIONAL_HEADER_OFFSET = 0x18;
    const int DATA_DIRECTORY_OFFSET_64 = 0x70;
    const int EXPORT_NAMES_OFFSET = 0x20;
    const int EXPORT_ORDINALS_OFFSET = 0x24;
    const int EXPORT_FUNCTIONS_OFFSET = 0x1C;
    const int EXPORT_NUM_NAMES_OFFSET = 0x18;

    public static IEnumerable<ExportedFunction> GetExportedFunctions(IntPtr handle, IntPtr mod) {
        using Memory memory = new(handle);
        int e_lfanew = memory.ReadInt(mod + PE_HEADER_OFFSET);
        IntPtr ntHeaders = mod + e_lfanew;
        IntPtr optionalHeader = ntHeaders + OPTIONAL_HEADER_OFFSET;
        IntPtr dataDirectory = optionalHeader + DATA_DIRECTORY_OFFSET_64;
        IntPtr exportDirectory = mod + memory.ReadInt(dataDirectory);
        IntPtr names = mod + memory.ReadInt(exportDirectory + EXPORT_NAMES_OFFSET);
        IntPtr ordinals = mod + memory.ReadInt(exportDirectory + EXPORT_ORDINALS_OFFSET);
        IntPtr functions = mod + memory.ReadInt(exportDirectory + EXPORT_FUNCTIONS_OFFSET);
        int count = memory.ReadInt(exportDirectory + EXPORT_NUM_NAMES_OFFSET);

        for (int i = 0; i < count; i++) {
            int offset = memory.ReadInt(names + (i * 4));
            string name = memory.ReadString(mod + offset, 32, Encoding.ASCII);
            short ordinal = memory.ReadShort(ordinals + (i * 2));
            IntPtr address = mod + memory.ReadInt(functions + (ordinal * 4));

            if (address != IntPtr.Zero) {
                yield return new ExportedFunction(name, address);
            }
        }
    }

    public static bool GetMonoModule(IntPtr handle, out IntPtr monoModule) {
        IntPtr[] ptrs = [];

        if (!Native.EnumProcessModulesEx(handle, ptrs, 0, out int bytesNeeded, Native.LIST_MODULES_ALL)) {
            throw new InjectorException("Failed to enumerate process modules", new Win32Exception(Marshal.GetLastWin32Error()));
        }

        int count = bytesNeeded / 8;
        ptrs = new IntPtr[count];

        if (!Native.EnumProcessModulesEx(handle, ptrs, bytesNeeded, out bytesNeeded, Native.LIST_MODULES_ALL)) {
            throw new InjectorException("Failed to enumerate process modules", new Win32Exception(Marshal.GetLastWin32Error()));
        }

        for (int i = 0; i < count; i++) {
            try {
                StringBuilder path = new(260);
                Native.GetModuleFileNameEx(handle, ptrs[i], path, 260);

                if (path.ToString().IndexOf("mono", StringComparison.OrdinalIgnoreCase) > -1) {
                    if (!Native.GetModuleInformation(handle, ptrs[i], out MODULEINFO info, (uint)Marshal.SizeOf<MODULEINFO>())) {
                        throw new InjectorException("Failed to get module information", new Win32Exception(Marshal.GetLastWin32Error()));
                    }

                    IEnumerable<ExportedFunction> funcs = GetExportedFunctions(handle, info.lpBaseOfDll);

                    if (funcs.Any(f => f.name == "mono_get_root_domain")) {
                        monoModule = info.lpBaseOfDll;
                        return true;
                    }
                }
            }

            catch (Exception ex) {
                Console.Error.WriteLine($"[ProcessUtils] GetMono - ERROR: {ex.Message}");
            }
        }

        monoModule = IntPtr.Zero;
        return false;
    }
}
