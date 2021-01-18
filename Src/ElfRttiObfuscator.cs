using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Diagnostics;
using System.Text;
using System.Security.Cryptography;

namespace RttiObfuscator
{
    public enum ElfClass : byte
    {
        None = 0,
        _32 = 1,
        _64 = 2
    }

    public enum SymbolType
    {
        NoType = 0,
        Object = 1,
        Func = 2,
        Section = 3,
        File = 4,
        LocProc = 13,
        HiProc = 15
    }

    [DebuggerDisplay("Name = {Name}, VirtualAddress = {VirtualAddress,h}, Offset = {Offset,h}, Size = {Size}, EntrySize = {EntrySize}")]
    public class Section
    {
        public UInt32 NameIndex;
        public String Name;
        public UInt64 VirtualAddress;
        public UInt64 Offset;
        public UInt64 Size;
        public UInt64 EntrySize;
    }

    [DebuggerDisplay("Name = {Name}, Value = {Value,h}, Size = {Size}, Info = {Info,h}, Type = {Type}, Other = {Other}, SectionIndex = {SectionIndex}")]
    public class Symbol
    {
        public UInt32 NameIndex;
        public String Name;
        public UInt64 Value;
        public UInt64 Size;
        public Byte Info;
        public Byte Other;
        public UInt64 SectionIndex;

        public SymbolType Type { get { return (SymbolType)(Info & 0xF); } }
    }

    public class RttiInfo
    {
        public String Name;
        public UInt64 Offset;
        public String Value;
    }

    public class ElfRttiObfuscator
    {
        public static void List(PathString Path)
        {
            var RttiInfoList = GetRttiInfoList(Path);
            foreach (var ri in RttiInfoList)
            {
                Console.WriteLine($"{ri.Offset:X8} {ri.Name} {ri.Value}");
            }
        }

        public static void Obfuscate(PathString InputPath, PathString OutputPath)
        {
            var RttiInfoList = GetRttiInfoList(InputPath);
            var NameMap = RttiInfoList.Select(ri => ri.Value).Distinct().OrderBy(n => n, StringComparer.Ordinal).Select(n => new { Name = n, Hash = GetHash(n) }).GroupBy(p => p.Hash).SelectMany(g => g.Count() == 1 ? g.Select(p => new KeyValuePair<String, String>(p.Name, "C" + p.Hash)) : g.Select((p, k) => new KeyValuePair<String, String>(p.Name, "C" + p.Hash + "_" + k.ToString()))).ToDictionary(p => p.Key, p => p.Value);

            var OutputDir = OutputPath.Parent;
            if (OutputDir != ".")
            {
                Directory.CreateDirectory(OutputDir);
            }
            if (InputPath != OutputPath)
            {
                File.Copy(InputPath, OutputPath, true);
            }

            var ItaniumBaseExp = System.Text.Encoding.UTF8.GetString(RttiObfuscator.Properties.Resources.ItaniumBaseExp);
            var ExcludedNames = new HashSet<String>(ItaniumBaseExp.Replace("\r\n", "\n").Split('\n').Where(Line => (Line != "") && !Line.StartsWith("#")).Select(Line => Line.Substring(1)));

            using (var fs = new FileStream(OutputPath, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
            using (var bw = new BinaryWriter(fs))
            {
                foreach (var ri in RttiInfoList)
                {
                    if (ExcludedNames.Contains(ri.Name)) { continue; }
                    var ObfuscatedValue = NameMap[ri.Value];
                    if (ObfuscatedValue.Length > ri.Value.Length)
                    {
                        Console.WriteLine($"{ri.Offset:X8} {ri.Name} {ri.Value} {ObfuscatedValue} LengthOverflow");
                    }
                    else
                    {
                        ObfuscatedValue += new String('_', ri.Value.Length - ObfuscatedValue.Length);
                        fs.Position = (Int64)(ri.Offset);
                        var Bytes = Encoding.ASCII.GetBytes(ObfuscatedValue);
                        bw.Write(Bytes);
                        Console.WriteLine($"{ri.Offset:X8} {ri.Name} {ri.Value} {ObfuscatedValue} Success");
                    }
                }
            }
        }

        public static List<RttiInfo> GetRttiInfoList(String Path)
        {
            using (var fs = new FileStream(Path, FileMode.Open, FileAccess.Read, FileShare.Read))
            using (var br = new BinaryReader(fs))
            {
                var Magic = br.ReadUInt32();
                if (Magic != 0x464C457F) //'\x7F' ELF
                {
                    throw new InvalidOperationException("InvalidIdentifier");
                }

                var Cls = (ElfClass)(br.ReadByte());
                if ((Cls != ElfClass._64) && (Cls != ElfClass._32))
                {
                    throw new InvalidOperationException("InvalidClass");
                }

                var Endian = br.ReadByte();
                if (Endian != 1) //Little-Endian
                {
                    throw new InvalidOperationException("InvalidEndian");
                }

                var HeaderVersion = br.ReadByte();
                if (HeaderVersion != 1)
                {
                    throw new InvalidOperationException("InvalidHeaderVersion");
                }

                UInt64 ProgramHeaderOffset;
                UInt64 SectionHeaderOffset;

                if (Cls == ElfClass._64)
                {
                    fs.Position = 16 + 16;
                    ProgramHeaderOffset = br.ReadUInt64();
                    SectionHeaderOffset = br.ReadUInt64();
                }
                else if (Cls == ElfClass._32)
                {
                    fs.Position = 16 + 12;
                    ProgramHeaderOffset = br.ReadUInt32();
                    SectionHeaderOffset = br.ReadUInt32();
                }
                else
                {
                    throw new InvalidOperationException();
                }

                if (Cls == ElfClass._64)
                {
                    fs.Position = 16 + 42;
                }
                else if (Cls == ElfClass._32)
                {
                    fs.Position = 16 + 30;
                }
                else
                {
                    throw new InvalidOperationException();
                }

                var SectionHeaderEntrySize = br.ReadUInt16();
                var SectionHeaderNum = br.ReadUInt16();
                var SectionHeaderStringTableIndex = br.ReadUInt16();

                var Sections = new Section[SectionHeaderNum];
                fs.Position = (Int64)(SectionHeaderOffset);

                for (int k = 0; k < SectionHeaderNum; k += 1)
                {
                    fs.Position = (Int64)(SectionHeaderOffset) + SectionHeaderEntrySize * k;

                    var s = new Section();
                    s.NameIndex = br.ReadUInt32();

                    if (Cls == ElfClass._64)
                    {
                        fs.Position += 12;
                        s.VirtualAddress = br.ReadUInt64();
                        s.Offset = br.ReadUInt64();
                        s.Size = br.ReadUInt64();
                        fs.Position += 16;
                        s.EntrySize = br.ReadUInt64();
                    }
                    else if (Cls == ElfClass._32)
                    {
                        fs.Position += 8;
                        s.VirtualAddress = br.ReadUInt32();
                        s.Offset = br.ReadUInt32();
                        s.Size = br.ReadUInt32();
                        fs.Position += 12;
                        s.EntrySize = br.ReadUInt32();
                    }
                    else
                    {
                        throw new InvalidOperationException();
                    }

                    Sections[k] = s;
                }

                for (int k = 0; k < SectionHeaderNum; k += 1)
                {
                    var s = Sections[k];
                    if (s.NameIndex > 0)
                    {
                        fs.Position = (Int64)(Sections[SectionHeaderStringTableIndex].Offset) + s.NameIndex;
                        s.Name = ReadAsciiString(br);
                    }
                }

                var SymbolTableSection = Sections.Where(s => s.Name == ".symtab").FirstOrDefault();
                if (SymbolTableSection == null)
                {
                    throw new InvalidOperationException("No .symtab");
                }
                var StringTableSection = Sections.Where(s => s.Name == ".strtab").FirstOrDefault();
                if (StringTableSection == null)
                {
                    throw new InvalidOperationException("No .strtab");
                }
                var ReadOnlyDataSection = Sections.Where(s => s.Name == ".rodata").FirstOrDefault();
                if (ReadOnlyDataSection == null)
                {
                    throw new InvalidOperationException("No .rodata");
                }

                var SymbolTableEntryCount = (int)(SymbolTableSection.Size / SymbolTableSection.EntrySize);
                var Symbols = new Symbol[SymbolTableEntryCount];
                for (int k = 0; k < SymbolTableEntryCount; k += 1)
                {
                    fs.Position = (Int64)(SymbolTableSection.Offset + SymbolTableSection.EntrySize * (UInt64)(k));

                    var s = new Symbol();
                    if (Cls == ElfClass._64)
                    {
                        s.NameIndex = br.ReadUInt32();
                        s.Info = br.ReadByte();
                        s.Other = br.ReadByte();
                        s.SectionIndex = br.ReadUInt16();
                        s.Value = br.ReadUInt64();
                        s.Size = br.ReadUInt64();
                    }
                    else if (Cls == ElfClass._32)
                    {
                        s.NameIndex = br.ReadUInt32();
                        s.Value = br.ReadUInt32();
                        s.Size = br.ReadUInt32();
                        s.Info = br.ReadByte();
                        s.Other = br.ReadByte();
                        s.SectionIndex = br.ReadUInt16();
                    }
                    else
                    {
                        throw new InvalidOperationException();
                    }
                    Symbols[k] = s;
                }

                for (int k = 0; k < SymbolTableEntryCount; k += 1)
                {
                    var s = Symbols[k];
                    if (s.NameIndex > 0)
                    {
                        fs.Position = (Int64)(StringTableSection.Offset) + s.NameIndex;
                        s.Name = ReadAsciiString(br);
                    }
                }

                var RttiInfoList = new List<RttiInfo> { };
                for (int k = 0; k < SymbolTableEntryCount; k += 1)
                {
                    var s = Symbols[k];
                    if ((s.Name != null) && (s.Type == SymbolType.Object) && (s.Name.StartsWith("_ZTS")))
                    {
                        if ((s.Value >= ReadOnlyDataSection.VirtualAddress) && (s.Value < ReadOnlyDataSection.VirtualAddress + ReadOnlyDataSection.Size))
                        {
                            RttiInfoList.Add(new RttiInfo { Name = s.Name, Offset = s.Value - ReadOnlyDataSection.VirtualAddress + ReadOnlyDataSection.Offset });
                        }
                    }
                }

                foreach (var ri in RttiInfoList)
                {
                    fs.Position = (Int64)(ri.Offset);
                    ri.Value = ReadAsciiString(br);
                }

                return RttiInfoList;
            }
        }

        private static String ReadAsciiString(BinaryReader br)
        {
            var l = new List<Byte>();
            while (true)
            {
                var b = br.ReadByte();
                if (b == 0) { break; }
                l.Add(b);
            }
            return Encoding.ASCII.GetString(l.ToArray());
        }

        private static String GetHash(String s)
        {
            var Bytes = Encoding.UTF8.GetBytes(s);
            var sha = new SHA256Managed();
            return String.Join("", sha.ComputeHash(Bytes).Take(4).Select(b => b.ToString("X2")));
        }
    }
}
