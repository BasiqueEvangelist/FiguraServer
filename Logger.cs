using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace FiguraServer
{
    public static class Logger
    {
        private static ConcurrentQueue<LoggerMessage> messages = new ConcurrentQueue<LoggerMessage>();
        private static Timer logTimer;

        static Logger()
        {
            logTimer = new Timer(async (obj) => { await DoLogs(); }, new AutoResetEvent(true), 15, 15);
        }

        public static async Task DoLogs()
        {
            while(messages.TryDequeue(out LoggerMessage msg))
            {
                await Console.Out.WriteLineAsync(msg.loggerMessage);
            }
        }

        public static void LogMessage(string str)
        {
            messages.Enqueue(new LoggerMessage()
            {
                level = 0,
                loggerMessage = str
            });
        }

        private struct LoggerMessage
        {
            public int level;
            public string loggerMessage;
        }
    }
}
