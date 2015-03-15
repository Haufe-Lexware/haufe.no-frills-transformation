using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NoFrillsTransformation.Interfaces
{
    public interface ILookupMap
    {
        string Id { get; }
        string KeyField { get;}

        string GetValue(string key, string fieldName);
        string GetValue(string key, int fieldIndex);
        int GetFieldIndex(string fieldName);
    }
}
