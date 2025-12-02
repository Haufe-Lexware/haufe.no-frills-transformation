using NoFrillsTransformation.Interfaces;

namespace NoFrillsTransformation.Engine
{
    class DummyTargetWriter : ITargetWriter
    {
        public int RecordsWritten => throw new NotImplementedException();

        public void Dispose()
        {
        }

        public void FinishWrite()
        {
            throw new NotImplementedException();
        }

        public void WriteRecord(string[] fieldValues)
        {
            throw new NotImplementedException();
        }
    }
}