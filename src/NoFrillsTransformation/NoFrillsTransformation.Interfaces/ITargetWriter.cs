using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NoFrillsTransformation.Interfaces
{
    public interface ITargetWriter : IDisposable
    {
        void WriteRecord(string[] fieldValues);
        int RecordsWritten { get; }
    }
}
