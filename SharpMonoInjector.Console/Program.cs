using System;
using System.IO;

namespace SharpMonoInjector.Console {
    internal static class Program {
        static void Main(string[] args) {
            System.Console.WriteLine("\nSharpMonoInjector4.8");
            if (args.Length == 0) PrintHelp();

            CommandLineArguments cla = new CommandLineArguments(args);

            bool inject = cla.IsSwitchPresent("inject");
            bool eject = cla.IsSwitchPresent("eject");

            if (!inject && !eject) PrintHelp("No operation (inject/eject) specified");

            Injector injector;

            if (cla.GetIntArg("-p", out int pid)) {
                injector = new Injector(pid);
            }

            else if (cla.GetStringArg("-p", out string pname)) {
                injector = new Injector(pname);
            }

            else {
                PrintHelp("No process id/name specified");
                return;
            }

            if (inject) {
                Inject(injector, cla);
            }

            else {
                Eject(injector, cla);
            }
        }

        static void PrintHelp(string additionalMessage = "") {
            const string help =
                "Usage:\n" +
                "smi.exe <inject/eject> <options>\n\n" +
                "Options:\n" +
                "-p - The id or name of the target process\n" +
                "-a - When injecting, the path of the assembly to inject. When ejecting, the address of the assembly to eject\n" +
                "-n - The namespace in which the loader class resides\n" +
                "-c - The name of the loader class\n" +
                "-m - The name of the method to invoke in the loader class\n\n" +
                "Examples:\n" +
                "SharpMonoInjector4.8.exe inject -p RobocraftClient -a rc15-hax.dll -n RC15_HAX -c Loader -m Load\n" +
                "SharpMonoInjector4.8.exe eject -p RobocraftClient -a 0x13D23A98 -n RC15_HAX -c Loader -m Unload\n";

            if (!string.IsNullOrEmpty(additionalMessage)) System.Console.WriteLine(additionalMessage);
            System.Console.WriteLine(help);
            Environment.Exit(0);
        }

        static void Inject(Injector injector, CommandLineArguments args) {
            string assemblyPath, @namespace, className, methodName;
            byte[] assembly;

            if (args.GetStringArg("-a", out assemblyPath)) {
                try {
                    assembly = File.ReadAllBytes(assemblyPath);
                }

                catch {
                    System.Console.WriteLine($"Could not read the file {assemblyPath}");
                    return;
                }
            }

            else {
                System.Console.WriteLine("No assembly specified");
                return;
            }

            args.GetStringArg("-n", out @namespace);

            if (!args.GetStringArg("-c", out className)) {
                System.Console.WriteLine("No class name specified");
                return;
            }

            if (!args.GetStringArg("-m", out methodName)) {
                System.Console.WriteLine("No method name specified");
                return;
            }

            using (injector) {
                IntPtr remoteAssembly = IntPtr.Zero;

                try {
                    remoteAssembly = injector.Inject(assembly, @namespace, className, methodName);
                }

                catch (InjectorException ie) {
                    System.Console.WriteLine($"Ejection failed: {ie}");
                }

                if (remoteAssembly == IntPtr.Zero) return;

                System.Console.WriteLine($"{Path.GetFileName(assemblyPath)}: " + (injector.Is64Bit ? $"0x{remoteAssembly.ToInt64():X16}" : $"0x{remoteAssembly.ToInt32():X8}"));
            }
        }

        static void Eject(Injector injector, CommandLineArguments args) {
            IntPtr assembly;
            string @namespace, className, methodName;

            if (args.GetIntArg("-a", out int intPtr)) {
                assembly = (IntPtr)intPtr;
            }

            else if (args.GetLongArg("-a", out long longPtr)) {
                assembly = (IntPtr)longPtr;
            }

            else {
                System.Console.WriteLine("No assembly pointer specified");
                return;
            }

            args.GetStringArg("-n", out @namespace);

            if (!args.GetStringArg("-c", out className)) {
                System.Console.WriteLine("No class name specified");
                return;
            }

            if (!args.GetStringArg("-m", out methodName)) {
                System.Console.WriteLine("No method name specified");
                return;
            }

            using (injector) {
                try {
                    injector.Eject(assembly, @namespace, className, methodName);
                    System.Console.WriteLine("Ejection successful");
                }

                catch (InjectorException ie) {
                    System.Console.WriteLine($"Ejection failed: {ie}");
                }
            }
        }
    }
}
