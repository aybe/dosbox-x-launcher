using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using Microsoft.Win32;

// ReSharper disable CheckNamespace
// ReSharper disable IdentifierTypo
// ReSharper disable StringLiteralTypo
internal static class Program
{
    // ReSharper disable once RedundantAssignment
    private static void Main(string[] args)
    {
        Console.WriteLine("Passed arguments: " + args.Length);

        foreach (var arg in args)
        {
            Console.WriteLine($"\t{arg}");
        }

        Console.WriteLine("Press a key to continue");
        Console.ReadKey();

//#if DEBUG
//        // ReSharper disable once RedundantAssignment
//        // ReSharper disable StringLiteralTypo
//        args = new[] {@"C:\temp\dosbox\QBASIC.EXE"};
//        args = new[] {@"C:\temp\dosbox\ski32.exe"};
//        // ReSharper restore StringLiteralTypo
//#endif

        if (args.Length != 0)
        {
            var fileName  = args[0];
            var arguments = string.Join(" ", args.Skip(1));

            if (!Start(fileName))
            {
                Console.WriteLine("Starting program directly");
                Process.Start(fileName, arguments);
            }
        }

        Console.WriteLine("Press a key to continue");
        Console.ReadKey();
    }

    private static bool Start(string fileName)
    {
        var key = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\DOSBox-X");

        if (key == null)
            return false;

        if (!(key.GetValue("Location") is string dosbox))
            return false;

        if (!NativeMethods.GetBinaryType(fileName, out var fileType))
            return false;

        if (fileType != NativeConstants.SCS_DOS_BINARY)
            return false;

        var arguments = $@"-fastlaunch -defaultdir ""{Path.GetDirectoryName(dosbox)}"" ""{fileName}""";

        var info = new ProcessStartInfo(dosbox, arguments)
        {
            UseShellExecute = true
        };

        Console.WriteLine("Starting program with DOSBox");
        Process.Start(info);

        return true;
    }
}

internal static class NativeMethods
{
    [DllImport("Kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool GetBinaryType(string lpApplicationName, out uint lpBinaryType);
}

[SuppressMessage("ReSharper", "InconsistentNaming")]
[SuppressMessage("ReSharper", "UnusedMember.Global")]
internal static class NativeConstants
{
    public const int ERROR_BAD_EXE_FORMAT = 193;
    public const int SCS_32BIT_BINARY     = 0;
    public const int SCS_DOS_BINARY       = 1;
    public const int SCS_WOW_BINARY       = 2;
    public const int SCS_PIF_BINARY       = 3;
    public const int SCS_POSIX_BINARY     = 4;
    public const int SCS_OS216_BINARY     = 5;
    public const int SCS_64BIT_BINARY     = 6;
}