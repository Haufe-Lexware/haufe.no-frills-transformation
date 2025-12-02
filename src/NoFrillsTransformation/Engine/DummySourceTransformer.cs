using System.Data;
using NoFrillsTransformation.Interfaces;

namespace NoFrillsTransformation.Engine
{
    class DummySourceTransformer : ISourceTransformer
    {
        public IRecord CurrentRecord => throw new NotImplementedException();

        public void Dispose()
        {
        }

        public void FinishTransform()
        {
            throw new NotImplementedException();
        }

        public bool HasField(string fieldName)
        {
            throw new NotImplementedException();
        }

        public bool HasMoreRecords()
        {
            throw new NotImplementedException();
        }

        public bool HasResult()
        {
            throw new NotImplementedException();
        }

        public void NextRecord()
        {
            throw new NotImplementedException();
        }

        public void Transform(IContext context, IEvaluator eval)
        {
            throw new NotImplementedException();
        }
    }
}