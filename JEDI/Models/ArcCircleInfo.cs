// GeometryAnalyzer : Utility class for arc and circle direction in GCode
using System;
using Rhino.Geometry;

namespace JEDI.Utilities
{
    public class ArcCircleInfo
    {
        public Point3d Start { get; set; }
        public Point3d End { get; set; }
        public Point3d Center { get; set; }
        public double Radius { get; set; }
        public bool Clockwise { get; set; }
    }

    public static class GeometryAnalyzer
    {
        public static bool UseMetric { get; set; } = true; // true = mm, false = inches

        public static ArcCircleInfo FromArcCurve(ArcCurve arcCurve)
        {
            if (!arcCurve.IsValid || !arcCurve.Arc.IsValid)
                return null;

            Arc arc = arcCurve.Arc;
            Point3d start = arc.StartPoint;
            Point3d end = arc.EndPoint;
            Point3d center = arc.Center;
            double radius = arc.Radius;

            if (!UseMetric)
                radius /= 25.4;

            Vector3d v1 = start - center;
            Vector3d v2 = end - start;
            Vector3d normal = arc.Plane.Normal;

            bool clockwise = Vector3d.CrossProduct(v1, v2) * normal > 0;

            return new ArcCircleInfo
            {
                Start = ConvertUnit(start),
                End = ConvertUnit(end),
                Center = ConvertUnit(center),
                Radius = radius,
                Clockwise = clockwise
            };
        }

        public static ArcCircleInfo FromCircle(Circle circle)
        {
            Point3d center = circle.Center;
            double radius = circle.Radius;
            Point3d start = circle.PointAt(Math.PI); // point à gauche
            Point3d end = start; // Cercle complet

            if (!UseMetric)
                radius /= 25.4;

            return new ArcCircleInfo
            {
                Start = ConvertUnit(start),
                End = ConvertUnit(end),
                Center = ConvertUnit(center),
                Radius = radius,
                Clockwise = true // toujours G2 en GRBL
            };
        }

        public static string GetArcCommand(Point3d start, Point3d end, Point3d center)
        {
            Vector3d startVec = start - center;
            Vector3d endVec = end - center;
            double cross = Vector3d.CrossProduct(startVec, endVec).Z;
            return cross < 0 ? "G2" : "G3";
        }

        public static Point2d GetIJ(Point3d start, Point3d center)
        {
            Point3d s = ConvertUnit(start);
            Point3d c = ConvertUnit(center);
            return new Point2d(c.X - s.X, c.Y - s.Y);
        }

        private static Point3d ConvertUnit(Point3d pt)
        {
            if (UseMetric) return pt;
            return new Point3d(pt.X / 25.4, pt.Y / 25.4, pt.Z / 25.4);
        }
    }
}
