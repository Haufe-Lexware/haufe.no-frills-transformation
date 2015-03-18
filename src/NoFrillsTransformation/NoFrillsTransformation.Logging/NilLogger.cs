using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NoFrillsTransformation.Interfaces;

namespace NoFrillsTransformation.Logging
{
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1063:ImplementIDisposableCorrectly")]
    public class NilLogger : ILogger
    {
        public void Info(string info) { }
        public void Warning(string warn) { }
        public void Error(string error) { }

        // Does not have any managed or unmanaged resources.
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1063:ImplementIDisposableCorrectly")]
        public void Dispose() { }
    }
}
