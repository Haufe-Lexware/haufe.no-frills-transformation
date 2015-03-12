using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NoFrillsTransformation.Engine
{
    // OK, I admit it. I am too lazy to actually learn how to use a proper
    // parser generator. Instead I'm rolling my own. So sue me.
    class ExpressionParser
    {
        public static Expression ParseExpression(string expressionString)
        {
            if (string.IsNullOrWhiteSpace(expressionString))
                throw new ArgumentException("The string '" + expressionString + "' is not a valid expression.");
            string t = expressionString.Trim();

            int pos = 0;
            char firstChar = t[pos];
            if (firstChar == '$')
                return CreateFieldExpression(t);
            if (firstChar == '"')
                return CreateLiteralExpression(t);
            if (!IsTokenChar(firstChar))
                throw new ArgumentException("Illegal first character in expression: '" + firstChar + "'. Expected letter, digit, _, -, \" or $.");

            string token = ReadNextToken(t, ref pos);
            try
            {
                SkipWhitespace(t, ref pos);
                char nextChar = t[pos];
                if (nextChar != '(')
                    throw new ArgumentException("After token '" + token + "' expected '(', got '" + nextChar + "'.");
                SkipWhitespace(t, ref pos);
                pos++;

                string tokenLow = token.ToLowerInvariant();
                var expressionType = GetExpressionType(tokenLow);

                Expression ex = null;

                // Most operators have 2 parameters; check the others:
                int paramCount = 2;
                switch (expressionType)
                {
                    case ExpressionType.TargetRowNum:
                    case ExpressionType.SourceRowNum:
                        paramCount = 0;
                        break;

                    case ExpressionType.LowerCase:
                    case ExpressionType.UpperCase:
                        paramCount = 1;
                        break;

                    case ExpressionType.If:
                        paramCount = 3;
                        break;
                }

                if (0 == paramCount)
                {
                    SkipWhitespace(t, ref pos);
                    if (t[pos] != ')')
                        throw new ArgumentException("Token '" + token + "' does not accept any arguments (expected ')').");
                    ex = new Expression
                    {
                        Content = token,
                        Type = expressionType
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
                        Type = expressionType,
                        Content = token,
                        Arguments = args.Select(a => ParseExpression(a)).ToArray()
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

        private static ExpressionType GetExpressionType(string tokenLow)
        {
            switch (tokenLow)
            {
                case "sourcerownum": return ExpressionType.SourceRowNum;
                case "targetrownum": return ExpressionType.TargetRowNum;
                case "concat": return ExpressionType.Concat;
                case "or": return ExpressionType.Or;
                case "and": return ExpressionType.And;
                case "equals": return ExpressionType.Equals;
                case "equalsignorecase": return ExpressionType.EqualsIgnoreCase;
                case "lowercase": return ExpressionType.LowerCase;
                case "uppercase": return ExpressionType.UpperCase;
                case "contains": return ExpressionType.Contains;
                case "containsignorecase": return ExpressionType.ContainsIgnoreCase;
                case "startswith": return ExpressionType.StartsWith;
                case "endswith": return ExpressionType.EndsWith;
                case "if": return ExpressionType.If;
                default: return ExpressionType.Lookup;
            }
        }

        private static void SanityCheckExpression(Expression ex)
        {
            switch (ex.Type)
            {
                case ExpressionType.SourceRowNum:
                case ExpressionType.TargetRowNum:
                case ExpressionType.StringLiteral:
                    // Nothing to check here, always sane
                    break;

                case ExpressionType.Concat:
                case ExpressionType.Contains:
                case ExpressionType.ContainsIgnoreCase:
                case ExpressionType.Equals:
                case ExpressionType.EqualsIgnoreCase:
                case ExpressionType.StartsWith:
                case ExpressionType.EndsWith:
                    SanityCheckBothArgumentsAreStrings(ex);
                    break;

                case ExpressionType.If:
                    SanityCheckIfArguments(ex);
                    break;

                case ExpressionType.Lookup:
                    SanityCheckBothArgumentsAreStrings(ex);
                    SanityCheckSecondArgumentIsField(ex);
                    break;

                case ExpressionType.LowerCase:
                case ExpressionType.UpperCase:
                    SanityCheckFirstArgumentIsString(ex);
                    break;

                case ExpressionType.Or:
                case ExpressionType.And:
                    SanityCheckBothArgumentsAreBool(ex);
                    break;
            }
        }

        internal static bool IsStringExpression(Expression ex)
        {
            switch (ex.Type)
            {
                case ExpressionType.StringLiteral:
                case ExpressionType.FieldName:
                case ExpressionType.SourceRowNum:
                case ExpressionType.TargetRowNum:
                case ExpressionType.Concat:
                case ExpressionType.Lookup:
                case ExpressionType.LowerCase:
                case ExpressionType.UpperCase:
                case ExpressionType.If:
                    return true;
            }
            return false;
        }

        internal static bool IsBoolExpression(Expression ex)
        {
            switch (ex.Type)
            {
                case ExpressionType.Contains:
                case ExpressionType.ContainsIgnoreCase:
                case ExpressionType.Equals:
                case ExpressionType.EqualsIgnoreCase:
                case ExpressionType.StartsWith:
                case ExpressionType.EndsWith:
                case ExpressionType.Or:
                case ExpressionType.And:
                    return true;
            }
            return false;
        }

        private static void SanityCheckBothArgumentsAreStrings(Expression ex)
        {
            if (IsStringExpression(ex.Arguments[0])
                && IsStringExpression(ex.Arguments[1]))
                return;
            throw new ArgumentException("Operator '" + ex.Content + "' needs two string arguments (one or both are bool).");
        }

        private static void SanityCheckFirstArgumentIsString(Expression ex)
        {
            if (IsStringExpression(ex.Arguments[0]))
                return;
            throw new ArgumentException("Operator '" + ex.Content + "' needs a string argument (is bool).");
        }

        private static void SanityCheckBothArgumentsAreBool(Expression ex)
        {
            if (IsBoolExpression(ex.Arguments[0])
                && IsBoolExpression(ex.Arguments[1]))
                return;
            throw new ArgumentException("Operator '" + ex.Content + "' needs two boolean arguments (one or both are strings).");
        }

        private static void SanityCheckSecondArgumentIsField(Expression ex)
        {
            if (ex.Arguments[1].Type == ExpressionType.FieldName)
                return;
            throw new ArgumentException("Second argument of operator '" + ex.Content + "' must be a field name ($<field>).");
        }

        private static void SanityCheckIfArguments(Expression ex)
        {
            if (IsBoolExpression(ex.Arguments[0])
                && IsStringExpression(ex.Arguments[1])
                && IsStringExpression(ex.Arguments[2]))
                return;
            throw new ArgumentException("Argument mismatch: If operator arguments must be boolean, string, string (condition, if true, else).");
        }

        public static string EvaluateExpression(Expression expression, Context context)
        {
            switch (expression.Type)
            {
                // First things first, the simple case.
                case ExpressionType.StringLiteral:
                    return expression.Content;

                // This one's also fairly simple: Concatenation
                case ExpressionType.Concat:
                    return HandleConcat(expression, context);

                case ExpressionType.SourceRowNum:
                    return HandleSourceRowNum(context);

                case ExpressionType.TargetRowNum:
                    return HandleTargetRowNum(context);

                // Evaluates to a source field
                case ExpressionType.FieldName:
                    return HandleFieldName(expression, context);

                // The most intricate case: Lookup evaluation.
                case ExpressionType.Lookup:
                    return HandleLookup(expression, context);

                case ExpressionType.LowerCase:
                    return HandleLowerCase(expression, context);

                case ExpressionType.UpperCase:
                    return HandleUpperCase(expression, context);

                case ExpressionType.Equals:
                    return HandleEquals(expression, context, false);

                case ExpressionType.EqualsIgnoreCase:
                    return HandleEquals(expression, context, true);

                case ExpressionType.Contains:
                    return HandleContains(expression, context, false);

                case ExpressionType.ContainsIgnoreCase:
                    return HandleContains(expression, context, true);

                case ExpressionType.StartsWith:
                    return HandleStartsWith(expression, context);

                case ExpressionType.EndsWith:
                    return HandleEndsWith(expression, context);

                case ExpressionType.If:
                    return HandleIf(expression, context);

                case ExpressionType.And:
                    return HandleAnd(expression, context);

                case ExpressionType.Or:
                    return HandleOr(expression, context);

                default:
                    throw new ArgumentException("Runtime expression error: Unknown expression type.");
            }
        }

        private static string HandleLookup(Expression expression, Context context)
        {
            // Second argument is of FieldName type (as enforced in the parser),
            // first argument may be any expression (the evaluation of which will
            // be used to look up the field in the lookup table).
            var lookupKey = EvaluateExpression(expression.Arguments[0], context);
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
            var secondArg = expression.Arguments[1];
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

        private static string HandleFieldName(Expression expression, Context context)
        {
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
            return context.SourceReader.CurrentRecord[expression.CachedFieldIndex];
        }

        private static string HandleTargetRowNum(Context context)
        {
            return (context.TargetRecordsWritten + 1).ToString();
        }

        private static string HandleSourceRowNum(Context context)
        {
            return context.SourceRecordsRead.ToString();
        }

        private static string HandleConcat(Expression expression, Context context)
        {
            return EvaluateExpression(expression.Arguments[0], context) +
                                    EvaluateExpression(expression.Arguments[1], context);
        }

        private static string HandleLowerCase(Expression expression, Context context)
        {
            return EvaluateExpression(expression.Arguments[0], context).ToLowerInvariant();
        }

        private static string HandleUpperCase(Expression expression, Context context)
        {
            return EvaluateExpression(expression.Arguments[0], context).ToUpperInvariant();
        }

        private static string HandleEquals(Expression expression, Context context, bool ignoreCase)
        {
            var compType = ignoreCase ? StringComparison.InvariantCultureIgnoreCase : StringComparison.InvariantCulture;
            return BoolToString(EvaluateExpression(expression.Arguments[0], context).
                        Equals(EvaluateExpression(expression.Arguments[1], context),
                        compType));
        }

        private static string HandleContains(Expression expression, Context context, bool ignoreCase)
        {
            string a = EvaluateExpression(expression.Arguments[0], context);
            string b = EvaluateExpression(expression.Arguments[1], context);
            if (ignoreCase)
            {
                a = a.ToUpper();
                b = b.ToUpper();
            }
            return BoolToString(a.Contains(b));
        }

        private static string HandleStartsWith(Expression expression, Context context)
        {
            string a = EvaluateExpression(expression.Arguments[0], context);
            string b = EvaluateExpression(expression.Arguments[1], context);
            return BoolToString(a.StartsWith(b, StringComparison.InvariantCultureIgnoreCase));
        }

        private static string HandleEndsWith(Expression expression, Context context)
        {
            string a = EvaluateExpression(expression.Arguments[0], context);
            string b = EvaluateExpression(expression.Arguments[1], context);
            return BoolToString(a.EndsWith(b, StringComparison.InvariantCultureIgnoreCase));
        }

        private static string HandleIf(Expression expression, Context context)
        {
            bool cond = StringToBool(EvaluateExpression(expression.Arguments[0], context));
            if (cond)
                return EvaluateExpression(expression.Arguments[1], context);
            return EvaluateExpression(expression.Arguments[2], context);
        }

        private static string HandleAnd(Expression expression, Context context)
        {
            bool a = StringToBool(EvaluateExpression(expression.Arguments[0], context));
            bool b = StringToBool(EvaluateExpression(expression.Arguments[1], context));
            return BoolToString(a && b);
        }
        private static string HandleOr(Expression expression, Context context)
        {
            bool a = StringToBool(EvaluateExpression(expression.Arguments[0], context));
            bool b = StringToBool(EvaluateExpression(expression.Arguments[1], context));
            return BoolToString(a || b);
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
                    throw new ArgumentException("End of expression found when looking for delimiter '" + delimiter + "' in expression '" + expression + "'.");
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
                Type = ExpressionType.FieldName,
                Content = expression.Substring(1)
            };
        }

        private static Expression CreateLiteralExpression(string expression)
        {
            if (expression[expression.Length - 1] != '"')
                throw new ArgumentException("Ill-terminated literal expression: '" + expression + "'.");
            return new Expression
            {
                Type = ExpressionType.StringLiteral,
                Content = expression.Substring(1, expression.Length - 2)
            };
        }
    }
}
