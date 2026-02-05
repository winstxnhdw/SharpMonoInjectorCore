using System;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.ComponentModel;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace SharpMonoInjector;
public class Injector : IDisposable {
    const string mono_get_root_domain = "mono_get_root_domain";
    const string mono_thread_attach = "mono_thread_attach";
    const string mono_image_open_from_data = "mono_image_open_from_data";
    const string mono_assembly_load_from_full = "mono_assembly_load_from_full";
    const string mono_assembly_get_image = "mono_assembly_get_image";
    const string mono_class_from_name = "mono_class_from_name";
    const string mono_class_get_method_from_name = "mono_class_get_method_from_name";
    const string mono_runtime_invoke = "mono_runtime_invoke";
    const string mono_image_strerror = "mono_image_strerror";
    const string mono_object_get_class = "mono_object_get_class";
    const string mono_class_get_name = "mono_class_get_name";

    Dictionary<string, IntPtr> Exports { get; } = new Dictionary<string, IntPtr> {
        { mono_get_root_domain, IntPtr.Zero },
        { mono_thread_attach, IntPtr.Zero },
        { mono_image_open_from_data, IntPtr.Zero },
        { mono_assembly_load_from_full, IntPtr.Zero },
        { mono_assembly_get_image, IntPtr.Zero },
        { mono_class_from_name, IntPtr.Zero },
        { mono_class_get_method_from_name, IntPtr.Zero },
        { mono_runtime_invoke, IntPtr.Zero },
        { mono_image_strerror, IntPtr.Zero },
        { mono_object_get_class, IntPtr.Zero },
        { mono_class_get_name, IntPtr.Zero }
    };

    Memory memory;

    IntPtr rootDomain;

    bool attach;

    readonly IntPtr handle;

    IntPtr mono;

    public Injector(string processName) {
        processName = processName.EndsWith(".exe") ? processName.Replace(".exe", "") : processName;

        Process process =
            Process.GetProcesses()
                   .FirstOrDefault(p => p.ProcessName.Equals(processName, StringComparison.OrdinalIgnoreCase))
                    ?? throw new InjectorException($"Could not find a process with the name {processName}");

        if ((this.handle = Native.OpenProcess(ProcessAccessRights.PROCESS_ALL_ACCESS, false, process.Id)) == IntPtr.Zero) {
            throw new InjectorException("Failed to open process", new Win32Exception(Marshal.GetLastWin32Error()));
        }

        if (!ProcessUtils.GetMonoModule(this.handle, out this.mono)) {
            Native.CloseHandle(this.handle);
            throw new InjectorException("Failed to find mono.dll in the target process");
        }

        this.memory = new Memory(this.handle);
    }

    public Injector(IntPtr processHandle, IntPtr monoModule) {
        if ((this.handle = processHandle) == IntPtr.Zero) throw new ArgumentException("Argument cannot be zero", nameof(processHandle));
        if ((this.mono = monoModule) == IntPtr.Zero) throw new ArgumentException("Argument cannot be zero", nameof(monoModule));

        this.memory = new Memory(this.handle);
    }

    public void Dispose() {
        this.memory.Dispose();
        Native.CloseHandle(this.handle);
    }

    void ObtainMonoExports() {
        foreach (ExportedFunction ef in ProcessUtils.GetExportedFunctions(this.handle, this.mono)) {
            if (this.Exports.ContainsKey(ef.name)) this.Exports[ef.name] = ef.address;
        }

        foreach (KeyValuePair<string, IntPtr> kvp in this.Exports) {
            if (kvp.Value == IntPtr.Zero) {
                throw new InjectorException($"Failed to obtain the address of {kvp.Key}()");
            }
        }
    }

    public IntPtr Inject(byte[] rawAssembly, string @namespace, string className) {
        if (rawAssembly == null) throw new ArgumentNullException(nameof(rawAssembly));
        if (rawAssembly.Length == 0) throw new ArgumentException($"{nameof(rawAssembly)} cannot be empty", nameof(rawAssembly));
        if (className == null) throw new ArgumentNullException(nameof(className));

        IntPtr rawImage, assembly, image, @class, method;

        this.ObtainMonoExports();
        this.rootDomain = this.GetRootDomain();
        rawImage = this.OpenImageFromData(rawAssembly);
        this.attach = true;
        assembly = this.OpenAssemblyFromImage(rawImage);
        image = this.GetImageFromAssembly(assembly);
        @class = this.GetClassFromName(image, @namespace, className);
        method = this.GetMethodFromName(@class, "Load");
        this.RuntimeInvoke(method);
        return assembly;
    }

    static void ThrowIfNull(IntPtr ptr, string methodName) {
        if (ptr != IntPtr.Zero) return;
        throw new InjectorException($"{methodName}() returned NULL");
    }

    IntPtr GetRootDomain() {
        IntPtr rootDomain = this.Execute(this.Exports[mono_get_root_domain]);
        ThrowIfNull(rootDomain, mono_get_root_domain);
        return rootDomain;
    }

    IntPtr OpenImageFromData(byte[] assembly) {
        IntPtr statusPtr = this.memory.Allocate(4);
        IntPtr rawImage = this.Execute(this.Exports[mono_image_open_from_data], this.memory.AllocateAndWrite(assembly), assembly.Length, 1, statusPtr);

        MonoImageOpenStatus status = (MonoImageOpenStatus)this.memory.ReadInt(statusPtr);

        if (status != MonoImageOpenStatus.MONO_IMAGE_OK) {
            IntPtr messagePtr = this.Execute(this.Exports[mono_image_strerror], (IntPtr)status);
            string message = this.memory.ReadString(messagePtr, 256, Encoding.UTF8);
            throw new InjectorException($"{mono_image_open_from_data}() failed: {message}");
        }

        return rawImage;
    }

    IntPtr OpenAssemblyFromImage(IntPtr image) {
        IntPtr statusPtr = this.memory.Allocate(4);
        IntPtr assembly = this.Execute(this.Exports[mono_assembly_load_from_full], image, this.memory.AllocateAndWrite(new byte[1]), statusPtr, IntPtr.Zero);

        MonoImageOpenStatus status = (MonoImageOpenStatus)this.memory.ReadInt(statusPtr);

        if (status != MonoImageOpenStatus.MONO_IMAGE_OK) {
            IntPtr messagePtr = this.Execute(this.Exports[mono_image_strerror], (IntPtr)status);
            string message = this.memory.ReadString(messagePtr, 256, Encoding.UTF8);
            throw new InjectorException($"{mono_assembly_load_from_full}() failed: {message}");
        }

        return assembly;
    }

    IntPtr GetImageFromAssembly(IntPtr assembly) {
        IntPtr image = this.Execute(this.Exports[mono_assembly_get_image], assembly);
        ThrowIfNull(image, mono_assembly_get_image);
        return image;
    }

    IntPtr GetClassFromName(IntPtr image, string @namespace, string className) {
        IntPtr @class = this.Execute(this.Exports[mono_class_from_name], image, this.memory.AllocateAndWrite(@namespace), this.memory.AllocateAndWrite(className));
        ThrowIfNull(@class, mono_class_from_name);
        return @class;
    }

    IntPtr GetMethodFromName(IntPtr @class, string methodName) {
        IntPtr method = this.Execute(this.Exports[mono_class_get_method_from_name], @class, this.memory.AllocateAndWrite(methodName), IntPtr.Zero);
        ThrowIfNull(method, mono_class_get_method_from_name);
        return method;
    }

    string GetClassName(IntPtr monoObject) {
        IntPtr @class = this.Execute(this.Exports[mono_object_get_class], monoObject);
        ThrowIfNull(@class, mono_object_get_class);
        IntPtr className = this.Execute(this.Exports[mono_class_get_name], @class);
        ThrowIfNull(className, mono_class_get_name);
        return this.memory.ReadString(className, 256, Encoding.UTF8);
    }

    string ReadMonoString(IntPtr monoString) {
        int len = this.memory.ReadInt(monoString + 0x10);
        return this.memory.ReadUnicodeString(monoString + 0x14, len * 2);
    }

    void RuntimeInvoke(IntPtr method) {
        IntPtr excPtr = this.memory.AllocateAndWrite((long)0);
        this.Execute(this.Exports[mono_runtime_invoke], method, IntPtr.Zero, IntPtr.Zero, excPtr);

        IntPtr exc = (IntPtr)this.memory.ReadLong(excPtr);

        if (exc != IntPtr.Zero) {
            string className = this.GetClassName(exc);
            string message = this.ReadMonoString((IntPtr)this.memory.ReadLong(exc + 0x20));
            throw new InjectorException($"The managed method threw an exception: ({className}) {message}");
        }
    }

    IntPtr Execute(IntPtr address, params IntPtr[] args) {
        IntPtr retValPtr = this.memory.AllocateAndWrite((long)0);

        byte[] code = this.Assemble(address, retValPtr, args);
        IntPtr alloc = this.memory.AllocateAndWrite(code);

        IntPtr thread = Native.CreateRemoteThread(this.handle, IntPtr.Zero, 0, alloc, IntPtr.Zero, 0, out _);

        if (thread == IntPtr.Zero)
            throw new InjectorException("Failed to create a remote thread", new Win32Exception(Marshal.GetLastWin32Error()));

        WaitResult result = Native.WaitForSingleObject(thread, -1);
        Native.CloseHandle(thread);

        if (result == WaitResult.WAIT_FAILED)
            throw new InjectorException("Failed to wait for a remote thread", new Win32Exception(Marshal.GetLastWin32Error()));

        IntPtr ret = (IntPtr)this.memory.ReadLong(retValPtr);

        return ret == 0x00000000C0000005
            ? throw new InjectorException($"An access violation occurred while executing {this.Exports.First(e => e.Value == address).Key}()")
            : ret;
    }

    byte[] Assemble(IntPtr functionPtr, IntPtr retValPtr, IntPtr[] args) {
        Assembler asm = new();

        asm.SubRsp(40);

        if (this.attach) {
            asm.MovRax(this.Exports[mono_thread_attach]);
            asm.MovRcx(this.rootDomain);
            asm.CallRax();
        }

        asm.MovRax(functionPtr);

        for (int i = 0; i < args.Length; i++) {
            switch (i) {
                case 0:
                    asm.MovRcx(args[i]);
                    break;
                case 1:
                    asm.MovRdx(args[i]);
                    break;
                case 2:
                    asm.MovR8(args[i]);
                    break;
                case 3:
                    asm.MovR9(args[i]);
                    break;
            }
        }

        asm.CallRax();
        asm.AddRsp(40);
        asm.MovRaxTo(retValPtr);
        asm.Return();

        return asm.ToByteArray();
    }
}
