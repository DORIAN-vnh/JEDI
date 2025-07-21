// Path Optimizer V2 : Optimizes toolpath order and direction (supports reversing segments)
using System;
using System.Collections.Generic;
using System.Drawing;
using Grasshopper.Kernel;
using Rhino.Geometry;
using JEDI.Models;
using System.Linq;

namespace JEDI.Components
{
    public class PathOptimizer : GH_Component
    {
        public PathOptimizer() : base("Path Optimizer V2", "PathOpt",
            "Optimizes the order and direction of LaserPathData for minimal travel distance",
            "JEDI", "Optimization")
        { }

        protected override Bitmap Icon => JEDI.Resources.Resource1.OPTI;

        public override Guid ComponentGuid => new Guid("7A2D1632-3A23-4DD3-8F55-C9E4D2479146");

        protected override void RegisterInputParams(GH_InputParamManager p)
        {
            p.AddGenericParameter("Laser Paths", "Paths", "List of LaserPathData to optimize", GH_ParamAccess.list);
            p.AddBooleanParameter("Keep Groups", "Group", "Preserve grouping by layer/calque", GH_ParamAccess.item, false);
            p.AddPointParameter("Start Point", "Start", "Starting point for optimization", GH_ParamAccess.item, Point3d.Origin);
            p.AddIntegerParameter("Iterations", "Iter", "Number of optimization iterations", GH_ParamAccess.item, 100);
        }

        protected override void RegisterOutputParams(GH_OutputParamManager p)
        {
            p.AddGenericParameter("Optimized Paths", "Optimized", "Optimized list of LaserPathData", GH_ParamAccess.list);
            p.AddNumberParameter("Distance Total", "Dist", "Total distance of optimized path", GH_ParamAccess.item);
            p.AddTextParameter("Info", "Info", "Information and debug logs", GH_ParamAccess.item);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            var paths = new List<LaserPathData>();
            bool keepGroups = false;
            Point3d startPoint = Point3d.Origin;
            int iterations = 100;

            if (!DA.GetDataList(0, paths)) return;
            if (!DA.GetData(1, ref keepGroups)) return;
            if (!DA.GetData(2, ref startPoint)) return;
            DA.GetData(3, ref iterations);

            if (paths.Count == 0)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "No paths provided.");
                return;
            }

            List<LaserPathData> bestResult = null;
            double bestDistance = double.MaxValue;

            for (int iter = 0; iter < iterations; iter++)
            {
                var available = new List<LaserPathData>(paths);
                var current = new List<LaserPathData>();
                Point3d currentPos = startPoint;
                double totalDist = 0;

                while (available.Count > 0)
                {
                    double minDist = double.MaxValue;
                    int bestIndex = -1;
                    bool reverse = false;

                    for (int i = 0; i < available.Count; i++)
                    {
                        var candidate = available[i];
                        Point3d start = candidate.Curve.PointAtStart;
                        Point3d end = candidate.Curve.PointAtEnd;
                        double distStart = currentPos.DistanceTo(start);
                        double distEnd = currentPos.DistanceTo(end);

                        if (distStart < minDist)
                        {
                            minDist = distStart;
                            bestIndex = i;
                            reverse = false;
                        }
                        if (distEnd < minDist)
                        {
                            minDist = distEnd;
                            bestIndex = i;
                            reverse = true;
                        }
                    }

                    var chosen = available[bestIndex];
                    Curve original = chosen.Curve;

                

                    current.Add(chosen);
                    currentPos = chosen.Curve.PointAtEnd;
                    totalDist += currentPos.DistanceTo(chosen.Curve.PointAtStart);
                    available.RemoveAt(bestIndex);
                }

                if (totalDist < bestDistance)
                {
                    bestDistance = totalDist;
                    bestResult = current;
                }
            }

            DA.SetDataList(0, bestResult);
            DA.SetData(1, bestDistance);
            DA.SetData(2, $"Best of {iterations} iterations | Distance: {bestDistance:0.##} mm");
        }
    }
}
