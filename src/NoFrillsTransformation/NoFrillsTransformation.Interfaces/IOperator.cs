using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NoFrillsTransformation.Interfaces
{
    public interface IOperator : IEvaluator
    {
        ExpressionType Type { get; }
        string Name { get; }
        int ParamCount { get; }
        ParamType[] ParamTypes { get; }
        ParamType ReturnType { get; }

        void Configure(string config);
    }
}
