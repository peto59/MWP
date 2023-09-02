#if DEBUG
using System;
using System.Diagnostics;
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

        public static void WriteLine(Exception ex, [CallerFilePath] string file = null, [CallerLineNumber] int line = 0)
        {
#if DEBUG
            StackTrace st = new StackTrace(ex, true);
            // Get the top stack frame
            StackFrame frame = st.GetFrame(st.FrameCount -1 );
            // Get the line number from the stack frame
            int exLine = frame.GetFileLineNumber();
            WriteLine($"[tryCatch line: {exLine}]: {ex}", file, line);
#endif
        }
    }
}
#endif