using System;

namespace WpfCalculator
{
    public class MissingInputException : Exception
    {
        public Type Type { get; }
        public int? Id { get; }

        public MissingInputException()
        {
        }

        public MissingInputException(string message) : base(message)
        {
        }

        public MissingInputException(string message, Exception inner) : base(message, inner)
        {
        }

        public MissingInputException(Type type, int? id) : this(GetMessage(type, id))
        {
        }

        private static string GetMessage(Type type, int? id)
        {
            if (id.HasValue)
                return $"No input with identifier '{id.Value}'.";
            return $"No input of type '{type.Name}'.";
        }
    }
}
