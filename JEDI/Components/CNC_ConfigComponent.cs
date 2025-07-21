using System;
using Grasshopper.Kernel;
using Rhino.Geometry;
using JEDI.Models; // Assure-toi que le namespace correspond à ta classe CNCSettings
using JEDI.Resources;
using System.Linq;

namespace JEDI.Components
{
    public class CNC_ConfigComponent : GH_Component
    {
        public CNC_ConfigComponent()
          : base("CNC Config", "CNCcfg",
              "Sets the CNC parameters and generates a table visualization",
              "JEDI", "Configuration")
        { }

        protected override System.Drawing.Bitmap Icon => Resource1.CNC;

        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddNumberParameter("Width X", "X", "CNC table width (in mm))", GH_ParamAccess.item);
            pManager.AddNumberParameter("Depth Y", "Y", "Depth of the CNC table (in mm)", GH_ParamAccess.item);
            pManager.AddNumberParameter("Height Z", "Z", "Maximum height in Z (in mm)", GH_ParamAccess.item);
            pManager.AddNumberParameter("Max speed", "V", "Maximum speed (in mm/s)", GH_ParamAccess.item);
            pManager.AddNumberParameter("Max Intensity", "I", "Maximum laser intensity (in %)", GH_ParamAccess.item);
        }

        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            pManager.AddTextParameter("Resume", "Infos", "Résumé lisible des paramètres CNC", GH_ParamAccess.item);
            pManager.AddRectangleParameter("Table", "Table", "dimensions", GH_ParamAccess.item);
            pManager.AddGenericParameter("CNC Settings", "Settings", "CNC params", GH_ParamAccess.item);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            double width = 0, depth = 0, heightZ = 0, vmax = 0, imax = 0;

            if (!DA.GetData(0, ref width)) return;
            if (!DA.GetData(1, ref depth)) return;
            if (!DA.GetData(2, ref heightZ)) return;
            if (!DA.GetData(3, ref vmax)) return;
            if (!DA.GetData(4, ref imax)) return;

            // Crée l'objet CNCSettings
            CNCSettings settings = new CNCSettings(width, depth, heightZ, vmax, imax);

            // Crée le rectangle de visualisation de la table
            Rectangle3d table = settings.WorkArea;

            // Sorties
            DA.SetData(0, settings.ToString());
            DA.SetData(1, table);
            DA.SetData(2, settings);
        }

        public override Guid ComponentGuid => new Guid("FBB48AF4-884E-41A5-8D96-CA9ACEDC1D67");

        
    }
}
