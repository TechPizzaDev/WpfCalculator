
namespace MathLib.Space.Shapes
{
    [MathValue]
    public readonly struct Rectangle : IShape
    {
        public Length Width { get; }
        public Length Height { get; }

        public Area Area => Area.FromMeters((Width * Height).Meters);
        public Length Circumference => Width * 2 + Height * 2;

        public Rectangle(Length width, Length height)
        {
            Width = width;
            Height = height;
        }

        [MathValueConstructor]
        public static Rectangle FromSides(Length width, Length height) => 
            new Rectangle(width, height);
    }
}
