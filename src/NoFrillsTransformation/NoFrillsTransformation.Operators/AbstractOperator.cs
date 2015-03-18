using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NoFrillsTransformation.Interfaces;

namespace NoFrillsTransformation.Operators
{
    public abstract class AbstractOperator
    {
        public ExpressionType Type { get; protected set; }
        public string Name { get; protected set; }
        public int ParamCount { get; protected set; }
        public ParamType[] ParamTypes { get; protected set; }
        public ParamType ReturnType { get; protected set; }

        public void Configure(string config)
        {
            // None of the built-in operators need configuring.
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

        internal static long StringToInt(string s)
        {
            long result = 0L;
            if (Int64.TryParse(s, out result))
                return result;
            return 0L;
        }

        internal static string IntToString(long l)
        {
            return l.ToString();
        }
    }
}
