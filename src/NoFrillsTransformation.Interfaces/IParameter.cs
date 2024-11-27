using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NoFrillsTransformation.Interfaces
{
    public interface IParameter
    {
        string Name { get; set; }
        string FunctionString { get; set; }
        IExpression Function { get; set; }
    }
}
