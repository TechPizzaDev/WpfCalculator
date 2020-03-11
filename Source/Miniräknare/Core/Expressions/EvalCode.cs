namespace Miniräknare.Expressions
{
    public enum EvalCode
    {
        Undefined,
        Ok,
        
        UnresolvedReference,
        UnresolvedOperator,
        UnresolvedFunction,

        ErroredReference,
        ErroredOperator,

        ErroredFunction,
        InvalidArguments,
        InvalidArgumentCount,

        OperatorMissingLeftValue,
        OperatorMissingRightValue
    }
}
