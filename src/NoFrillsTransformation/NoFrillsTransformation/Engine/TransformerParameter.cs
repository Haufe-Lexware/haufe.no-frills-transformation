using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NoFrillsTransformation.Interfaces;

namespace NoFrillsTransformation
{
    class TransformerParameter : IParameter
    {
        public string Name { get; set; }
        public string FunctionString { get; set; }
 
        public IExpression Function { get; set; }
    }
}
