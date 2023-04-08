using System;
using System.Linq;
using System.Collections.Generic;

namespace SharpMonoInjector;
public class Assembler {
    List<byte> OpCodes { get; } = new List<byte>();

    public void Push(IntPtr arg) {
        OpCodes.Add((int)arg < 128 ? (byte)0x6A : (byte)0x68);
        OpCodes.AddRange((int)arg <= 255 ? new[] { (byte)arg } : BitConverter.GetBytes((int)arg));
    }

    public void MovRax(IntPtr arg) => OpCodes.AddRange(new byte[] { 0x48, 0xB8 }.Concat(BitConverter.GetBytes((long)arg)));

    public void MovRcx(IntPtr arg) => OpCodes.AddRange(new byte[] { 0x48, 0xB9 }.Concat(BitConverter.GetBytes((long)arg)));

    public void MovRdx(IntPtr arg) => OpCodes.AddRange(new byte[] { 0x48, 0xBA }.Concat(BitConverter.GetBytes((long)arg)));

    public void MovR8(IntPtr arg) => OpCodes.AddRange(new byte[] { 0x49, 0xB8 }.Concat(BitConverter.GetBytes((long)arg)));

    public void MovR9(IntPtr arg) => OpCodes.AddRange(new byte[] { 0x49, 0xB9 }.Concat(BitConverter.GetBytes((long)arg)));

    public void SubRsp(byte arg) => OpCodes.AddRange(new byte[] { 0x48, 0x83, 0xEC, arg });

    public void CallRax() => OpCodes.AddRange(new byte[] { 0xFF, 0xD0 });

    public void AddRsp(byte arg) => OpCodes.AddRange(new byte[] { 0x48, 0x83, 0xC4, arg });

    public void MovRaxTo(IntPtr dest) => OpCodes.AddRange(new byte[] { 0x48, 0xA3 }.Concat(BitConverter.GetBytes((long)dest)));

    public void MovEax(IntPtr arg) => OpCodes.AddRange(BitConverter.GetBytes((int)arg).Prepend<byte>(0xB8));

    public void CallEax() => OpCodes.AddRange(new byte[] { 0xFF, 0xD0 });

    public void AddEsp(byte arg) => OpCodes.AddRange(new byte[] { 0x83, 0xC4, arg });

    public void MovEaxTo(IntPtr dest) => OpCodes.AddRange(BitConverter.GetBytes((int)dest).Prepend<byte>(0xA3));

    public void Return() => OpCodes.Add(0xC3);

    public byte[] ToByteArray() => OpCodes.ToArray();
}