using System.Linq;
using MathLib.Space;
using MathLib.Systems;

namespace MathLib.Strengths
{
    [DerivedUnit]
    public readonly struct SurfaceTension
    {
        public const string Unit = "N/mm^2";

        public Force Force { get; }
        public Area Area { get; }

        /// <summary>
        /// Denoted by Rho (ϱ).
        /// <para>
        /// Shearing tension is denoted by Tao (τ).
        /// </para>
        /// </summary>
        public double Value => Force / Area.MilliMeters;

        public SurfaceTension(Force force, Area area)
        {
            Force = force;
            Area = area;
        }

        public static implicit operator double(SurfaceTension drag) => drag.Value;

        #region From

        public static SurfaceTension FromForceOverArea(Force force, Area area) =>
            new SurfaceTension(force, area);

        public static SurfaceTension FromForceOverDrag(Force force, double drag) =>
            new SurfaceTension(force, Area.FromMilliMeters(force / drag));

        public static SurfaceTension FromDragArea(double drag, Area area) =>
            new SurfaceTension(Force.FromNewtons(drag * area.MilliMeters), area);

        #endregion
        
        public static Force GetLowestForce(params SurfaceTension[] values)
        {
            return new Force(values.Select((x) => x.Force.Newtons).Min());
        }

        public override string ToString()
        {
            return Value + " " + Unit;
        }
    }
}