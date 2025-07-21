// Parallel Hatch Generator Component (Updated: English + Optional Hole + Intensity Gradient Fix + Z from CNC Config)
using System;
using System.Collections.Generic;
using Grasshopper.Kernel;
using Rhino;
using Rhino.Geometry;
using JEDI.Models;
using JEDI.Resources;

namespace JEDI.Components
{
    public class ParallelHatchComponent : GH_Component
    {
        public ParallelHatchComponent() : base("Parallel Hatch", "HatchPar",
            "Generates parallel hatches within a given contour.",
            "JEDI", "Filling")
        { }

        public override Guid ComponentGuid => new Guid("89A6B1D5-6EF6-4436-BC3C-13463C2E1A1B");

        protected override System.Drawing.Bitmap Icon => Resource1.PARA;

        protected override void RegisterInputParams(GH_InputParamManager p)
        {
            p.AddCurveParameter("Contour", "C", "Outer closed contour", GH_ParamAccess.item);
            p.AddCurveParameter("Hole", "T", "Hole contour (optional)", GH_ParamAccess.item);
            p[1].Optional = true;
            p.AddNumberParameter("Spacing", "Step", "Spacing between lines", GH_ParamAccess.item, 5.0);
            p.AddNumberParameter("Angle (°)", "Angle", "Line orientation angle", GH_ParamAccess.item, 0.0);
            p.AddNumberParameter("Start Intensity", "I", "Laser start intensity (if gradient active)", GH_ParamAccess.item, 100);
            p.AddNumberParameter("End Intensity", "IEnd", "End intensity for gradient", GH_ParamAccess.item, 0);
            p.AddNumberParameter("Speed", "V", "Laser movement speed (constant)", GH_ParamAccess.item, 100);
            p.AddTextParameter("Layer", "L", "Layer name for laser path", GH_ParamAccess.item, "Parallel");
            p.AddBooleanParameter("Use Gradient", "Grad", "Enable intensity gradient", GH_ParamAccess.item, false);
            p.AddPointParameter("Gradient Origin", "O", "Origin point for intensity gradient", GH_ParamAccess.item, Point3d.Origin);
            p.AddNumberParameter("Z Height", "Z", "Z height for focus from CNC config", GH_ParamAccess.item, 0.0);
        }

        protected override void RegisterOutputParams(GH_OutputParamManager p)
        {
            p.AddCurveParameter("Hatches", "H", "Generated hatching lines", GH_ParamAccess.list);
            p.AddGenericParameter("LaserPaths", "LP", "LaserPathData objects", GH_ParamAccess.list);
            p.AddTextParameter("Info", "Infos", "Debug and count info", GH_ParamAccess.item);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            Curve contour = null;
            Curve hole = null;
            double step = 5.0, angle = 0.0, intensityStart = 100, speed = 100, intensityEnd = 0, zHeight = 0.0;
            bool useGradient = false;
            string layer = "Parallel";
            Point3d origin = Point3d.Origin;

            if (!DA.GetData(0, ref contour) || contour == null)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Invalid or missing contour.");
                return;
            }

            DA.GetData(1, ref hole);
            DA.GetData(2, ref step);
            DA.GetData(3, ref angle);
            DA.GetData(4, ref intensityStart);
            DA.GetData(5, ref intensityEnd);
            DA.GetData(6, ref speed);
            DA.GetData(7, ref layer);
            DA.GetData(8, ref useGradient);
            DA.GetData(9, ref origin);
            DA.GetData(10, ref zHeight);

            origin.Z = 0;
            contour = contour.DuplicateCurve();
            if (hole != null) hole = hole.DuplicateCurve();

            double tol = RhinoDoc.ActiveDoc?.ModelAbsoluteTolerance ?? 0.01;
            Curve[] region = hole != null ? Curve.CreateBooleanDifference(contour, hole, tol) : new Curve[] { contour };
            if (region == null || region.Length == 0)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Failed to compute planar region.");
                return;
            }

            Brep[] breps = Brep.CreatePlanarBreps(region, tol);
            if (breps == null || breps.Length == 0)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Could not generate planar Brep for hatching.");
                return;
            }
            Brep brep = breps[0];

            Vector3d dir = new Vector3d(Math.Cos(RhinoMath.ToRadians(angle)), Math.Sin(RhinoMath.ToRadians(angle)), 0);
            if (dir.IsTiny()) dir = Vector3d.XAxis;

            Point3d far = origin + dir * 10000;
            List<Curve> contours = new List<Curve>(Brep.CreateContourCurves(brep, origin, far, step));
            if (contours.Count == 0)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "No hatch lines generated.");
                return;
            }

            var paths = new List<LaserPathData>();
            double maxDist = 0;
            if (useGradient)
            {
                foreach (var c in contours)
                    maxDist = Math.Max(maxDist, c.PointAtNormalizedLength(0.5).DistanceTo(origin));
                if (maxDist < RhinoMath.ZeroTolerance) maxDist = 1;
            }

            foreach (var c in contours)
            {
                Curve flat = c.DuplicateCurve();
                flat.Transform(Transform.PlanarProjection(Plane.WorldXY));

                double intensity = intensityStart;
                if (useGradient)
                {
                    double d = flat.PointAtNormalizedLength(0.5).DistanceTo(origin);
                    double t = Math.Min(1.0, d / maxDist);
                    intensity = intensityStart + (intensityEnd - intensityStart) * t;
                }

                Point3d start = flat.PointAtStart;
                Point3d end = flat.PointAtEnd;
                start.Z = zHeight;
                end.Z = zHeight;

                Line newLine = new Line(start, end);
                paths.Add(new LaserPathData(newLine.ToNurbsCurve(), intensity, speed, layer));
            }

            DA.SetDataList(0, contours);
            DA.SetDataList(1, paths);
            DA.SetData(2, $"{contours.Count} hatch lines generated.");
        }
    }
}
