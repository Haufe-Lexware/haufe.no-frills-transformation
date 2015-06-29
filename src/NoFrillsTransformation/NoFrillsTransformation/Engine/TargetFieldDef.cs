using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NoFrillsTransformation.Interfaces;

namespace NoFrillsTransformation.Engine
{
    class TargetFieldDef : IFieldDefinition
    {
        public string FieldName { get; set; }
        public int FieldSize { get; set; }
        public string Config { get; set; }
        public Expression Expression { get; set; }
    }
}
