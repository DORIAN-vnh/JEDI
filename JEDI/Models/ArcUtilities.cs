using System;
using Rhino.Geometry;

namespace JEDI.Utils
{
    public static class ArcUtilities
    {
        public static bool TryGetArcCenter(Point3d origin, Point3d end, double radius, bool clockwise, out Point3d center)
        {
            double x1 = origin.X;
            double y1 = origin.Y;
            double x2 = end.X;
            double y2 = end.Y;

            double dx = x2 - x1;
            double dy = y2 - y1;
            double q = Math.Sqrt(dx * dx + dy * dy);

            if (q > 2 * radius)
            {
                center = Point3d.Unset;
                return false;
            }

            double xm = (x1 + x2) / 2;
            double ym = (y1 + y2) / 2;

            double d = Math.Sqrt(radius * radius - (q / 2) * (q / 2));
            double ox = -dy * (d / q);
            double oy = dx * (d / q);

            if (!clockwise)
                center = new Point3d(xm - ox, ym - oy, 0);
            else
                center = new Point3d(xm + ox, ym + oy, 0);

            return true;
        }

        public static string GetArcCommand(Point3d start, Point3d end, Point3d center)
        {
            Vector3d centerToStart = start - center;
            Vector3d startToEnd = end - start;
            double cross = Vector3d.CrossProduct(centerToStart, startToEnd).Z;
            return cross < 0 ? "G2" : "G3";
        }

        public static Vector2d GetIJ(Point3d start, Point3d center)
        {
            return new Vector2d(center.X - start.X, center.Y - start.Y);
        }
    }
}
