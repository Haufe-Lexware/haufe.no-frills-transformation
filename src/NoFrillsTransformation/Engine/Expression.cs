using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NoFrillsTransformation.Interfaces;

namespace NoFrillsTransformation.Engine
{
    class Expression : IExpression
    {
        #region Properties
        //public ExpressionType Type { get; set; }
        public IOperator Operator { get; set; } = new DummyOperator();
        public string Content { get; set; } = string.Empty;
        //public Expression FirstArgument { get; set; }
        //public Expression SecondArgument { get; set; }
        public IExpression[] Arguments { get; set; } = [];
        #endregion

        #region Caching
        private int _fieldIndex = -1;
        public int CachedFieldIndex
        {
            get { return _fieldIndex; }
            set { _fieldIndex = value; }
        }
        private ILookupMap? _lookupMap = null;
        public ILookupMap? CachedLookupMap
        {
            get { return _lookupMap; }
            set { _lookupMap = value; }
        }
        #endregion
    }
}
