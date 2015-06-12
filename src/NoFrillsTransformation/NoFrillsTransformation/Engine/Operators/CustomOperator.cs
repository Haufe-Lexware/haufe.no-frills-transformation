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

        // Function
        public IExpression Expression { get; set; }
        // Switch/Case
        public IExpression[] Conditions { get; set; }
        public IExpression[] CaseFunctions { get; set; }
        public IExpression Otherwise { get; set; }

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
                // Plain function
                if (null != Expression)
                {
                    return eval.Evaluate(eval, Expression, context);
                }
                else // Switch/Case
                {
                    for (int i=0; i<Conditions.Length; ++i)
                    {
                        var cond = Conditions[i];
                        var func = CaseFunctions[i];
                        if (StringToBool(eval.Evaluate(eval, cond, context)))
                            return eval.Evaluate(eval, func, context);
                    }
                    return eval.Evaluate(eval, Otherwise, context);
                }
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

        internal static bool StringToBool(string s)
        {
            if (s.Equals("true"))
                return true;
            return false;
        }

        internal static string BoolToString(bool b)
        {
            return b ? "true" : "false";
        }
    }
}
