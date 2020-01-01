using System.Linq;
using MathLib.Space;

namespace MathLib.Strengths
{
    /// <summary>
    /// Force in Newtons over area in millimeters.
    /// </summary>
    public readonly struct Drag
    {
        public const string Unit = "N/mm^2";

        public Force Force { get; }
        public Area Area { get; }

        /// <summary>
        /// Denoted by Rho (ϱ).
        /// <para>
        /// Shearing drag is denoted by Tao (τ).
        /// </para>
        /// </summary>
        public double Value => Force / Area.MilliMeters;

        public Drag(Force force, Area area)
        {
            Force = force;
            Area = area;
        }

        public static implicit operator double(Drag drag) => drag.Value;

        #region From

        public static Drag FromForceOverArea(Force force, Area area) =>
            new Drag(force, area);

        public static Drag FromForceOverDrag(Force force, double drag) =>
            new Drag(force, Area.FromMilliMeters(force / drag));

        public static Drag FromDragArea(double drag, Area area) =>
            new Drag(Force.FromNewtons(drag * area.MilliMeters), area);

        #endregion
        
        public static Force GetLowestForce(params Drag[] values)
        {
            return new Force(values.Select((x) => x.Force.Newtons).Min());
        }

        public override string ToString()
        {
            return Value + " " + Unit;
        }
    }
}