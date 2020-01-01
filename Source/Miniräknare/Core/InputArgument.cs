using System;

namespace Miniräknare
{
    public class InputArgument
    {
        public int? Id { get; }
        public BoxedValue Box { get; }

        public InputArgument(int? id, BoxedValue box)
        {
            Id = id;
            Box = box ?? throw new ArgumentNullException(nameof(box));
        }
    }
}
