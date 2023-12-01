using System;

namespace SharpMonoInjector;

public struct ExportedFunction(string name, IntPtr address) {
    internal string name = name;
    internal IntPtr address = address;
}
