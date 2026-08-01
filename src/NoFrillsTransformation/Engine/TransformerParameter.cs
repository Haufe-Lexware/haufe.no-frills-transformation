using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NoFrillsTransformation.Interfaces;

namespace NoFrillsTransformation
{
    class TransformerParameter : IParameter
    {
        public TransformerParameter(string name, string functionString, IExpression function, 
            string? keyString = null, IExpression? key = null, 
            string? valueString = null, IExpression? value = null)
        {
            Name = name;
            FunctionString = functionString;
            Function = function;
            KeyString = keyString;
            Key = key;
            ValueString = valueString;
            Value = value;
        }

        public string Name { get; set; }
        
        public string FunctionString { get; set; }
 
        public IExpression Function { get; set; }

        public string? KeyString { get; set; }

        public IExpression? Key { get; set; }

        public string? ValueString { get; set; }

        public IExpression? Value { get; set; }
    }
}
