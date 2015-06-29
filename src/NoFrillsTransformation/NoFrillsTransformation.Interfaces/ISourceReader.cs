using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NoFrillsTransformation.Interfaces
{

    public interface ISourceReader : IDisposable
    {
        //void Initialize(string sourceFile, string config);
        bool IsEndOfStream { get; }
        void NextRecord();
        IRecord CurrentRecord { get; }
        int FieldCount { get; }
        string[] FieldNames { get; }
        int GetFieldIndex(string fieldName);
        IRecord Query(string key);
        //string GetFieldValue(string fieldName);
        //string GetFieldValue(int index);
    }
}
