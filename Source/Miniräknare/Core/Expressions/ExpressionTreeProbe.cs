using System;
using System.Collections.Generic;
using Miniräknare.Expressions.Tokens;

namespace Miniräknare.Expressions
{
    public class ExpressionTreeProbe
    {
        public delegate void ProbeReferenceDelegate(
            EvalCode code, ValueToken reference);
        
        public delegate void ProbeOperatorDelegate(
            EvalCode code, ValueToken op, Token left, Token right);
        
        public delegate void ProbeFunctionDelegate(
            EvalCode code, FunctionToken function);

        public event ProbeReferenceDelegate ProbeReference;
        public event ProbeOperatorDelegate ProbeOperator;
        public event ProbeFunctionDelegate ProbeFunction;

        public ExpressionTreeProbe()
        {
        }

        public void Probe(ExpressionTree tree)
        {

        }
    }
}
