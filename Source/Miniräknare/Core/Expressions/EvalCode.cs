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
        ErroredOperator,
        ErroredFunction,

        OperatorMissingLeftValue,
        OperatorMissingRightValue
    }
}
