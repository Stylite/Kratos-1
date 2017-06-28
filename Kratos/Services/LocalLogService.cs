﻿using System;
using System.IO;
using System.Threading.Tasks;
using Discord;

namespace Kratos.Services
{
    public class LocalLogService
    {
        public async Task LogAsync(LogMessage m)
        {
            Console.ForegroundColor = ConsoleColor.Gray;
            Console.Write(DateTime.UtcNow.ToString("hh:mm:ss"));
            switch (m.Severity)
            {
                case LogSeverity.Warning: Console.ForegroundColor = ConsoleColor.Yellow; break;
                case LogSeverity.Error: Console.ForegroundColor = ConsoleColor.Red; break;
                case LogSeverity.Critical: Console.ForegroundColor = ConsoleColor.DarkRed; break;
                case LogSeverity.Verbose: Console.ForegroundColor = ConsoleColor.Cyan; break;
                case LogSeverity.Info: Console.ForegroundColor = ConsoleColor.Green; break;
                case LogSeverity.Debug: Console.ForegroundColor = ConsoleColor.Magenta; break;
            }

            Console.CursorLeft = 9;
            Console.Write($"[{m.Severity}]");
            Console.ForegroundColor = ConsoleColor.Gray;
            Console.CursorLeft = 19;
            Console.Write($"{m.Source}:");
            Console.CursorLeft = 28;
            Console.WriteLine(m.Message);

            if (m.Exception != null)
            {
                var path = Program.GetLogPath(DateTime.UtcNow.ToString("dd-MM-yyyy HH-mm-ss") + " " + m.Exception.GetType().Name + ".txt");
                using (var stream = new FileStream(path, FileMode.CreateNew))
                {
                    using (var writer = new StreamWriter(stream))
                    {
                        await writer.WriteAsync(m.Exception.ToString());
                    }
                }

                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"^ {m.Exception.GetType().Name} occurred. See {path} for details.");
                Console.ForegroundColor = ConsoleColor.Gray;
            }
        }
    }
}
