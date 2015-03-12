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
                // RowNum token?
                string tokenLow = token.ToLowerInvariant();
                switch (tokenLow)
                {
                    case "targetrownum":
                    case "sourcerownum":
                        // These two have no arguments.
                        SkipWhitespace(t, ref pos);
                        if (t[pos] != ')')
                            throw new ArgumentException("Token '" + token + "' does not accept any arguments (expected ')').");
                        return new Expression
                        {
                            Type = tokenLow.StartsWith("source") ? ExpressionType.SourceRowNum : ExpressionType.TargetRowNum
                        };

                    default:
                        // All other operations have two arguments. How convenient.
                        int commaPos = FindDelimiterPosition(t, pos, ',');
                        int endParenPos = FindDelimiterPosition(t, commaPos + 1, ')');

                        string firstArg = t.Substring(pos, commaPos - pos);
                        string secondArg = t.Substring(commaPos + 1, endParenPos - commaPos - 1);

                        bool isConcat = token.Equals("concat", StringComparison.InvariantCultureIgnoreCase);

                        var ex = new Expression
                        {
                            Type = isConcat ? ExpressionType.Concatenation : ExpressionType.Lookup,
                            Content = token,
                            FirstArgument = ParseExpression(firstArg),
                            SecondArgument = ParseExpression(secondArg)
                        };
                        if (!isConcat)
                        {
                            // Some sanity checks, not all lookup arguments are allowed
                            //if (ex.FirstArgument.Type != ExpressionType.FieldName)
                            //    throw new ArgumentException("First argument of lookup '" + token + "' must be a field name.");
                            if (ex.SecondArgument.Type != ExpressionType.FieldName)
                                throw new ArgumentException("Second argument of lookup '" + token + "' must be a lookup source field name ($<field>).");
                        }
                        return ex;
                }

            }
            catch (IndexOutOfRangeException)
            {
                throw new ArgumentException("Prematurely reached end of line at position " + pos);
            }
        }

        public static string EvaluateExpression(Expression expression, Context context)
        {
            switch (expression.Type)
            {
                // First things first, the simple case.
                case ExpressionType.StringLiteral:
                    return expression.Content;

                // This one's also fairly simple: Concatenation
                case ExpressionType.Concatenation:
                    return EvaluateExpression(expression.FirstArgument, context) +
                        EvaluateExpression(expression.SecondArgument, context);

                case ExpressionType.SourceRowNum:
                    return context.SourceRecordsRead.ToString();

                case ExpressionType.TargetRowNum:
                    return (context.TargetRecordsWritten + 1).ToString();

                // Evaluates to a source field
                case ExpressionType.FieldName:
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

                // The most intricate case: Lookup evaluation.
                case ExpressionType.Lookup:
                    // Second argument is of FieldName type (as enforced in the parser),
                    // first argument may be any expression (the evaluation of which will
                    // be used to look up the field in the lookup table).
                    var lookupKey = EvaluateExpression(expression.FirstArgument, context);
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
                    if (expression.SecondArgument.CachedFieldIndex < 0)
                    {
                        // And now let's cache the index of the lookup field.
                        try
                        {
                            expression.SecondArgument.CachedFieldIndex = 
                                expression.CachedLookupMap.GetFieldIndex(expression.SecondArgument.Content);
                        }
                        catch (Exception)
                        {
                            throw new ArgumentException("Runtime expression evaluation error: Unknown lookup field '" + 
                                expression.SecondArgument.Content + "' in lookup map '" + expression.Content + "'.");
                        }
                    }
                    try
                    {
                        // And now we can look stuff up. Which might also fluke.
                        return expression.CachedLookupMap.GetValue(lookupKey, expression.SecondArgument.CachedFieldIndex);
                    }
                    catch (Exception)
                    {
                        // Questionable whether this always must be an error. May also be a warning,
                        // should be configurable.
                        throw new ArgumentException("Runtime expression evaluation error: Cannot find key '" +
                            lookupKey + "' in lookup map '" + expression.Content + "'.");
                    }

                default:
                    throw new ArgumentException("Runtime expression error: Unknown expression type.");
            }
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

    enum ExpressionType
    {
        SourceRowNum,
        TargetRowNum,
        FieldName,
        StringLiteral,
        Concatenation,
        Lookup
    }

    class Expression
    {
        #region Properties
        public ExpressionType Type { get; set; }
        public string Content { get; set; }
        public Expression FirstArgument { get; set; }
        public Expression SecondArgument { get; set; }
        #endregion

        #region Caching
        private int _fieldIndex = -1;
        public int CachedFieldIndex
        {
            get { return _fieldIndex; }
            set { _fieldIndex = value; }
        }
        private LookupMap _lookupMap = null;
        public LookupMap CachedLookupMap
        {
            get { return _lookupMap; }
            set { _lookupMap = value; }
        }
        #endregion
    }
}
