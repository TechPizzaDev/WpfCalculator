
namespace Miniräknare
{
    public static class FieldStateExtensions
    {
        public static FieldState AsNested(this FieldState state)
        {
            return state switch
            {
                FieldState.SyntaxError => FieldState.SyntaxErrorNested,
                FieldState.UnknownWords => FieldState.UnknownWordsNested,
                FieldState.UnknownFunctions => FieldState.UnknownFunctionsNested,
                _ => state,
            };
        }
    }
}
