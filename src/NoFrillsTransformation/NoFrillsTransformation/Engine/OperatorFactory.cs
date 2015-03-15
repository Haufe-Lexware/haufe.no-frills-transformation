using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Hosting;
using System.Linq;
using System.Text;
using NoFrillsTransformation.Interfaces;

namespace NoFrillsTransformation
{
    class OperatorFactory
    {
        public OperatorFactory(CompositionContainer container)
        {
            container.ComposeParts(this);
        }

#pragma warning disable 0649
        // Inject Operators via MEF
        [ImportMany(typeof(NoFrillsTransformation.Interfaces.IOperator))]
        private IOperator[] _injectedOperators;
#pragma warning restore 0649

        public IOperator[] Operators
        {
            get
            {
                return _injectedOperators;
            }
        }
    }
}
