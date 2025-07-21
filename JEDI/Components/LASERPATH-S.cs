// Toolpath Component V2: Converts curves into LaserPathData and flags laser ON/OFF states
using System;
using System.Collections.Generic;
using Grasshopper.Kernel;
using Rhino.Geometry;
using JEDI.Models;
using JEDI.Resources;

namespace JEDI.Components
{
    public class ToolpathS : GH_Component
    {
        public ToolpathS() : base("Toolpath-S", "Toolpath-S",
            "Simple data-Converts curves into LaserPathData and assigns laser ON/OFF states",
            "JEDI", "Préparation")
        { }

        public override Guid ComponentGuid => new Guid("F4F2AC12-42A2-4BC9-AF3A-DA9AE64E92F2");

        protected override System.Drawing.Bitmap Icon => Resource1.PATH;

        protected override void RegisterInputParams(GH_InputParamManager p)
        {
            p.AddCurveParameter("Curves", "C", "List of curves to convert", GH_ParamAccess.list);
            p.AddTextParameter("Layer", "L", "Layer name", GH_ParamAccess.item, "Découpe");
            p.AddNumberParameter("Speed", "S", "Default speed", GH_ParamAccess.item, 100.0);
            p.AddNumberParameter("Intensity", "I", "Default intensity", GH_ParamAccess.item, 100.0);
        }

        protected override void RegisterOutputParams(GH_OutputParamManager p)
        {
            p.AddGenericParameter("Toolpaths", "TP", "Complete list of LaserPathData with ON/OFF state", GH_ParamAccess.list);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            List<Curve> curves = new();
            string layer = "Découpe";
            double speed = 100.0;
            double intensity = 100.0;

            if (!DA.GetDataList(0, curves)) return;
            DA.GetData(1, ref layer);
            DA.GetData(2, ref speed);
            DA.GetData(3, ref intensity);

            List<LaserPathData> toolpaths = new();

            for (int i = 0; i < curves.Count; i++)
            {
                Curve current = curves[i];
                if (current == null || !current.IsValid) continue;

                if (i > 0)
                {
                    Point3d prevEnd = curves[i - 1].PointAtEnd;
                    Point3d currStart = current.PointAtStart;
                    if (prevEnd.DistanceTo(currStart) > 0.1)
                    {
                        var jump = new LineCurve(prevEnd, currStart);
                        var transfer = new LaserPathData(jump, speed, 0.0, layer);
                        transfer.IsLaserOn = false;
                        toolpaths.Add(transfer);
                    }
                }

                var cutPath = new LaserPathData(current, speed, intensity, layer);
                cutPath.IsLaserOn = true;
                toolpaths.Add(cutPath);
            }

            DA.SetDataList(0, toolpaths);
        }
    }
}
