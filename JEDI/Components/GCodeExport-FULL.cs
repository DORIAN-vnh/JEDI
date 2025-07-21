// GCode Exporter V3 : Correct circle direction + enhancements 
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Text;
using Grasshopper.Kernel;
using Rhino.Geometry;
using JEDI.Models;
using JEDI.Resources;
using JEDI.Utilities;

namespace JEDI.Components
{
    public class GCodeExporterFULL : GH_Component
    {
        public GCodeExporterFULL() : base("GCode Exporter FULL", "GCodeExpFULL",
            "Exports GCode with proper circle direction and enhanced fallback conversion",
            "JEDI", "Export")
        { }

        protected override Bitmap Icon => Resource1.EXPORT;
        public override Guid ComponentGuid => new Guid("F209FA87-C36D-4A63-97B2-BF25D5CDE8AB");

        protected override void RegisterInputParams(GH_InputParamManager p)
        {
            p.AddGenericParameter("Laser Paths", "LP", "List of LaserPathData", GH_ParamAccess.list);
            p.AddGenericParameter("Machine Profile", "Profile", "Machine profile settings", GH_ParamAccess.item);
            p.AddGenericParameter("CNC Config", "CNC", "CNC workspace config", GH_ParamAccess.item);
            p.AddNumberParameter("Tolerance", "Tol", "Polyline fallback tolerance", GH_ParamAccess.item, 0.1);
            p.AddTextParameter("File Path", "Path", "Export file path", GH_ParamAccess.item, "");
            p.AddBooleanParameter("Export", "Export", "Export GCode to file", GH_ParamAccess.item, false);
            p.AddBooleanParameter("Return to Origin", "Return", "Add G0 to origin at the end", GH_ParamAccess.item, true);
            p.AddBooleanParameter("Use Inches", "Inches", "Use inches instead of mm", GH_ParamAccess.item, false);
            p.AddBooleanParameter("Include Z Height", "Z", "Include G1 Z# ; Set Z height at the beginning of each path", GH_ParamAccess.item, true); // Nouveau paramètre
        }

        protected override void RegisterOutputParams(GH_OutputParamManager p)
        {
            p.AddTextParameter("GCode", "GCode", "Generated GCode text", GH_ParamAccess.item);
            p.AddPointParameter("Points", "Pts", "All toolpath points", GH_ParamAccess.list);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            var paths = new List<LaserPathData>();
            MachineProfile profile = null;
            CNCSettings cnc = null;
            double tol = 0.1;
            string filePath = string.Empty;
            bool export = false;
            bool returnToOrigin = true;
            bool useInches = false;
            bool includeZ = true;

            if (!DA.GetDataList(0, paths)) return;
            if (!DA.GetData(1, ref profile)) return;
            if (!DA.GetData(2, ref cnc)) return;
            DA.GetData(3, ref tol);
            DA.GetData(4, ref filePath);
            DA.GetData(5, ref export);
            DA.GetData(6, ref returnToOrigin);
            DA.GetData(7, ref useInches);
            DA.GetData(8, ref includeZ); // récupération du booléen Include Z

            GeometryAnalyzer.UseMetric = !useInches;

            var sb = new StringBuilder();
            var points = new List<Point3d>();

            sb.AppendLine("; JEDI /// GCode Export/// By Dorian Vnh /// www.dv-concept.be");
            sb.AppendLine(useInches ? "G20 ; Units in inches" : "G21 ; Units in mm");
            sb.AppendLine("G90 ; Absolute positioning");

            foreach (var path in paths)
            {
                if (path?.Curve == null || !path.Curve.IsValid) continue;
                var crv = path.Curve;
                double power = path.Intensite;
                double speed = path.Vitesse;
                double z = path.Z ?? cnc?.HeightZ ?? 0;

                BoundingBox bbox = crv.GetBoundingBox(true);
                if (cnc != null && (bbox.Max.X > cnc.WidthX || bbox.Max.Y > cnc.DepthY || bbox.Min.X < 0 || bbox.Min.Y < 0))
                {
                    AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "Path exceeds CNC work area.");
                    continue;
                }

                if (includeZ)
                    sb.AppendLine($"G1 Z{z:0.###} ; Set Z height");

                if (crv is LineCurve line)
                {
                    Point3d pt = line.PointAtEnd;
                    if (path.IsLaserOn) sb.AppendLine(string.Format(profile.CommandLaserOn, power));
                    sb.AppendLine(string.Format(profile.CommandMoveLinear, pt.X, pt.Y, speed));
                    if (path.IsLaserOn) sb.AppendLine(profile.CommandLaserOff);
                    points.Add(pt);
                }
                else if (crv is ArcCurve arc && arc.IsValid && arc.Arc.IsValid && profile.SupporteG2G3)
                {
                    Arc arcData = arc.Arc;
                    Point3d start = arcData.StartPoint;
                    Point3d end = arcData.EndPoint;
                    Point3d center = arcData.Center;

                    string cmd = GeometryAnalyzer.GetArcCommand(start, end, center);
                    var ij = GeometryAnalyzer.GetIJ(start, center);

                    sb.AppendLine($"G0 X{start.X:0.###} Y{start.Y:0.###}");
                    if (path.IsLaserOn) sb.AppendLine(string.Format(profile.CommandLaserOn, power));
                    sb.AppendLine($"{cmd} X{end.X:0.###} Y{end.Y:0.###} I{ij.X:0.###} J{ij.Y:0.###} F{speed:0.#}");
                    if (path.IsLaserOn) sb.AppendLine(profile.CommandLaserOff);
                    points.Add(start);
                    points.Add(end);
                }
                else if (crv.TryGetCircle(out Circle circle))
                {
                    Point3d start = circle.PointAt(Math.PI); // Start à gauche
                    Point3d end = start;
                    Point3d center = circle.Center;
                    string cmd = "G2";
                    var ij = GeometryAnalyzer.GetIJ(start, center);

                    sb.AppendLine($"G0 X{start.X:0.###} Y{start.Y:0.###}");
                    if (path.IsLaserOn) sb.AppendLine(string.Format(profile.CommandLaserOn, power));
                    sb.AppendLine($"{cmd} X{end.X:0.###} Y{end.Y:0.###} I{ij.X:0.###} J{ij.Y:0.###} F{speed:0.#}");
                    if (path.IsLaserOn) sb.AppendLine(profile.CommandLaserOff);
                    points.Add(start);
                }
                else
                {
                    Curve simplified = crv.Simplify(CurveSimplifyOptions.All, tol, tol);
                    if (simplified != null && simplified.TryGetPolyline(out Polyline poly))
                    {
                        if (poly.Count > 0)
                        {
                            sb.AppendLine($"G0 X{poly[0].X:0.###} Y{poly[0].Y:0.###}");
                            if (path.IsLaserOn) sb.AppendLine(string.Format(profile.CommandLaserOn, power));

                            for (int i = 1; i < poly.Count; i++)
                            {
                                sb.AppendLine(string.Format(profile.CommandMoveLinear, poly[i].X, poly[i].Y, speed));
                                points.Add(poly[i]);
                            }
                            if (path.IsLaserOn) sb.AppendLine(profile.CommandLaserOff);
                        }
                    }
                    else
                    {
                        Point3d[] pts = crv.DivideEquidistant(tol);
                        if (pts.Length > 0)
                        {
                            sb.AppendLine($"G0 X{pts[0].X:0.###} Y{pts[0].Y:0.###}");
                            if (path.IsLaserOn) sb.AppendLine(string.Format(profile.CommandLaserOn, power));

                            for (int i = 1; i < pts.Length; i++)
                            {
                                sb.AppendLine(string.Format(profile.CommandMoveLinear, pts[i].X, pts[i].Y, speed));
                                points.Add(pts[i]);
                            }
                            if (path.IsLaserOn) sb.AppendLine(profile.CommandLaserOff);
                        }
                    }
                }
            }

            if (returnToOrigin)
                sb.AppendLine("G0 X0 Y0 ; Return to origin");

            sb.AppendLine("M2 ; End of program");

            if (export && !string.IsNullOrWhiteSpace(filePath))
            {
                try
                {
                    File.WriteAllText(filePath, sb.ToString());
                }
                catch (Exception ex)
                {
                    AddRuntimeMessage(GH_RuntimeMessageLevel.Error, $"Export failed: {ex.Message}");
                }
            }

            DA.SetData(0, sb.ToString());
            DA.SetDataList(1, points);
        }
    }
}
