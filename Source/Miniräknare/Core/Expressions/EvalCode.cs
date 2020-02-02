namespace Miniräknare.Expressions
{
    public enum EvalCode
    {
        Undefined,
        Ok,
        Empty,
        
        UnresolvedReference,
        UnresolvedOperator,
        UnresolvedFunction,

        ErroredReference,
        CyclicReferences,
        
        ErroredOperator,

        ErroredFunction,
        InvalidArguments,
        InvalidArgumentCount,

        OperatorMissingLeftValue,
        OperatorMissingRightValue
    }
}
