using System;

namespace Miniräknare
{
    public class ResourceUri
    {
        public const char PathSeparator = '/';

        public string Path { get; }
        public ReadOnlyMemory<string> SegmentsMemory { get; }
        public ReadOnlySpan<string> Segments => SegmentsMemory.Span;

        public ResourceUri(string path)
        {
            Path = path;
            SegmentsMemory = Path.Split(PathSeparator, StringSplitOptions.RemoveEmptyEntries);

            if (SegmentsMemory.Length == 0)
                throw new ArgumentNullException("Value contains only empy segments.", nameof(path));
        }

        public override string ToString()
        {
            return Path;
        }
    }
}
