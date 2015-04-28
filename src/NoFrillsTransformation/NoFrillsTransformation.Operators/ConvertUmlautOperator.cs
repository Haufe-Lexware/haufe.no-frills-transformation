using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using NoFrillsTransformation.Interfaces;

namespace NoFrillsTransformation.Operators
{
    [Export(typeof(NoFrillsTransformation.Interfaces.IOperator))]
    public class ConvertUmlautOperator : AbstractOperator, IOperator
    {
        public ConvertUmlautOperator()
        {
            Type = ExpressionType.ConvertUmlaut;
            Name = "convertumlaut";
            ParamCount = 1;
            ParamTypes = new ParamType[] { ParamType.String };
            ReturnType = ParamType.String;
        }

        private static string[,] _umlauts = { 
                                            { "ä", "ae" },
                                            { "ö", "oe" },
                                            { "ü", "ue" },
                                            { "ß", "ss" },
                                            { "é", "e" },
                                            { "è", "e" },
                                            { "ì", "i" },
                                            { "í", "i" },
                                            { "å", "a" }
                                            };

        public string Evaluate(IEvaluator eval, IExpression expression, IContext context)
        {
            string a = eval.Evaluate(eval, expression.Arguments[0], context);
            for (int i = 0; i < _umlauts.GetLength(0); ++i)
                a = a.Replace(_umlauts[i, 0], _umlauts[i, 1]);
            return a;
        }
    }
}
