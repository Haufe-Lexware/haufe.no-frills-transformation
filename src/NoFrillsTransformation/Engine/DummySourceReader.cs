using System.Data;
using NoFrillsTransformation.Interfaces;

namespace NoFrillsTransformation.Engine
{
    class DummySourceReader : ISourceReader
    {
        public bool IsEndOfStream => throw new NotImplementedException();

        public IRecord CurrentRecord => throw new NotImplementedException();

        public int FieldCount => throw new NotImplementedException();

        public string[] FieldNames => throw new NotImplementedException();

        public void Dispose() { }

        public int GetFieldIndex(string fieldName)
        {
            throw new NotImplementedException();
        }

        public void NextRecord()
        {
            throw new NotImplementedException();
        }

        public IRecord Query(string key)
        {
            throw new NotImplementedException();
        }
    }
}