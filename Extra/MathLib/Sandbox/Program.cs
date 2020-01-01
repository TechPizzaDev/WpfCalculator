using System;
using MathLib.Angles;
using MathLib.Space;
using MathLib.Space.Shapes;
using MathLib.Strengths;
using MathLib.Systems;

namespace Sandbox
{
    partial class Program
    {
        static void Main(string[] args)
        {
            #region Spänning and stuffs

            #region Uppgift 1
            {
                var area = Area.FromMilliMeters(200);
                var force = Force.FromNewtons(30000);
                var drag = Drag.FromForceOverArea(force, area);

                Console.WriteLine("Uppgift 1 | Drag: " + drag);
            }
            #endregion

            #region Uppgift 1*
            {
                var area = Area.FromMilliMeters(100);
                var force = Force.FromNewtons(20000);
                var drag = Drag.FromForceOverArea(force, area);

                Console.WriteLine("Uppgift 1* | Drag: " + drag);
            }
            #endregion

            #region Uppgift 2
            {
                var square = Square.FromSideLength(Length.FromMilli(20));
                var force = Force.FromNewtons(50000);
                var drag = Drag.FromForceOverArea(force, square.Area);

                Console.WriteLine("Uppgift 2 | Drag: " + drag);
            }
            #endregion

            #region Uppgift 2*
            {
                var circle = Circle.FromDiameter(Length.FromMilli(1000));
                var force = Force.FromNewtons(5 * Math.Pow(10, 6));
                var drag = Drag.FromForceOverArea(force, circle.Area);

                Console.WriteLine("Uppgift 2* | Drag: " + Math.Round(drag.Value, 1) + " " + Drag.Unit);
            }
            #endregion

            #region Uppgift 3
            {
                var rect = Rectangle.FromSides(
                    Length.FromMilli(50), Length.FromMilli(100));

                var drag = Drag.FromDragArea(9, rect.Area);

                Console.WriteLine("Uppgift 3 | Force: " + drag.Force);
            }
            #endregion

            #region Uppgift 3*
            {
                var circle = Circle.FromDiameter(Length.FromMilli(100));
                var drag = Drag.FromDragArea(100, circle.Area);

                Console.WriteLine("Uppgift 3 | Force: " +
                    Math.Round(drag.Force * 2 / 1000, 1) + " " + SIPrefix.Kilo.Symbol + "N");
            }
            #endregion

            #region Uppgift 4
            {
                var force = Force.FromNewtons(40000);
                var drag = Drag.FromForceOverDrag(force, drag: 120);
                var circle = Circle.FromArea(drag.Area);
                Console.WriteLine("Uppgift 4 | Diameter: " + Math.Round(circle.Diameter * 1000, 1) + " mm");
            }
            #endregion

            // hoppa över 5

            #region Uppgift 5*
            {
                var originalLength = Length.FromMilli(5000);
                var strechedLength = Length.FromMilli(5100);
                var tension = Tension.FromLengths(originalLength, strechedLength);
                Console.WriteLine("Uppgift 5* | Tension: " + tension);
            }
            #endregion

            // hoppa över 7, 8

            Console.WriteLine();

            #region Uppgift 8* 
            {
                double sträckgräns = 380;
                double brottgräns = 480;
                double elasticitetsModul = 300 / 0.0032;

                Console.WriteLine("Uppgift 8*");
                Console.WriteLine("a) " + nameof(sträckgräns) + ": " + sträckgräns + " " + Drag.Unit);
                Console.WriteLine("b) " + nameof(brottgräns) + ": " + brottgräns + " " + Drag.Unit);
                Console.WriteLine("c) " + nameof(elasticitetsModul) + ": " + elasticitetsModul + " " + Drag.Unit);
                Console.WriteLine();
            }
            #endregion

            #region Uppgift 9
            {
                double kpUnit = 9.80665;

                Console.WriteLine("Uppgift 9");
                Console.WriteLine("a) aluminum: " + 75_000 * 0.05 / 100 + " " + Drag.Unit);
                Console.WriteLine("b) stål: " + 210_000 * 0.0013 + " " + Drag.Unit);
                Console.WriteLine("c) " + Math.Round(21 * kpUnit / 0.001) + " " + Drag.Unit);
                Console.WriteLine("d) " + Math.Round(150 / (0.073 / 100)) + " " + Drag.Unit);
                Console.WriteLine("e) " + Math.Round(100 / 120_000d * 100, 3) + "% " + Drag.Unit);
                Console.WriteLine();
            }
            #endregion

            const double stålElasticitetsModul = 210_000;

            #region Uppgift 10
            {
                // sökt: spänning
                // givet: töjning, elasticitetsmodul

                var tension = Tension.FromLengthExtension(Length.FromMeters(2), Length.FromMilli(2));
                Console.WriteLine("Uppgift 10: " + tension.Value * stålElasticitetsModul + " " + Drag.Unit);
            }
            #endregion

            #region Uppgift 11
            {
                // sökt: elasticitetsmodul
                // givet: 
                //  diameter=10mm,
                //  längd=100mm, 
                //  längdökning=0.1mm,
                //  kraft = 9500N

                var area = Circle.FromDiameter(Length.FromMilli(10)).Area;
                var tension = Tension.FromLengthExtension(Length.FromMilli(100), Length.FromMilli(0.1));
                var drag = Drag.FromForceOverArea(Force.FromNewtons(9500), area);

                Console.WriteLine("Uppgift 11: E = " + Math.Round(drag.Value / tension.Value));
            }
            #endregion

            const double kopparElasticitetsModul = 120_000;

            #region Uppgift 12
            {
                Console.WriteLine("Uppgift 12");
                // sökt: förlängning
                // givet:
                //  kraft = 10_000N
                //  stor  diameter = 30mm
                //  liten diameter = 10mm
                //  stor  längd = 40mm
                //  liten längd = 20mm

                var force = Force.FromNewtons(10_000);

                var storArea = Circle.FromDiameter(Length.FromMilli(30)).Area;
                var storDrag = Drag.FromForceOverArea(force, storArea);
                double storTension = storDrag.Value / kopparElasticitetsModul;
                var storLength = Length.FromMilli(40);
                var storExtension = Length.FromMilli(storLength * storTension);
                Console.WriteLine(
                    "Stor-förlängning = " + Math.Round(storExtension.Micro) + " " + SIPrefix.Micro.Symbol + "m");

                var litenArea = Circle.FromDiameter(Length.FromMilli(10)).Area;
                var litenDrag = Drag.FromForceOverArea(force, litenArea);
                double litenTension = litenDrag.Value / kopparElasticitetsModul;
                var litenLength = Length.FromMilli(20);
                var litenExtension = Length.FromMilli(litenLength * litenTension);
                Console.WriteLine(
                    "Liten-förlängning = " + Math.Round(litenExtension.Micro) + " " + SIPrefix.Micro.Symbol + "m");

                var totalExtensionDeci = Math.Round((storExtension + litenExtension).Micro);
                Console.WriteLine("Total förlängning = " + totalExtensionDeci + " " + SIPrefix.Micro.Symbol + "m");

                Console.WriteLine();
            }
            #endregion

            const double aluminiumElasticitetsModul = 70_000;

            #region Uppgift 12*
            {
                Console.WriteLine("Uppgift 12");
                // sökt: förlängning
                // givet:
                //  kraft = 10_000N
                //  dellängd = 300mm
                //  alum diameter = 80mm
                //  stål diameter = 40mm

                var force = Force.FromNewtons(10_000);
                var partLength = Length.FromMilli(300);

                var alumArea = Circle.FromDiameter(Length.FromMilli(80)).Area;
                var alumDrag = Drag.FromForceOverArea(force, alumArea);
                double alumTension = alumDrag.Value / aluminiumElasticitetsModul;
                var alumExtension = Length.FromMilli(partLength * alumTension);
                Console.WriteLine(
                    "Alum-förlängning = " + Math.Round(alumExtension.Micro) + " " + SIPrefix.Micro.Symbol + "m");

                var stålArea = Circle.FromDiameter(Length.FromMilli(40)).Area;
                var stålDrag = Drag.FromForceOverArea(force, stålArea);
                double stålTension = stålDrag.Value / stålElasticitetsModul;
                var stålExtension = Length.FromMilli(partLength * stålTension);
                Console.WriteLine(
                    "Stål-förlängning = " + Math.Round(stålExtension.Micro) + " " + SIPrefix.Micro.Symbol + "m");

                var totalExtensionDeci = Math.Round((alumExtension + stålExtension).Micro);
                Console.WriteLine("Total förlängning = " + totalExtensionDeci + " " + SIPrefix.Micro.Symbol + "m");

                Console.WriteLine();
            }
            #endregion

            const double konstantanElasticitetsModul = 110_000;

            #region Uppgift 13
            {
                Console.WriteLine("Uppgift 13");
                // sökt: diameter (mm)
                // givet:
                //  kraft = 100N
                //  längd = 8mm
                //  förlängning = 0.06mm

                var length = Length.FromMilli(8);
                var extension = Length.FromMilli(0.06);
                double tension = Tension.FromLengthExtension(length, extension).Value;
                double drag = tension * konstantanElasticitetsModul;

                var force = Force.FromNewtons(100);
                var area = Drag.FromForceOverDrag(force, drag).Area;

                Console.WriteLine(Circle.FromArea(area).Diameter.ToString());
            }
            #endregion

            #endregion

            Console.WriteLine();

            #region Skjuvning

            const double härdplastLimDragMax = 15; //  N/mm^2

            #region Uppgift 36
            {
                var area = Rectangle.FromSides(Length.FromMilli(50), Length.FromMilli(90)).Area;
                var skjuv = Drag.FromDragArea(härdplastLimDragMax, area);

                Console.WriteLine("Uppgift 36 | " + skjuv.Force + " innan limmet släpper");
            }
            #endregion

            #region Uppgift 36*
            {
                double maxLimDrag = 10; // N/mm^2
                var force = Force.FromKiloNewtons(50);
                var area = Drag.FromForceOverDrag(force, maxLimDrag).Area;
                var length = area / Length.FromMilli(60);

                Console.WriteLine("Uppgift 36* | " + Length.FromMilli(Math.Ceiling(length.Milli)));
            }
            #endregion

            #region Uppgift 37
            {
                var bredd = Length.FromMilli(30);
                var längd = Length.FromMilli(100);

                var skärArea = Rectangle.FromSides(bredd, längd).Area;
                var limMaxSpänning = Drag.FromDragArea(härdplastLimDragMax, skärArea);

                var vänsterHöjd = Length.FromMilli(2);
                var vänsterArea = Rectangle.FromSides(bredd, vänsterHöjd).Area;
                var vänsterMaxAluminiumSpänning = Drag.FromDragArea(150, vänsterArea);

                var högerHöjd = Length.FromMilli(3);
                var högerArea = Rectangle.FromSides(bredd, högerHöjd).Area;
                var högerMaxAluminiumSpänning = Drag.FromDragArea(150, högerArea);

                var lowestForce = Drag.GetLowestForce(
                    limMaxSpänning, vänsterMaxAluminiumSpänning, högerMaxAluminiumSpänning);

                Console.WriteLine("Uppgift 37 | " + lowestForce);
            }
            #endregion

            #region Uppgift 38
            {
                var a = Length.FromMilli(3);
                var length = Length.FromMilli(20);

                var area = Rectangle.FromSides(a, length * 2).Area;
                var spänning = Drag.FromDragArea(200, area);

                Console.WriteLine("Uppgift 38 | " + spänning.Force);
            }
            #endregion

            #region Uppgift 38*
            {
                var force = Force.FromNewtonsPow(10, 5);
                double skärSpänning = 150;
                var area = Drag.FromForceOverDrag(force, skärSpänning).Area;
                var svetsLängd = Length.FromMilli(150);
                var längd = area / svetsLängd;

                Console.WriteLine("Uppgift 38* | " + längd);
            }
            #endregion

            #region Uppgift 39
            {
                var omkrets = Circle.FromDiameter(Length.FromMilli(30)).Circumference;
                var svetsArea = Rectangle.FromSides(omkrets, Length.FromMilli(5)).Area;
                var sänktTillåtenSpänning = 100 / 2d;
                var spänning = Drag.FromDragArea(sänktTillåtenSpänning, svetsArea);

                Console.WriteLine("Uppgift 39 | " + spänning.Force);
            }
            #endregion

            #region Uppgift 40
            {
                var kraft = Force.FromKiloNewtons(20);
                double skärSpänning = 100;
                var skärArea = Drag.FromForceOverDrag(kraft, skärSpänning).Area;

                //Console.WriteLine(Math.Sqrt(skärArea.MilliMeters / 5 / 2));
                Console.WriteLine("Uppgift 40 | " + "TODO");

                // a * L
            }
            #endregion

            #region Uppgift 41
            {
                var nitArea = Circle.FromDiameter(Length.FromMilli(10)).Area;
                double tillåtenSpänning = 80;
                var kraft = Force.FromNewtons(nitArea.MilliMeters * tillåtenSpänning);

                Console.WriteLine("Uppgift 41 | " + kraft);
            }
            #endregion

            #region Uppgift 42
            {
                var kraft = Force.FromKiloNewtons(10);
                double tillåtenSpänning = 90;
                var area = Drag.FromForceOverDrag(kraft, tillåtenSpänning).Area;

                var areaFörNit = area / 4;
                var nitDiameter = Circle.FromArea(areaFörNit).Diameter;

                Console.WriteLine("Uppgift 42 | " + nitDiameter);
            }
            #endregion

            #region Uppgift 43
            {
                var kraft = Force.FromKiloNewtons(50);
                double tillåtenSkärSpänning = 100;
                var totalNitArea = Drag.FromForceOverDrag(kraft, tillåtenSkärSpänning).Area;
                var areaFörNit = totalNitArea / 4;
                var nitDiameter = Circle.FromArea(areaFörNit).Diameter;

                double ssStål_1312_00__tillåtenSpänning = 220;
                double säkerhetsFaktor = 1.0 / 3;
                double säkerTillåtenSpänning = ssStål_1312_00__tillåtenSpänning * säkerhetsFaktor;

                var plåtSpänningsArea = Drag.FromForceOverDrag(kraft, säkerTillåtenSpänning).Area;
                var totalSkärLängd = plåtSpänningsArea / Length.FromMilli(10);
                var skärLängd = totalSkärLängd + nitDiameter * 2;
                
                Console.WriteLine("Uppgift 43 | d = " + nitDiameter + ", b = " + skärLängd);
            }
            #endregion

            #region Uppgift på tavlan :|
            {
                var tjocklek = Length.FromMilli(7);
                var circle = Circle.FromDiameter(12);
                var area = (Area)(circle.Circumference * tjocklek);
                double brottGräns = 360;
                var kraft = Drag.FromDragArea(brottGräns, area).Force;

                //Console.WriteLine("Uppgift 44 | " + kraft);
            }
            #endregion

            #region Uppgift 44
            {

            }
            #endregion

            #endregion
        }
    }
}
