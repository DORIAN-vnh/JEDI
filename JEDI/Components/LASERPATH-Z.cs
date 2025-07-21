using System;
using System.Collections.Generic;
using Grasshopper.Kernel;
using Rhino.Geometry;
using JEDI.Models;
using JEDI.Resources;
using System.Linq;

namespace JEDI.Components
{
    public class LaserPathZ : GH_Component
    {
        public LaserPathZ()
          : base("Laser Path-Z", "LPath-Z",
              "Create a LaserPathData object from a Curve/Line/Circle/Arc, a layer, a speed, an intensity, and a Laser ON/OFF information (and optionally a height Z)",
              "JEDI", "Préparation")
        { }
        protected override System.Drawing.Bitmap Icon => Resource1.PATH;


        protected override void RegisterInputParams(GH_InputParamManager p)
        {
            p.AddCurveParameter("Curve", "C", "Cutting curve", GH_ParamAccess.item);
            p.AddTextParameter("Layer", "Layer", "Name of the associated layer", GH_ParamAccess.item, "Découpe");
            p.AddNumberParameter("Speed", "V", "Movement speed (mm/s)", GH_ParamAccess.item, 100.0);
            p.AddNumberParameter("Intensity", "I", "Laser intensity (0-100%)", GH_ParamAccess.item, 100.0);
            p.AddNumberParameter("Z Height (optional)", "Z", "Custom Z Height (optional)", GH_ParamAccess.item);
            p.AddBooleanParameter("Laser ON", "On", "Enables the laser for this path (true = cut, false = link)", GH_ParamAccess.item, true);
            p[4].Optional = true;
        }

        protected override void RegisterOutputParams(GH_OutputParamManager p)
        {
            p.AddGenericParameter("LaserPath", "LP", "Objet LaserPathData", GH_ParamAccess.item);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            Curve curve = null;
            string layer = "Découpe";
            double speed = 100.0;
            double power = 100.0;
            double z = 0.0;
            bool isLaserOn = true;
            bool hasZ = DA.GetData(4, ref z);

            if (!DA.GetData(0, ref curve)) return;
            if (!DA.GetData(1, ref layer)) return;
            DA.GetData(2, ref speed);
            DA.GetData(3, ref power);
            DA.GetData(5, ref isLaserOn);

            if (curve == null || !curve.IsValid)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Invalid curve.");
                return;
            }

            var laserPath = new LaserPathData(curve, power, speed, layer, hasZ ? (double?)z : null, isLaserOn);

            DA.SetData(0, laserPath);
        }

        public override Guid ComponentGuid => new Guid("D709F3C2-3B7E-4D13-A1B9-B28ACBFBF612");
    }
}