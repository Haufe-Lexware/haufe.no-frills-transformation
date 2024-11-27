using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NoFrillsTransformation.Interfaces;

namespace NoFrillsTransformation.Engine
{
    class TargetFieldDef : IFieldDefinition
    {
        public TargetFieldDef(string fieldName, int fieldSize, string? config, Expression expression)
        {
            FieldName = fieldName;
            FieldSize = fieldSize;
            Config = config;
            Expression = expression;
        }

        public string FieldName { get; set; }
        public int FieldSize { get; set; }
        public string? Config { get; set; }
        public Expression Expression { get; set; }
    }
}
