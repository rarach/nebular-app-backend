using System.Collections.Generic;

namespace NebularApi
{
    public interface ILog
    {
        void Info(string message);
        void Warning(string message);
        void Error(string message);

        IEnumerable<string> Dumb();
    }
}
