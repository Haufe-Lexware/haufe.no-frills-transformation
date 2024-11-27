using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NoFrillsTransformation.Interfaces
{
    public interface IEvaluator
    {
        string Evaluate(IEvaluator eval, IExpression expression, IContext context);
    }
}
