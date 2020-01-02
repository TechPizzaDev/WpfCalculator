using System;

namespace MathLib.Space.Shapes
{
    [MathValue]
    public readonly struct Circle
    {
        public Length Radius { get; }
        public Length Diameter => Radius * 2;
        public Length Circumference => Math.PI * Diameter;
        public Area Area => Area.FromMeters(Math.PI * Radius * Radius);

        public Circle(Length radius) => Radius = radius;

        public static Circle FromRadius(Length radius) =>
            new Circle(radius);

        public static Circle FromDiameter(Length diameter) =>
            new Circle(diameter / 2);

        public static Circle FromArea(Area area) =>
            new Circle(Math.Sqrt(area.Meters / Math.PI));
    }
}
