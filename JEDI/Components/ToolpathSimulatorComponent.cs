using System;
using System.Collections.Generic;
using System.Linq;
using Grasshopper.Kernel;
using Rhino.Geometry;
using JEDI.Models;
using JEDI.Resources;

namespace JEDI.Components
{
    public class ToolpathSimulatorComponent_V2 : GH_Component
    {
        public ToolpathSimulatorComponent_V2()
          : base("Toolpath Simulator", "PathSIM",
              "Simulates tool feed in progressive or full mode",
              "JEDI", "Visualisation")
        { }

        protected override void RegisterInputParams(GH_InputParamManager p)
        {
            p.AddGenericParameter("Paths", "Paths", "List of LaserPathData to simulate", GH_ParamAccess.list);
            p.AddNumberParameter("Step (mm)", "Step", "Distance between simulated points", GH_ParamAccess.item, 5.0);
            p.AddBooleanParameter("Progressive Mode", "Progressive", "Progressive or Full Display", GH_ParamAccess.item, false);
            p.AddNumberParameter("Progress (%)", "Progress", "Percentage of total distance to simulate (0 to 100)", GH_ParamAccess.item, 100.0);
            p.AddBooleanParameter("3D Mode", "3D", "Enable Z-axis (height) tracking", GH_ParamAccess.item, true);
        }

        protected override void RegisterOutputParams(GH_OutputParamManager p)
        {
            p.AddPointParameter("Points", "Pts", "Simulated Lead Points", GH_ParamAccess.list);
            p.AddVectorParameter("Directions", "Dirs", "Directions at every step", GH_ParamAccess.list);
            p.AddNumberParameter("Speeds", "F", "Speed at each point (mm/s)", GH_ParamAccess.list);
            p.AddCurveParameter("Tool Curve", "Toolpath", "Ghost Curve Tool Path", GH_ParamAccess.item);
            p.AddBooleanParameter("Laser ON", "Laser", "Laser ON/OFF status at each point", GH_ParamAccess.list);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            List<LaserPathData> paths = new List<LaserPathData>();
            double step = 5.0;
            bool progressive = false;
            double progressPercent = 100.0;
            bool mode3D = true;

            if (!DA.GetDataList(0, paths)) return;
            DA.GetData(1, ref step);
            DA.GetData(2, ref progressive);
            DA.GetData(3, ref progressPercent);
            DA.GetData(4, ref mode3D);

            List<Point3d> resultPoints = new List<Point3d>();
            List<Vector3d> resultVectors = new List<Vector3d>();
            List<double> resultSpeeds = new List<double>();
            List<bool> resultLaserOn = new List<bool>();

            double totalLength = paths.Sum(p => p.Curve.GetLength());
            double maxDistance = totalLength * (progressPercent / 100.0);
            double accumulated = 0.0;

            foreach (var path in paths)
            {
                double length = path.Curve.GetLength();
                if (length < 0.01) continue;

                double[] tParams = path.Curve.DivideByCount(Math.Max(2, (int)(length / step)), true);

                for (int i = 0; i < tParams.Length; i++)
                {
                    Point3d pt = path.Curve.PointAt(tParams[i]);
                    if (!mode3D) pt.Z = 0;

                    resultPoints.Add(pt);
                    resultSpeeds.Add(path.Vitesse);
                    resultLaserOn.Add(path.Intensite > 0);

                    if (i > 0)
                    {
                        Vector3d dir = pt - resultPoints[resultPoints.Count - 2];
                        resultVectors.Add(dir);
                        accumulated += dir.Length;

                        if (progressive && accumulated > maxDistance)
                            goto END;
                    }
                    else
                    {
                        resultVectors.Add(Vector3d.Zero);
                    }
                }
            }

        END:
            DA.SetDataList(0, resultPoints);
            DA.SetDataList(1, resultVectors);
            DA.SetDataList(2, resultSpeeds);
            DA.SetDataList(4, resultLaserOn);

            if (resultPoints.Count >= 2)
            {
                Polyline polyline = new Polyline(resultPoints);
                DA.SetData(3, new PolylineCurve(polyline));
            }
            else
            {
                DA.SetData(3, null);
            }
        }

        public override Guid ComponentGuid => new Guid("60DFAE72-33E2-4D83-9F9B-7A4AB0B4A124");
        protected override System.Drawing.Bitmap Icon => Resource1.SIMU;
    }
}