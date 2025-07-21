using Rhino.Geometry;
using System;

namespace JEDI.Models
{
    public enum CurveType
    {
        Line,
        Arc,
        Circle,
        Polyline,
        Nurbs,
        Unknown
    }

    public class LaserPathData
    {
        public Curve Curve;
        public double Intensite;
        public double Vitesse;
        public string Calque;
        public double? Z;
        public bool IsLaserOn { get; set; }

        public CurveType Type { get; private set; } = CurveType.Unknown;
        public bool IsClockwise { get; private set; }
        public Point3d StartPoint { get; private set; }
        public Point3d EndPoint { get; private set; }
        public Point3d? Center { get; private set; }

        // ✅ Constructeur principal
        public LaserPathData(Curve curve, double intensite, double vitesse, string calque, double? z = null, bool isLaserOn = true)
        {
            Curve = curve;
            Intensite = intensite;
            Vitesse = vitesse;
            Calque = calque;
            Z = z;
            IsLaserOn = isLaserOn;

            AnalyzeCurve();
        }

        private void AnalyzeCurve()
        {
            if (Curve == null || !Curve.IsValid) return;

            StartPoint = Curve.PointAtStart;
            EndPoint = Curve.PointAtEnd;

            if (Curve is LineCurve)
            {
                Type = CurveType.Line;
            }
            else if (Curve is ArcCurve arc && arc.IsValid && arc.Arc.IsValid)
            {
                Type = CurveType.Arc;
                Center = arc.Arc.Center;

                Vector3d startToCenter = arc.Arc.Center - arc.Arc.StartPoint;
                Vector3d startToEnd = arc.Arc.EndPoint - arc.Arc.StartPoint;
                IsClockwise = Vector3d.CrossProduct(startToCenter, startToEnd).Z < 0;
            }
            else if (Curve.TryGetCircle(out Circle circle))
            {
                Type = CurveType.Circle;
                Center = circle.Center;
                StartPoint = circle.PointAt(Math.PI); // côté gauche
                EndPoint = StartPoint;
                IsClockwise = true; // par défaut
            }
            else if (Curve is PolylineCurve)
            {
                Type = CurveType.Polyline;
            }
            else if (Curve is NurbsCurve)
            {
                Type = CurveType.Nurbs;
            }
        }

        public string GroupKey => $"{Intensite}_{Vitesse}_{Calque}";
    }
}
