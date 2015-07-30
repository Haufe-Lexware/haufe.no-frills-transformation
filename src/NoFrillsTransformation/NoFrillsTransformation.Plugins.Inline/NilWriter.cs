using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NoFrillsTransformation.Interfaces;

namespace NoFrillsTransformation.Plugins.Inline
{
    class NilWriter : ITargetWriter
    {
        private int _recordsWritten = 0;

        public NilWriter()
        {
        }

        public void WriteRecord(string[] fieldValues)
        {
            _recordsWritten++;
        }

        public int RecordsWritten
        {
            get { return _recordsWritten; }
        }

        public void FinishWrite()
        {
        }

        public void Dispose()
        {
        }
    }
}
