using System;
using System.IO;
using System.CommandLine;
using SharpMonoInjector;

void Inject(string targetProcess, FileInfo assembly, string loaderNamespace, string loaderClass) {
    using Injector injector = new(targetProcess);

    string assemblyPath = assembly.FullName;
    byte[] assemblyBytes = File.ReadAllBytes(assemblyPath);

    try {
        IntPtr remoteAssembly = injector.Inject(assemblyBytes, loaderNamespace, loaderClass);
        if (remoteAssembly == IntPtr.Zero) return;

        Console.WriteLine($"{assemblyPath}: 0x{remoteAssembly.ToInt64():X16}");
    }

    catch (InjectorException ie) {
        Console.WriteLine($"Injection failed: {ie}");
    }
}

RootCommand rootCommand = new(
    "SharpMonoInjectorCore is a tool for injecting assemblies into Mono embedded applications, made compatible with Microsoft .NET Core. " +
    "The target process may require a restart before injecting an updated version of the assembly. " +
    "Both x86 and x64 processes are supported. " +
    "You can see an example implementation here: https://github.com/winstxnhdw/rc15-hax/blob/master/rc15-hax/Scripts/Loader.cs"
);

Command injectCommand = new("inject", "Injects a managed assembly into a process");

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

Option<FileInfo> assembly = OptionHelper.CreateOption<FileInfo>(
    name: "--assembly",
    alias: "-a",
    description: "Path of the assembly to inject"
);

injectCommand.Add(targetProcess);
injectCommand.Add(loaderNamespace);
injectCommand.Add(loaderClass);
injectCommand.Add(assembly);

rootCommand.Add(injectCommand);

injectCommand.SetHandler(
    Inject,
    targetProcess,
    assembly,
    loaderNamespace,
    loaderClass
);

rootCommand.Invoke(args);
