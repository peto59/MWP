#if DEBUG
using System;
using System.Runtime.CompilerServices;
using System.IO;
using System.Reflection;

namespace Ass_Pain.Helpers
{
    public static class MyConsole
    {
        public static void WriteLine(string message, [CallerFilePath] string file = null, [CallerLineNumber] int line = 0)
        {
            char seperatorChar = (char)typeof(Path).GetTypeInfo().GetDeclaredField("DirectorySeparatorChar").GetValue(null);
            file = file.Replace("\\\\", $"{seperatorChar}").Replace('\\', seperatorChar);
            file = Path.GetFileName(file);
            Console.WriteLine("[{0}][{1}]: {2}", file, line, message);
        }
    }
}
#endif