using System;
using System.Linq;
using System.Collections.Generic;

namespace SharpMonoInjector;
public class Assembler {
    List<byte> OpCodes { get; } = [];

    public void MovRax(IntPtr arg) => this.OpCodes.AddRange(new byte[] { 0x48, 0xB8 }.Concat(BitConverter.GetBytes(arg)));

    public void MovRcx(IntPtr arg) => this.OpCodes.AddRange(new byte[] { 0x48, 0xB9 }.Concat(BitConverter.GetBytes(arg)));

    public void MovRdx(IntPtr arg) => this.OpCodes.AddRange(new byte[] { 0x48, 0xBA }.Concat(BitConverter.GetBytes(arg)));

    public void MovR8(IntPtr arg) => this.OpCodes.AddRange(new byte[] { 0x49, 0xB8 }.Concat(BitConverter.GetBytes(arg)));

    public void MovR9(IntPtr arg) => this.OpCodes.AddRange(new byte[] { 0x49, 0xB9 }.Concat(BitConverter.GetBytes(arg)));

    public void SubRsp(byte arg) => this.OpCodes.AddRange(new byte[] { 0x48, 0x83, 0xEC, arg });

    public void CallRax() => this.OpCodes.AddRange(new byte[] { 0xFF, 0xD0 });

    public void AddRsp(byte arg) => this.OpCodes.AddRange(new byte[] { 0x48, 0x83, 0xC4, arg });

    public void MovRaxTo(IntPtr dest) => this.OpCodes.AddRange(new byte[] { 0x48, 0xA3 }.Concat(BitConverter.GetBytes(dest)));

    public void Return() => this.OpCodes.Add(0xC3);

    public byte[] ToByteArray() => [.. this.OpCodes];
}
