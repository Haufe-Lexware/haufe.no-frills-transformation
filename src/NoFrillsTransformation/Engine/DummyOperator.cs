using NoFrillsTransformation.Interfaces;

namespace NoFrillsTransformation.Engine
{
    class DummyOperator : IOperator
    {
        public ExpressionType Type => throw new NotImplementedException();

        public string Name => throw new NotImplementedException();

        public int ParamCount => throw new NotImplementedException();

        public ParamType[] ParamTypes => throw new NotImplementedException();

        public ParamType ReturnType => throw new NotImplementedException();

        public void Configure(string? config)
        {
            throw new NotImplementedException();
        }

        public string Evaluate(IEvaluator eval, IExpression expression, IContext context)
        {
            throw new NotImplementedException();
        }
    }
}