using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NoFrillsTransformation.Interfaces;

namespace NoFrillsTransformation.Engine
{
    class TransformerSetting : ISetting
    {
        public string Name { get; set; }
        public string Setting { get; set; }
    }
}
