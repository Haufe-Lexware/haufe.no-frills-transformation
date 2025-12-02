using System;
using System.Collections.Generic;
using System.Composition;
using System.Composition.Hosting;
using System.Linq;
using System.Text;
using NoFrillsTransformation.Interfaces;

namespace NoFrillsTransformation
{
    class OperatorFactory
    {
        public OperatorFactory(CompositionHost container)
        {
            container.SatisfyImports(this);
        }

        // Inject Operators via MEF
        [ImportMany]
        private IEnumerable<IOperator>? InjectedOperators { get; set; }

        private IOperator[]? _injectedOperators;
        public IOperator[] Operators
        {
            get
            {
                if (null == InjectedOperators)
                {
                    throw new InvalidOperationException("No operators found/MEF could not inject operators.");
                }
                if (null == _injectedOperators)
                {
                    _injectedOperators = InjectedOperators.ToArray();
                }
                return _injectedOperators;
            }
        }
    }
}
