# SharpMonoInjectorCore

SharpMonoInjectorCore is a tool for injecting assemblies into Mono embedded applications, made compatible with the Microsoft .NET Core. The target process may require a restart before injecting an updated version of the assembly. Your unload method should destroy all allocated resources to prevent any memory leaks. Both x86 and x64 processes are supported. You can see an example implementation [here](https://github.com/winstxnhdw/rc15-hax/blob/master/rc15-hax/Scripts/Loader.cs).

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
Usage:
SharpMonoInjector.exe inject <options>

Required arguments:
-p      id or name of the target process
-a      path of the assembly to inject
-n      namespace in which the loader class resides
-c      name of the loader class
-m      name of the method to invoke in the loader class
```

Eject

```bash
SharpMonoInjector.exe eject -p RobocraftClient -a 0x13D23A98 -n RC15_HAX -c Loader -m Unload
```

```yaml
Usage:
SharpMonoInjector.exe eject <options>

Required arguments:
-p      id or name of the target process
-a      address of the assembly to eject
-n      namespace in which the loader class resides
-c      name of the loader class
-m      name of the method to invoke in the loader class
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
