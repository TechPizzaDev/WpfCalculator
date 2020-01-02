using System;
using MathLib.Space;

namespace MathLib.Strengths
{
    [MathValue]
    public readonly struct Tension
    {
        public Length Length { get; }

        public Length Extension { get; }

        public Length StretchedLength => Length + Extension;

        public double Value => Extension / Length;

        public Tension(Length length, Length extension)
        {
            Length = length;
            Extension = extension;
        }

        public static Tension FromLengthExtension(
            Length original, Length extension)
        {
            return new Tension(original, extension);
        }

        public static Tension FromLengths(Length original, Length streched)
        {
            var extension = streched - original;
            return Tension.FromLengthExtension(original, extension);
        }

        public override string ToString()
        {
            return Math.Round(Value * 100, 1) + "%, " +
                StretchedLength + "/" + Length;
        }
    }
}
