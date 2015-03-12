using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NoFrillsTransformation.Engine
{
    class TargetFieldDef
    {
        public string FieldName { get; set; }
        public int FieldSize { get; set; }
        public Expression Expression { get; set; }
    }
}
