using System.Globalization;
using System.Linq;

namespace SharpMonoInjector;
public class CommandLineArguments {
    string[] Arguments { get; }

    public CommandLineArguments(string[] args) {
        this.Arguments = args;
    }

    public bool IsSwitchPresent(string switchName) => this.Arguments.Any(argument => argument == switchName);

    public bool GetLongArg(string name, out long value) {
        if (GetStringArg(name, out string str)) {
            return long.TryParse(str.StartsWith("0x") 
                 ? str.Substring(2) 
                 : str, NumberStyles.AllowHexSpecifier, null, out value);
        }

        value = default(long);
        return false;
    }

    public bool GetIntArg(string name, out int value) {
        if (GetStringArg(name, out string str)) {
            return int.TryParse(str.StartsWith("0x") 
                 ? str.Substring(2) 
                 : str, NumberStyles.AllowHexSpecifier, null, out value);
        }

        value = default(int);
        return false;
    }

    public bool GetStringArg(string name, out string value) {
        for (int i = 0; i < this.Arguments.Length; i++) {
            if (this.Arguments[i] != name) continue;
            if (i == this.Arguments.Length - 1) break;

            value = this.Arguments[i + 1];
            return true;
        }

        value = null;
        return false;
    }
}
