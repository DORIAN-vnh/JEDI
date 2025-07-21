using System;
using System.Collections.Generic;
using System.Text;
using Rhino.Geometry;

namespace JEDI.Utils
{
    public static class GeometryToGCodeHelper
    {
        public static string ExportArcToGCode(ArcCurve arc, double feedRate, double power, bool laserOn, out List<Point3d> points)
        {
            points = new List<Point3d>();

            if (!arc.IsValid || !arc.Arc.IsValid)
                return "; Invalid arc";

            Arc a = arc.Arc;
            Point3d start = a.StartPoint;
            Point3d end = a.EndPoint;
            Point3d center = a.Center;

            Vector3d centerToStart = start - center;
            Vector3d startToEnd = end - start;

            // Déterminer le sens (G2 horaire, G3 antihoraire)
            Vector3d normal = a.Plane.Normal;
            double direction = Vector3d.CrossProduct(centerToStart, startToEnd) * normal;
            string gCommand = direction >= 0 ? "G2" : "G3";

            StringBuilder sb = new();
            sb.AppendLine($"G0 X{start.X:0.###} Y{start.Y:0.###}");

            if (laserOn)
                sb.AppendLine($"M3 S{power:0.#}");

            sb.AppendLine($"{gCommand} X{end.X:0.###} Y{end.Y:0.###} I{centerToStart.X:0.###} J{centerToStart.Y:0.###} F{feedRate:0.#}");

            if (laserOn)
                sb.AppendLine("M5");

            points.Add(start);
            points.Add(end);
            return sb.ToString();
        }

        public static string ExportCircleToGCode(Circle circle, double feedRate, double power, bool laserOn, out List<Point3d> points)
        {
            points = new List<Point3d>();

            if (!circle.IsValid)
                return "; Invalid circle";

            Point3d start = circle.PointAt(Math.PI); // départ côté gauche du cercle
            Point3d end = start; // Cercle fermé : même point
            Point3d center = circle.Center;
            Vector3d centerToStart = start - center;

            StringBuilder sb = new();
            sb.AppendLine($"G0 X{start.X:0.###} Y{start.Y:0.###}");

            if (laserOn)
                sb.AppendLine($"M3 S{power:0.#}");

            sb.AppendLine($"G2 X{end.X:0.###} Y{end.Y:0.###} I{centerToStart.X:0.###} J{centerToStart.Y:0.###} F{feedRate:0.#}");

            if (laserOn)
                sb.AppendLine("M5");

            points.Add(start);
            points.Add(end);
            return sb.ToString();
        }
    }
}
