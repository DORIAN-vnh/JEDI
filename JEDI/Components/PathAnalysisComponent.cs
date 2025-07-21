// LaserPath Time Analyzer : Calcule le temps d'exécution total des segments avec laser ON et OFF
using System;
using System.Collections.Generic;
using System.Drawing;
using Grasshopper.Kernel;
using JEDI.Models;
using Rhino.Geometry;

namespace JEDI.Components
{
    public class LaserPathTimeAnalyzer : GH_Component
    {
        public LaserPathTimeAnalyzer()
          : base("LaserPath Time Analyzer", "PathTime",
              "Analyzes the total execution time for Laser ON/OFF segments",
              "JEDI", "Analyse")
        { }

        protected override Bitmap Icon => JEDI.Resources.Resource1.SIMU;

        public override Guid ComponentGuid => new Guid("69C9B188-511A-4F37-9CB7-92F57CE3C2EF");

        protected override void RegisterInputParams(GH_InputParamManager p)
        {
            p.AddGenericParameter("Laser Paths", "LP", "Liste des chemins laser (LaserPathData)", GH_ParamAccess.list);
        }

        protected override void RegisterOutputParams(GH_OutputParamManager p)
        {
            p.AddNumberParameter("Temps Total (s)", "T", "Temps total estimé (secondes)", GH_ParamAccess.item);
            p.AddNumberParameter("Temps Laser ON (s)", "TOn", "Temps total avec laser activé", GH_ParamAccess.item);
            p.AddNumberParameter("Temps Laser OFF (s)", "TOff", "Temps total sans laser", GH_ParamAccess.item);
            p.AddNumberParameter("Distance Totale (mm)", "D", "Distance totale parcourue", GH_ParamAccess.item);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            List<LaserPathData> paths = new();
            if (!DA.GetDataList(0, paths)) return;

            double totalTime = 0.0;
            double timeOn = 0.0;
            double timeOff = 0.0;
            double totalDistance = 0.0;

            foreach (var path in paths)
            {
                if (path?.Curve == null || !path.Curve.IsValid) continue;

                double speed = path.Vitesse;
                if (speed <= 0) continue;

                double length = path.Curve.GetLength();
                double time = length / (speed / 60.0); // vitesse mm/s => mm/min

                totalDistance += length;
                totalTime += time;
                if (path.IsLaserOn) timeOn += time;
                else timeOff += time;
            }

            DA.SetData(0, totalTime);
            DA.SetData(1, timeOn);
            DA.SetData(2, timeOff);
            DA.SetData(3, totalDistance);
        }
    }
}
