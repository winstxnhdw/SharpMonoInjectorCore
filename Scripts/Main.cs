using System;
using System.IO;
using System.CommandLine;
using SharpMonoInjector;

void Inject(string targetProcess, FileInfo assembly, string loaderNamespace, string loaderClass, string loaderMethod) {
    using Injector injector = new(targetProcess);

    string assemblyPath = assembly.FullName;
    byte[] assemblyBytes = File.ReadAllBytes(assemblyPath);

    try {
        IntPtr remoteAssembly = injector.Inject(assemblyBytes, loaderNamespace, loaderClass, loaderMethod);
        if (remoteAssembly == IntPtr.Zero) return;

        Console.WriteLine($"{assemblyPath}: {(injector.Is64Bit ? $"0x{remoteAssembly.ToInt64():X16}" : $"0x{remoteAssembly.ToInt32():X8}")}");
    }

    catch (InjectorException ie) {
        Console.WriteLine($"Injection failed: {ie}");
    }
}

void Eject(string targetProcess, IntPtr assemblyAddress, string loaderNamespace, string loaderClass, string loaderMethod) {
    using Injector injector = new(targetProcess);

    try {
        injector.Eject(assemblyAddress, loaderNamespace, loaderClass, loaderMethod);
        Console.WriteLine("Ejection successful");
    }

    catch (InjectorException ie) {
        Console.WriteLine($"Ejection failed: {ie}");
    }
}

RootCommand rootCommand = new(
    "SharpMonoInjectorCore is a tool for injecting assemblies into Mono embedded applications, made compatible with Microsoft .NET Core. " +
    "The target process may require a restart before injecting an updated version of the assembly. " +
    "Your unload method should destroy all allocated resources to prevent any memory leaks. Both x86 and x64 processes are supported. " +
    "You can see an example implementation here: https://github.com/winstxnhdw/rc15-hax/blob/master/rc15-hax/Scripts/Loader.cs"
);

Command injectCommand = new("inject", "Injects a managed assembly into a process");
Command ejectCommand = new("eject", "Ejects a managed assembly from a process");

Option<string> targetProcess = OptionHelper.CreateOption<string>(
    name: "--process",
    alias: "-p",
    description: "Name of the target process"
);

Option<string> loaderNamespace = OptionHelper.CreateOption<string>(
    name: "--namespace",
    alias: "-n",
    description: "Namespace in which the loader class resides"
);

Option<string> loaderClass = OptionHelper.CreateOption(
    name: "--class",
    alias: "-c",
    description: "Name of the loader class",
    defaultValue: "Loader"
);

Option<string> loaderMethod = OptionHelper.CreateOption(
    name: "--method",
    alias: "-m",
    description: "Name of the method to invoke in the loader class",
    defaultValue: "Unload"
);

Option<FileInfo> assembly = OptionHelper.CreateOption<FileInfo>(
    name: "--assembly",
    alias: "-a",
    description: "Path of the assembly to inject"
);

Option<IntPtr> assemblyAddress = OptionHelper.CreateOption<IntPtr>(
    name: "--address",
    alias: "-a",
    description: "Address of the assembly to eject"
);

rootCommand.AddGlobalOption(targetProcess);
rootCommand.AddGlobalOption(loaderNamespace);
rootCommand.AddGlobalOption(loaderClass);
rootCommand.AddGlobalOption(loaderMethod);

injectCommand.Add(assembly);
ejectCommand.Add(assemblyAddress);

rootCommand.Add(injectCommand);
rootCommand.Add(ejectCommand);

injectCommand.SetHandler(
    Inject,
    targetProcess,
    assembly,
    loaderNamespace,
    loaderClass,
    loaderMethod
);

ejectCommand.SetHandler(
    Eject,
    targetProcess,
    assemblyAddress,
    loaderNamespace,
    loaderClass,
    loaderMethod
);

rootCommand.Invoke(args);
