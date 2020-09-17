using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Microsoft.Win32;

// ReSharper disable CheckNamespace
// ReSharper disable IdentifierTypo
// ReSharper disable InconsistentNaming
// ReSharper disable StringLiteralTypo
// ReSharper disable CommentTypo

internal static class Program
{
    private const string HKCR = "HKEY_CURRENT_USER";
    private const int SCS_DOS_BINARY = 1;

    private static void SetupKeyDefaultValue(string keyName, string valueData, string valueName = null)
    {
        if (keyName == null)
            throw new ArgumentException("Key cannot be null.", nameof(keyName));
        if (valueData == null)
            throw new ArgumentException("Data cannot be null.", nameof(valueData));
        var value = Registry.GetValue(keyName, valueName, null);
        if (value as string != valueData)
            Registry.SetValue(keyName, valueName, valueData);
    }

    private static void Main(string[] args)
    {
        if (args.Length > 0 && (args[0] == "/u" || args[0] == "-u"))
        {
            var keytree = Registry.CurrentUser.OpenSubKey($@"Software\Classes\Applications", true);
            if (keytree != null && keytree.OpenSubKey($@"{AppDomain.CurrentDomain.FriendlyName}") != null)
                keytree.DeleteSubKeyTree($@"{AppDomain.CurrentDomain.FriendlyName}");
            SetupKeyDefaultValue($@"{HKCR}\Software\Classes\.com",      "comfile");
            SetupKeyDefaultValue($@"{HKCR}\Software\Classes\.exe",      "exefile");
            MessageBox.Show("DOSBox-X Launcher uninstalled.", "Message");
            return;
        }

        var launcher    = $@"{HKCR}\Software\Classes\Applications\{AppDomain.CurrentDomain.FriendlyName}";
        var launcherKey = launcher.Substring(launcher.IndexOf("Applications", StringComparison.OrdinalIgnoreCase));
        var key = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\DOSBox-X");
        string dosbox = "";
        if (key != null && key.GetValue("Location") is string) dosbox = key.GetValue("Location").ToString();
        if (key == null || !(key.GetValue("Location") is string) || !File.Exists(dosbox)) {
            dosbox = GetDosBoxXPath();
            if (dosbox != null) SetupKeyDefaultValue($@"{HKCR}\Software\DOSBox-X", dosbox, "Location");
        }
        SetupKeyDefaultValue($@"{launcher}\DefaultIcon",            @"""%1""");
        SetupKeyDefaultValue($@"{launcher}\shell\open\command",     $@"""{Assembly.GetExecutingAssembly().Location}"" ""%1"" %2 %3 %4 %5 %6 %7 %8 %9");
        SetupKeyDefaultValue($@"{launcher}\shell\runas",            "", "HasLUAShield");
        SetupKeyDefaultValue($@"{launcher}\shell\runas\command",    @"""%1"" %*");
        SetupKeyDefaultValue($@"{launcher}\shell\runas\command",    @"""%1"" %*", "IsolatedCommand");
        SetupKeyDefaultValue($@"{HKCR}\Software\Classes\.com",      launcherKey);
        SetupKeyDefaultValue($@"{HKCR}\Software\Classes\.exe",      launcherKey);

        if (args.Length <= 0) {
            MessageBox.Show("DOSBox-X Launcher installed.", "Message");
            return;
        }

        var fileName = args[0];
        var fileType = Path.GetExtension(fileName);
        args[0] = "";
        var fileArgs = string.Join(" ", args).Trim();

        Registry.SetValue($@"{HKCR}\Software\Classes\{fileType}", null, $"{fileType.Substring(1)}file");

        if (dosbox != null && IsMsDosExecutable(fileName))
        {
            fileArgs = $@"-fastlaunch -defaultdir ""{Path.GetDirectoryName(dosbox)}"" ""{fileName}""";
            fileName = dosbox;
        }

        Process.Start(fileName, fileArgs);

        Registry.SetValue($@"{HKCR}\Software\Classes\{fileType}", null, launcherKey);
    }

    private static string GetDosBoxXPath()
    {
        var path = Assembly.GetExecutingAssembly().Location;
        path = Path.GetDirectoryName(path);
        if (path == null) return null;
        path = Path.Combine(path, "dosbox-x.exe");
        if (File.Exists(path))
            return path;
        path = @"C:\DOSBox-X\dosbox-x.exe";
        if (File.Exists(path))
            return path;
        return null;
    }

    private static bool IsMsDosExecutable(string path)
    {
        uint type = 0;
        return NativeMethods.GetBinaryType(path, out type) && type == SCS_DOS_BINARY;
    }
}

internal static class NativeMethods
{
    [DllImport("Kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool GetBinaryType(string lpApplicationName, out uint lpBinaryType);
}