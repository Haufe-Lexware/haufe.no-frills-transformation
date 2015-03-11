using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NoFrillsTransformation.Interfaces;

namespace NoFrillsTranformation.Plugins.Csv
{
    class CsvWriterPlugin : ITargetWriter
    {
        public void WriteRecord(string[] fieldValues)
        {
            _recordsWritten++;
        }

        private int _recordsWritten = 0;
        public int RecordsWritten
        {
            get
            {
                return _recordsWritten;
            }
        }
    }
}
