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
        InvalidOperatorCall,

        OperatorMissingLeftValue,
        OperatorMissingRightValue
    }
}
