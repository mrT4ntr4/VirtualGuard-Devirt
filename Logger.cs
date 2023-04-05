using System;

namespace VirtualGuardDevirt
{
    internal class Logger
    {
        public enum TypeMessage
        {
            Error,
            Done,
            Debug,
            Info,
        }

        public static void Log(string message, TypeMessage type)
        {
            switch (type)
            {
                case TypeMessage.Debug:
                    Console.ForegroundColor = ConsoleColor.Blue;
                    Console.WriteLine($"{DateTime.Now:hh:mm:ss} [Debug] {message}");
                    Console.ResetColor();
                    break;
                case TypeMessage.Done:
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine($"{DateTime.Now:hh:mm:ss} [+] : {message}");
                    Console.ResetColor();
                    break;
                case TypeMessage.Error:
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"{DateTime.Now:hh:mm:ss} [!] : {message}");
                    Console.ResetColor();
                    break;
                case TypeMessage.Info:
                    Console.ForegroundColor = ConsoleColor.Cyan;
                    Console.WriteLine($"{DateTime.Now:hh:mm:ss} [~] : {message}");
                    Console.ResetColor();
                    break;
            }
        }

    }
}
