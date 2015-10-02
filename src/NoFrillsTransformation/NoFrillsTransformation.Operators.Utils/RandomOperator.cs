using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using NoFrillsTransformation.Interfaces;

namespace NoFrillsTransformation.Operators.Utils
{
    [Export(typeof(NoFrillsTransformation.Interfaces.IOperator))]
    public class RandomOperator : AbstractUtilsOperator, IOperator
    {
        public RandomOperator()
        {
            Type = ExpressionType.Custom;
            Name = "random";
            ParamCount = 1;
            ParamTypes = new ParamType[] { ParamType.Int };
            ReturnType = ParamType.Int;
        }

        private Random _random;

        public string Evaluate(IEvaluator eval, IExpression expression, IContext context)
        {
            int max = Int32.Parse(eval.Evaluate(eval, expression.Arguments[0], context));

            if (max <= 0)
                throw new ArgumentException("RandomOperator: Argument must be larger than 0: " + max + ".");

            if (null == _random)
            {
                _random = new Random();
            }
            return _random.Next(max).ToString();
        }
    }
}
