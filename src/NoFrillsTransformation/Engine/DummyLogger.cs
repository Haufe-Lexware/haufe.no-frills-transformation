using NoFrillsTransformation.Interfaces;

namespace NoFrillsTransformation.Engine
{
    class DummyLogger : ILogger
    {
        public void Dispose()
        {
        }

        public void Error(string log)
        {
            throw new NotImplementedException();
        }

        public void Info(string log)
        {
            throw new NotImplementedException();
        }

        public void Warning(string log)
        {
            throw new NotImplementedException();
        }
    }
}
