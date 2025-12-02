using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NoFrillsTransformation.Interfaces
{
    public interface ILogger : IDisposable
    {
        void Info(string log);
        void Warning(string log);
        void Error(string log);
    }
}
