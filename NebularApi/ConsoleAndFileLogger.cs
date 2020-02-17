using System;
using System.IO;


namespace NebularApi
{
    public class ConsoleAndFileLogger : ILog
    {
        private readonly string _logFilePath;


        public ConsoleAndFileLogger(string filePath)
        {
            _logFilePath = filePath;
        }


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


        private void Log(string logType, string message)
        {
            message = $"{DateTime.Now.ToString("MMM-dd HH:mm:ss")} {logType} {message}";

            Console.WriteLine(message);

            using (StreamWriter sw = File.AppendText(_logFilePath))
            {
                sw.WriteLine(message);
            }
        }
    }
}
