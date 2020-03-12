using System;
using System.Collections.Generic;


namespace NebularApi
{
    public class ConsoleAndMemoryLogger : ILog
    {
        private const int Capacity = 1000;
        private readonly List<string> _logs = new List<string>();   //TODO: there must be some .NET native queue or something


        public void Error(string message)
        {
            Log("ERROR", message);
        }

        public void Info(string message)
        {
            Log("INFO", message);
        }

        public void Warning(string message)
        {
            Log("WARN", message);
        }

        public IEnumerable<string> Dumb()
        {
            return _logs;
        }


        private void Log(string logType, string message)
        {
            message = $"{DateTime.Now.ToString("MMM-dd HH:mm:ss")} {logType} {message}";
            Console.WriteLine(message);

            _logs.Add(message);
            if (_logs.Count >= Capacity)
            {
                _logs.RemoveAt(0);
            }
        }
    }
}
