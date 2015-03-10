using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NoFrillsTransformation.Interfaces
{
    interface ISourceReader
    {
        bool CanReadFormat(string fileSuffix);
        void Initialize(string sourceFile, string config);
        bool HasMoreData { get; }
        bool ReadNextRecord();
        IRecord CurrentRecord { get; }
        int FieldCount { get; }
        string[] FieldNames { get; }
        int GetFieldIndex(string fieldName);
        string GetFieldValue(string fieldName);
        string GetFieldValue(int index);
    }
}
