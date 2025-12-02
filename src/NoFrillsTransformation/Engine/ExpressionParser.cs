using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NoFrillsTransformation.Engine.Operators;
using NoFrillsTransformation.Interfaces;

namespace NoFrillsTransformation.Engine
{
    // OK, I admit it. I am too lazy to actually learn how to use a proper
    // parser generator. Instead I'm rolling my own. So sue me.
    class ExpressionParser : IEvaluator
    {
        public static Expression ParseExpression(string? expressionString, IContext context)
        {
            if (string.IsNullOrWhiteSpace(expressionString))
                throw new ArgumentException("The string '" + expressionString + "' is not a valid expression.");
            string t = expressionString.Trim();

            t = ReplaceInfixOperators(t);

            int pos = 0;
            char firstChar = t[pos];
            if (firstChar == '$')
                return CreateFieldExpression(t);
            if (firstChar == '%')
                return CreateParameterExpression(t);
            if (firstChar == '"')
                return CreateLiteralExpression(t);
            if (Char.IsDigit(firstChar)
                || '-' == firstChar)
                return CreateIntLiteralExpression(t);
            if (!IsTokenChar(firstChar))
                throw new ArgumentException("Illegal first character in expression: '" + firstChar + "'. Expected letter, digit, _, -, \" or $.");

            string token = ReadNextToken(t, ref pos);
            string tokenLow = token.ToLowerInvariant();
            if (!context.HasOperator(tokenLow))
                throw new ArgumentException("Unknown operator '" + token + "'. Typo, or missing lookup definition?");
            IOperator op = context.GetOperator(tokenLow);
            try
            {
                SkipWhitespace(t, ref pos);
                char nextChar = t[pos];
                if (nextChar != '(')
                    throw new ArgumentException("After token '" + token + "' expected '(', got '" + nextChar + "'.");
                SkipWhitespace(t, ref pos);
                pos++;

                Expression? ex = null;

                // Most operators have 2 parameters; check the others:
                int paramCount = op.ParamCount;

                if (0 == paramCount)
                {
                    SkipWhitespace(t, ref pos);
                    if (t[pos] != ')')
                        throw new ArgumentException("Token '" + token + "' does not accept any arguments (expected ')').");
                    ex = new Expression
                    {
                        Content = token,
                        Operator = op
                    };
                }
                else
                {
                    // One or more arguments
                    string[] args = new string[paramCount];
                    int tempPos = pos - 1;
                    for (int i = 0; i < paramCount; ++i)
                    {
                        char delim = (i == paramCount - 1) ? ')' : ',';
                        int delimPos = FindDelimiterPosition(t, tempPos + 1, delim);
                        args[i] = t.Substring(tempPos + 1, delimPos - tempPos - 1);
                        tempPos = delimPos;
                    }

                    ex = new Expression
                    {
                        Operator = op,
                        Content = token,
                        Arguments = args.Select(a => ParseExpression(a, context)).ToArray()
                    };
                }

                SanityCheckExpression(ex);
                return ex;
            }
            catch (IndexOutOfRangeException)
            {
                throw new ArgumentException("Prematurely reached end of line at position " + pos);
            }
        }

        private class InfixOperator
        {
            public char Infix;
            public string Operator = string.Empty;
        }

        // In top-down order of evaluation.
        private static InfixOperator[] _infixOperators = new InfixOperator[]
            {
                new InfixOperator { Infix = '=', Operator = "Equals" },
                new InfixOperator { Infix = '+', Operator = "Concat" },
            };

        private static string ReplaceInfixOperators(string exp)
        {
            return ReplaceInfixOperatorsInternal(exp, _infixOperators);
        }

        private static string ReplaceInfixOperatorsInternal(string exp, InfixOperator[] operators)
        {
            int infixIndex = -1;
            int infixPosition = -1;
            for (int i = 0; i < operators.Length; ++i)
            {
                infixPosition = FindDelimiterPosition(exp, 0, operators[i].Infix, false);
                if (infixPosition >= 0)
                {
                    infixIndex = i;
                    break;
                }
            }

            if (infixIndex < 0)
                return exp;

            return string.Format("{0}({1}, {2})", operators[infixIndex].Operator, exp.Substring(0, infixPosition), exp.Substring(infixPosition + 1));
        }

        private static void SanityCheckExpression(Expression ex)
        {
            for (int i = 0; i < ex.Operator.ParamCount; ++i)
            {
                if (null == ex.Operator.ParamTypes)
                    throw new ArgumentException("Operator '" + ex.Content + "' has no parameter types defined.");
                var paramType = ex.Operator.ParamTypes[i];
                var returnType = ex.Arguments[i].Operator.ReturnType;

                if (paramType == ParamType.Any)
                    continue;
                if (ex.Arguments[i].Operator.Type == ExpressionType.Parameter)
                    continue; // Don't check parameters; that'd turn out nasty.

                if (paramType != returnType)
                    throw new ArgumentException("Parameter type mismatch in operator '" + ex.Content + "', argument " + (i + 1)
                        + ". Expected " + TypeToString(paramType) + ", got " + TypeToString(returnType) + ".");
            }

            // Extra sausage for the Lookup operator
            if (ex.Operator.Type == ExpressionType.Lookup)
            {
                SanityCheckSecondArgumentIsField(ex);
            }
            else if (ex.Operator.Type == ExpressionType.HasKey)
            {
                SanityCheckFirstArgumentIsStringLiteral(ex);
            }
        }

        internal static string TypeToString(ParamType pt)
        {
            switch (pt)
            {
                case ParamType.String: return "String";
                case ParamType.Bool: return "Bool";
                case ParamType.Int: return "Int";
                case ParamType.Any: return "Any type";
            }
            return "<unknown type>";
        }

        internal static ParamType StringToType(string? pt)
        {
            if (string.IsNullOrEmpty(pt))
                return ParamType.Undefined;
            switch (pt.ToLowerInvariant())
            {
                case "string": return ParamType.String;
                case "bool": return ParamType.Bool;
                case "int": return ParamType.Int;
                case "any": return ParamType.Any;
            }
            return ParamType.Undefined;
        }

        private static void SanityCheckSecondArgumentIsField(IExpression ex)
        {
            if (ex.Arguments[1].Operator.Type == ExpressionType.FieldName)
                return;
            throw new ArgumentException("Second argument of operator '" + ex.Content + "' must be a field name ($<field>).");
        }

        private static void SanityCheckFirstArgumentIsStringLiteral(IExpression ex)
        {
            if (ex.Arguments[0].Operator.Type == ExpressionType.StringLiteral)
                return;
            throw new ArgumentException("First argument of operator '" + ex.Content + "' must be a string literal (the lookup map name)");
        }

        public string Evaluate(IEvaluator eval, IExpression expression, IContext context)
        {
            switch (expression.Operator.Type)
            {
                // First things first, the simple case.
                case ExpressionType.StringLiteral:
                case ExpressionType.IntLiteral:
                    return ReplaceEscapeChars(context.ReplaceParameters(expression.Content));

                // Evaluates to a source field
                case ExpressionType.FieldName:
                    return HandleFieldName((Expression)expression, context);

                // The most intricate case: Lookup evaluation.
                case ExpressionType.Lookup:
                    return HandleLookup(eval, (Expression)expression, context);

                default:
                    // All other operators are called using their own implementations
                    return expression.Operator.Evaluate(eval, expression, context);
            }
        }

        private static string ReplaceEscapeChars(string s)
        {
            if (!s.Contains('\\'))
                return s;
            var sb = new StringBuilder();
            int len = s.Length;
            bool prevWasBackslash = false;
            for (int i = 0; i < len; ++i)
            {
                char c = s[i];
                if (prevWasBackslash)
                {
                    switch (c)
                    {
                        case 't': sb.Append('\t'); break;
                        case 'n': sb.Append('\n'); break;
                        case 'r': sb.Append('\r'); break;
                        default: sb.Append(c); break;
                    }
                    prevWasBackslash = false;
                }
                else
                {
                    switch (c)
                    {
                        case '\\': prevWasBackslash = true; break;
                        default: sb.Append(c); break;
                    }
                }
            }
            if (prevWasBackslash)
                throw new ArgumentException("String '" + s + "' ends with a backslash, did you mean \\\\?");

            return sb.ToString();
        }

        private static string HandleLookup(IEvaluator eval, Expression expression, IContext context)
        {
            // Second argument is of FieldName type (as enforced in the parser),
            // first argument may be any expression (the evaluation of which will
            // be used to look up the field in the lookup table).
            var lookupKey = eval.Evaluate(eval, expression.Arguments[0], context);
            if (null == expression.CachedLookupMap)
            {
                try
                {
                    // The content of the second argument contains the name of the desired
                    // lookup map; let's cache it.
                    expression.CachedLookupMap = context.GetLookupMap(expression.Content);
                }
                catch (Exception)
                {
                    throw new ArgumentException("Runtime expresion evaluation error: Unknown lookup map '" + expression.Content + "'.");
                }
            }
            var secondArg = (Expression)expression.Arguments[1];
            if (secondArg.CachedFieldIndex < 0)
            {
                // And now let's cache the index of the lookup field.
                try
                {
                    secondArg.CachedFieldIndex =
                        expression.CachedLookupMap.GetFieldIndex(secondArg.Content);
                }
                catch (Exception)
                {
                    throw new ArgumentException("Runtime expression evaluation error: Unknown lookup field '" +
                        secondArg.Content + "' in lookup map '" + expression.Content + "'.");
                }
            }
            try
            {
                // And now we can look stuff up. Which might also fluke.
                return expression.CachedLookupMap.GetValue(lookupKey, secondArg.CachedFieldIndex);
            }
            catch (Exception)
            {
                // Questionable whether this always must be an error. May also be a warning,
                // should be configurable.
                throw new ArgumentException("Runtime expression evaluation error: Cannot find key '" +
                    lookupKey + "' in lookup map '" + expression.Content + "'.");
            }
        }

        private static string HandleFieldName(Expression expression, IContext context)
        {
            if (!context.InTransform
                && null != context.Transformer)
            {
                if (context.Transformer.HasField(expression.Content))
                {
                    return context.Transformer.CurrentRecord[expression.Content];
                }
            }
            if (expression.CachedFieldIndex < 0)
            {
                try
                {
                    expression.CachedFieldIndex = context.SourceReader.GetFieldIndex(expression.Content);
                }
                catch (Exception)
                {
                    throw new ArgumentException("Runtime expression evaluation error: Unknown field name '" + expression.Content + "'.");
                }
            }
            if (expression.CachedFieldIndex < 0)
                throw new ArgumentException("Runtime expression evaluation error: Unknown field name '" + expression.Content + "'.");

            return context.SourceReader.CurrentRecord[expression.CachedFieldIndex];
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

        private static string ReadNextToken(string expression, ref int pos)
        {
            var sb = new StringBuilder();
            while (IsTokenChar(expression[pos]))
            {
                sb.Append(expression[pos]);
                pos++;
            }
            return sb.ToString();
        }

        private static void SkipWhitespace(string expression, ref int pos)
        {
            while (char.IsWhiteSpace(expression[pos]))
                pos++;
        }

        private static bool IsTokenChar(char c)
        {
            return char.IsLetterOrDigit(c)
                || c == '-'
                || c == '_';
        }

        private static int FindDelimiterPosition(string expression, int pos, char delimiter)
        {
            return FindDelimiterPosition(expression, pos, delimiter, true);
        }

        private static int FindDelimiterPosition(string expression, int pos, char delimiter, bool throwException)
        {
            int delimPos = pos;
            bool inQuote = false;
            int parCount = 0;
            // This would be the last to read; in case we have parenthesis inside other ones,
            // this is important (nested expressions)
            if (delimiter == ')')
                parCount = 1;

            char c = expression[delimPos];
            int len = expression.Length;
            bool skipNext = false;
            while (c != delimiter || inQuote || parCount > 0)
            {
                if (delimPos >= len)
                {
                    if (throwException)
                        throw new ArgumentException("End of expression found when looking for delimiter '" + delimiter + "' in expression '" + expression + "'.");
                    return -1;
                }
                if (!skipNext)
                {
                    c = expression[delimPos];
                    if (c == '\\') // escape char
                        skipNext = true;
                    else if (c == '(' && !inQuote)
                        parCount++;
                    else if (c == ')' && !inQuote)
                        parCount--;
                    else if (c == '"')
                    {
                        if (inQuote)
                            inQuote = false;
                        else
                            inQuote = true;
                    }
                }
                else
                {
                    skipNext = false;
                }
                delimPos++;
            }
            delimPos--;
            return delimPos;
        }

        private static Expression CreateFieldExpression(string expression)
        {
            return new Expression
            {
                Operator = new FieldNameOperator(),
                Content = expression.Substring(1)
            };
        }

        private static Expression CreateParameterExpression(string expression)
        {
            return new Expression
            {
                Operator = new ParameterOperator(),
                Content = expression.Substring(1)
            };
        }

        private static Expression CreateLiteralExpression(string expression)
        {
            if (expression[expression.Length - 1] != '"')
                throw new ArgumentException("Ill-terminated literal expression: '" + expression + "'.");
            return new Expression
            {
                Operator = new StringLiteralOperator(),
                Content = expression.Substring(1, expression.Length - 2)
            };
        }

        private static Expression CreateIntLiteralExpression(string expression)
        {
            if (expression[0] == '-'
                && expression.Length == 1)
                throw new ArgumentException("Illegal integer literal expression: " + expression);
            for (int i = 1; i < expression.Length; ++i)
            {
                if (!Char.IsDigit(expression[i]))
                    throw new ArgumentException("Illegal integer literal expression: " + expression);
            }
            long l = Int64.Parse(expression);
            return new Expression
            {
                Operator = new IntLiteralOperator(),
                Content = l.ToString()
            };
        }
    }
}
