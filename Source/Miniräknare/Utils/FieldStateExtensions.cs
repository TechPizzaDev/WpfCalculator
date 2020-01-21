
namespace Miniräknare
{
    public static class FieldStateExtensions
    {
        public static FieldState AsNested(this FieldState state)
        {
            return state switch
            {
                FieldState.SyntaxError => FieldState.SyntaxErrorNested,
                FieldState.UnknownWord => FieldState.UnknownWordNested,
                FieldState.UnknownFunction => FieldState.UnknownFunctionNested,
                _ => state,
            };
        }
    }
}
