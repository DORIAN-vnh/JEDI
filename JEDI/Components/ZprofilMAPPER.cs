// ZProfile Mapper: Create dynamic Z-profile from formula or reference curve (Improved parser)
using System;
using System.Collections.Generic;
using System.Drawing;
using Grasshopper.Kernel;
using Rhino.Geometry;
using JEDI.Models;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Data;
using JEDI.Resources;

namespace JEDI.Components
{
    public class ZProfileMapper : GH_Component
    {
        public ZProfileMapper() : base("ZProfile Mapper", "ZMap",
            "Generate a Z-profile curve for dynamic toolpath height",
            "JEDI", "Preparation")
        { }

        public override Guid ComponentGuid => new Guid("5D66EC90-4B3D-4A87-A2F0-3C49C6E15E01");
        protected override System.Drawing.Bitmap Icon => Resource1.PATH;

        protected override void RegisterInputParams(GH_InputParamManager p)
        {
            p.AddCurveParameter("Path Curve", "C", "Input path curve for Z mapping", GH_ParamAccess.item);
            p.AddBooleanParameter("Use Formula", "F", "Use mathematical formula for Z (otherwise use input curve Z)", GH_ParamAccess.item, false);
            p.AddTextParameter("Formula", "Z=f(x)", "Z formula: e.g., sin(x), cos(l), x+y", GH_ParamAccess.item, "0.5*sin(l)");
            p.AddBooleanParameter("Evaluate on Length", "L", "Use length along curve instead of x", GH_ParamAccess.item, false);
            p.AddNumberParameter("Tolerance", "Tol", "Polyline tolerance for conversion", GH_ParamAccess.item, 0.1);
        }

        protected override void RegisterOutputParams(GH_OutputParamManager p)
        {
            p.AddCurveParameter("Z Profile", "ZP", "Generated Z profile curve", GH_ParamAccess.item);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            Curve path = null;
            bool useFormula = false;
            string formula = "0";
            bool useLength = false;
            double tol = 0.1;

            if (!DA.GetData(0, ref path)) return;
            if (!DA.GetData(1, ref useFormula)) return;
            if (!DA.GetData(2, ref formula)) return;
            if (!DA.GetData(3, ref useLength)) return;
            if (!DA.GetData(4, ref tol)) return;

            if (!path.IsValid)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Invalid input curve");
                return;
            }

            Curve simplified = path.Simplify(CurveSimplifyOptions.All, tol, tol) ?? path;
            if (!simplified.TryGetPolyline(out Polyline poly))
                poly = new Polyline(simplified.DivideEquidistant(tol));

            var pts = new List<Point3d>();
            double accLen = 0;

            for (int i = 0; i < poly.Count; i++)
            {
                double x = poly[i].X;
                double y = poly[i].Y;
                double l = accLen;
                double z = poly[i].Z;

                if (useFormula)
                {
                    z = EvaluateFormula(formula, x, y, useLength ? l : x);
                }

                pts.Add(new Point3d(x, y, z));

                if (i < poly.Count - 1)
                    accLen += poly[i].DistanceTo(poly[i + 1]);
            }

            var profile = new Polyline(pts).ToNurbsCurve();
            DA.SetData(0, profile);
        }

        private double EvaluateFormula(string formula, double x, double y, double l)
        {
            try
            {
                string expr = formula.ToLowerInvariant();

                // Remplacement des variables
                expr = expr.Replace("x", x.ToString(CultureInfo.InvariantCulture));
                expr = expr.Replace("y", y.ToString(CultureInfo.InvariantCulture));
                expr = expr.Replace("l", l.ToString(CultureInfo.InvariantCulture));

                // Calcul via DataTable uniquement pour les expressions simples
                var dt = new DataTable();
                var result = dt.Compute(expr, "");
                return Convert.ToDouble(result);
            }
            catch (Exception ex)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, $"Invalid formula '{formula}': {ex.Message}");
                return 0;
            }
        }

    }
}
