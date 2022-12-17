# SharpMonoInjectorCore

![main.yml](https://github.com/winstxnhdw/SharpMonoInjectorCore/actions/workflows/main.yml/badge.svg)

SharpMonoInjectorCore is a tool for injecting assemblies into Mono embedded applications, made compatible with Microsoft .NET Core. The target process may require a restart before injecting an updated version of the assembly. Your unload method should destroy all allocated resources to prevent any memory leaks. Both x86 and x64 processes are supported. You can see an example implementation [here](https://github.com/winstxnhdw/rc15-hax/blob/master/rc15-hax/Scripts/Loader.cs).

## Requirements

- Windows 10 or higher
- [Microsoft .NET SDK](https://dotnet.microsoft.com/en-us/download)

## Concept

SharpMonoInjector works by dynamically generating machine code, writing it to the target process, and executing it using `CreateRemoteThread`. It calls functions within the Mono embedded API. The return value is obtained with `ReadProcessMemory`.

## Build

```bash
dotnet publish SharpMonoInjector
```

## Usage

Inject

```bash
SharpMonoInjector.exe inject -p RobocraftClient -a rc15-hax.dll -n RC15_HAX -c Loader -m Load
```

```yaml
Description:
  Injects a managed assembly into a process

Usage:
  SharpMonoInjector inject [options]

Options:
  -a, --assembly <assembly>    Path of the assembly to inject []
  -p, --process <process>      Name of the target process []
  -n, --namespace <namespace>  Namespace in which the loader class resides []
  -c, --class <class>          Name of the loader class ["Loader"]
  -m, --method <method>        Name of the method to invoke in the loader class ["Unload"]
  -?, -h, --help               Show help and usage information
```

Eject

```bash
SharpMonoInjector.exe eject -p RobocraftClient -a 0x13D23A98 -n RC15_HAX -c Loader -m Unload
```

```yaml
Description:
  Ejects a managed assembly from a process

Usage:
  SharpMonoInjector eject [options]

Options:
  -a, --address <address>      Address of the assembly to eject [0]
  -p, --process <process>      Name of the target process []
  -n, --namespace <namespace>  Namespace in which the loader class resides []
  -c, --class <class>          Name of the loader class ["Loader"]
  -m, --method <method>        Name of the method to invoke in the loader class ["Unload"]
  -?, -h, --help               Show help and usage information
```

## Submodule

If you plan to package this along with your cheats, I would recommend that you add this repository as a git submodule.

```bash
mkdir submodules
git submodule add --depth 1 https://github.com/winstxnhdw/SharpMonoInjectorCore.git ./submodules/SharpMonoInjectorCore
git config -f .gitmodules submodule.submodules/SharpMonoInjectorCore.shallow true
```

To update your submodule later, simply run the following.

```bash
git submodule update --remote
```

## Used by

- [rc15-hax](https://github.com/winstxnhdw/rc15-hax)
- [raft-hax](https://github.com/winstxnhdw/raft-hax)
