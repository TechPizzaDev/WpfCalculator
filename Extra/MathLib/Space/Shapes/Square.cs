
namespace MathLib.Space.Shapes
{
    [MathValue]
    public readonly struct Square : IShape
    {
        public Length SideLength { get; }
        public Area Area => Area.FromMeters((SideLength * SideLength).Meters);
        public Length Circumference => SideLength * 4;

        public Square(Length sideLength) => SideLength = sideLength;

        [MathValueConstructor]
        public static Square FromSideLength(Length sideLength) => new Square(sideLength);
    }
}
