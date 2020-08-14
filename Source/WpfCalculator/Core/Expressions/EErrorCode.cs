namespace WpfCalculator.Expressions
{
    public enum EErrorCode
    {
        Undefined,
        Empty,

        CyclicReference,
        SyntaxError,

        InvalidArguments,
        InvalidArgumentCount,

        OperatorMissingLeftValue,
        OperatorMissingRightValue,

        UnknownReference,
        UnknownOperator,
        UnknownFunction,

        ErroredReference,
        ErroredOperator,
        ErroredFunction
    }
}
