using MathLib.Space;

namespace MathLib.Strengths
{
    /// <summary>
    /// Force in Newtons over area in millimeters.
    /// </summary>
    public readonly struct Shear
    {
        public Force Force { get; }
        public Area Area { get; }

        /// <summary>
        /// Denoted by Tao (τ).
        /// </summary>
        public double Value => Force / Area.MilliMeters;

        public Shear(Force force, Area area)
        {
            Force = force;
            Area = area;
        }

        public static implicit operator double(Shear drag) => drag.Value;

        #region From

        public static Shear FromForceOverArea(Force force, Area area) =>
            new Shear(force, area);

        public static Shear FromForceOverDrag(Force force, double drag) =>
            new Shear(force, Area.FromMilliMeters(force / drag));

        public static Shear FromDragArea(double drag, Area area) =>
            new Shear(Force.FromNewtons(drag * area.MilliMeters), area);

        #endregion

        public override string ToString()
        {
            return Value + " " + Drag.Unit;
        }
    }
}