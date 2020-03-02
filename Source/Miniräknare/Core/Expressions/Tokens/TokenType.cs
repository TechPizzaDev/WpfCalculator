namespace Miniräknare.Expressions.Tokens
{
    public enum TokenType
    {
        Unknown,
        DecimalDigit,
        DecimalNumber,
        DecimalSeparator,
        Name,
        Operator,
        ListStart,
        ListEnd,
        ListSeparator,
        WhiteSpace,
        Space,

        Function,
        List,
    }
}
