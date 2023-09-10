#if DEBUG
using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.IO;
using System.Reflection;

namespace Ass_Pain.Helpers
{
    /// <summary>
    /// Console "extensions"
    /// </summary>
    public static class MyConsole
    {
        /// <summary>
        /// Writes message with file and line in which it's called
        /// </summary>
        /// <param name="message">message to write</param>
        /// <param name="file">supplied by compiler</param>
        /// <param name="line">supplied by compiler</param>
        public static void WriteLine(string message, [CallerFilePath] string? file = null, [CallerLineNumber] int line = 0)
        {
            char separatorChar = (char)typeof(Path).GetTypeInfo().GetDeclaredField("DirectorySeparatorChar").GetValue(null);
            if (file == null) return;
            file = file.Replace(@"\\", $"{separatorChar}").Replace('\\', separatorChar);
            file = Path.GetFileName(file);
            Console.WriteLine("[{0}][{1}]: {2}", file, line, message);
        }

        /// <summary>
        /// Writes exception with file and line in which it's called alongside exception stack trace
        /// </summary>
        /// <param name="ex">exception to write</param>
        /// <param name="file">supplied by compiler</param>
        /// <param name="line">supplied by compiler</param>
        public static void WriteLine(Exception ex, [CallerFilePath] string? file = null, [CallerLineNumber] int line = 0)
        {
            StackTrace st = new StackTrace(ex, true);
            // Get the top stack frame
            StackFrame frame = st.GetFrame(st.FrameCount -1 );
            // Get the line number from the stack frame
            int exLine = frame.GetFileLineNumber();
            string message = $"[tryCatch line: {exLine}]: {ex}";
            char separatorChar = (char)typeof(Path).GetTypeInfo().GetDeclaredField("DirectorySeparatorChar").GetValue(null);
            if (file == null) return;
            file = file.Replace(@"\\", $"{separatorChar}").Replace('\\', separatorChar);
            file = Path.GetFileName(file);
            Console.WriteLine("[{0}][{1}]: {2}", file, line, message);
        }
    }
}
#endif