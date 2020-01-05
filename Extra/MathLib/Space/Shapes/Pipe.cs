
namespace MathLib.Space.Shapes
{
    [MathValue]
    public readonly struct Pipe : IShape
    {
        public Circle Inner { get; }
        public Circle Outer { get; }

        public Area Area => Outer.Area - Inner.Area;

        public Pipe(Circle inner, Circle outer)
        {
            Inner = inner;
            Outer = outer;
        }
    }
}
