using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NoFrillsTransformation.Interfaces;

namespace NoFrillsTransformation.Engine.Operators
{
    public class CustomOperator : IOperator
    {
        public CustomOperator(string name)
        {
            _name = name;
        }

        private string _name;

        public int ParamCount { get; set; }
        public string Name { get { return _name; } }
        public ExpressionType Type { get { return ExpressionType.Custom; } }
        public string[] ParamNames { get; set; }
        public ParamType[] ParamTypes { get; set; }
        public ParamType ReturnType { get; set; }

        public IExpression Expression { get; set; }

        public string Evaluate(IEvaluator eval, IExpression expression, IContext context)
        {
            var parameterContext = new ParameterContext();
            for (int i=0; i<ParamNames.Length; ++i)
            {
                parameterContext.Parameters[ParamNames[i]] = eval.Evaluate(eval, expression.Arguments[i], context);
            }
            context.ParameterStack.Push(parameterContext);

            try
            {
                return eval.Evaluate(eval, Expression, context);
            }
            finally
            {
                context.ParameterStack.Pop();
            }
        }

        public void Configure(string config)
        {
            // No config for this operator.
        }
    }
}
