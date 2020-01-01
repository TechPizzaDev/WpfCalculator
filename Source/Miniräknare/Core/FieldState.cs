using System;

namespace Miniräknare
{
    [Flags]
    public enum FieldState
    {
        Indeterminate = 0,
        Ok = 1 << 0,
        NestedError = 1 << 1,

        CyclicReferences = 1 << 2,
        SyntaxError = 1 << 3,
        UnknownWords = 1 << 4,
        UnknownFunctions = 1 << 5,
        NameDuplicates = 1 << 6,

        UnknownWordsNested = UnknownWords | NestedError,
        UnknownFunctionsNested = UnknownFunctions | NestedError,
        SyntaxErrorNested = SyntaxError | NestedError
    }
}
