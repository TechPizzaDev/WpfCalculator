using System;

namespace Miniräknare
{
    public class MathScriptActionBase
    {
        public InputArgument[] Arguments { get; }

        public MathScriptActionBase(InputArgument[] arguments)
        {
            Arguments = arguments ?? throw new ArgumentNullException(nameof(arguments));
        }

        public T GetArgument<T>(int? id = null)
        {
            return GetArgumentBox<T>(id).Value;
        }

        public BoxedValue<T> GetArgumentBox<T>(int? id = null)
        {
            for (int i = 0; i < Arguments.Length; i++)
            {
                var argument = Arguments[i];
                if (!(argument.Box is BoxedValue<T> boxed))
                    continue;

                if (id.HasValue)
                {
                    if (argument.Id.HasValue && argument.Id == id)
                        return boxed;
                }
                else
                {
                    return boxed;
                }
            }
            throw new MissingInputException(typeof(T), id);
        }
    }
}
