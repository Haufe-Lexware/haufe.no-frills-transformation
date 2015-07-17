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

        private class SPair
        {
            public string A;
            public string B;
        }
        
        public override void Configure(string config)
        {
            if (string.IsNullOrEmpty(config))
                return;
            var replaces = new List<SPair>();
            string[] configs = config.Split(new char[] { ',' });
            for (int i=0; i<configs.Length; ++i)
            {
                string[] pair = configs[i].Split(new char[] { '=' });
                if (pair.Length != 2)
                    throw new ArgumentException("Invalid configuration for ConvertUmlaut: '" + configs[i] + "'. Expected <char>=<string>, e.g ö=oe.");
                string c = pair[0].Trim();
                string r = pair[1].Trim();

                if (c.Length != 1)
                    throw new ArgumentException("Invalid configuration for ConvertUmlaut: '" + configs[i] + "'. Char to replace must be a single character.");
                replaces.Add(new SPair { A = c, B = r });
            }

            _umlauts = new string[replaces.Count, 2];
            for (int i = 0; i < replaces.Count; ++i)
            {
                _umlauts[i, 0] = replaces[i].A;
                _umlauts[i, 1] = replaces[i].B;
            }
        }
    }
}
