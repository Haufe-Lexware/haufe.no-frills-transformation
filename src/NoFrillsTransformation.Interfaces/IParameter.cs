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
        string? KeyString { get; set; }
        IExpression? Key { get; set; }
        string? ValueString { get; set; }
        IExpression? Value { get; set; }
    }
}
