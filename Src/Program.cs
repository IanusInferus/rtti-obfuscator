using System;
using System.Collections.Generic;
using System.Linq;

namespace RttiObfuscator
{
    public class Program
    {
        public static int Main(String[] args)
        {
            if (System.Diagnostics.Debugger.IsAttached)
            {
                return MainInner(args);
            }
            else
            {
                try
                {
                    return MainInner(args);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.ToString());
                    return 1;
                }
            }
        }

        public static int MainInner(String[] args)
        {
            var argv = args.Where(arg => !arg.StartsWith("--") && !arg.Contains("=")).ToArray();
            var options = args.Where(arg => arg.StartsWith("--")).Select(arg => arg.Substring(2).Split(new Char[] { ':' }, 2)).GroupBy(p => p[0]).ToDictionary(g => g.Key, g => g.Last().Skip(1).SingleOrDefault(), StringComparer.OrdinalIgnoreCase);
            var optionLists = args.Where(arg => arg.StartsWith("--")).Select(arg => arg.Substring(2).Split(new Char[] { ':' }, 2)).GroupBy(p => p[0]).ToDictionary(g => g.Key, g => g.Select(Value => Value.Skip(1).SingleOrDefault()).ToList(), StringComparer.OrdinalIgnoreCase);

            var Help = options.ContainsKey("help");
            if (Help)
            {
                DisplayInfo();
                return 0;
            }

            if (argv.Length >= 2)
            {
                var FileType = argv[0];
                var Command = argv[1];
                if (FileType != "elf")
                {
                    Console.WriteLine($"FileTypeNotSupported: {FileType}");
                    return 1;
                }
                if (Command == "list")
                {
                    if (argv.Length >= 3)
                    {
                        var InputFilePath = argv[2];
                        ElfRttiObfuscator.List(InputFilePath);
                        return 0;
                    }
                }
                else if (Command == "obfuscate")
                {
                    if (argv.Length >= 4)
                    {
                        var InputFilePath = argv[2];
                        var OutputFilePath = argv[3];
                        ElfRttiObfuscator.Obfuscate(InputFilePath, OutputFilePath);
                        return 0;
                    }
                }
                else
                {
                    Console.WriteLine($"CommandNotSupported: {Command}");
                    return 1;
                }
            }

            DisplayInfo();
            return 1;
        }

        public static void DisplayInfo()
        {
            Console.WriteLine(@"RttiObfuscator");
            Console.WriteLine(@"ELF RTTI obfuscator");
            Console.WriteLine(@"");
            Console.WriteLine(@"Usage:");
            Console.WriteLine(@"RttiObfuscator elf list <InputFilePath>");
            Console.WriteLine(@"RttiObfuscator elf obfuscate <InputFilePath> <OutputFilePath>");
            Console.WriteLine(@"");
            Console.WriteLine(@"Sample:");
            Console.WriteLine(@"RttiObfuscator elf list libxxx.so");
            Console.WriteLine(@"RttiObfuscator elf obfuscate libxxx.so libxxx2.so");
            Console.WriteLine(@"");
            Console.WriteLine(@"Notice:");
            Console.WriteLine(@"clang will inline string literal into 16-byte bulk copies and intermediate value assignments with -O2, which will embed trailing chars into opcode (when char count is not a multiple of 16). This can be worked around by marking __attribute__((noinline)) on std::type_info::name() .");
        }
    }
}
