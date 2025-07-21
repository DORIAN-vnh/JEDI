using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Grasshopper.Kernel;
using Rhino.Geometry;
using Rhino.FileIO;
using Rhino.DocObjects;
using System.Drawing;
using JEDI.Models;
using JEDI.Resources;

namespace JEDI.Components
{
    public class DxfExportComponent : GH_Component
    {
        public DxfExportComponent()
          : base("3DM Export", "Export3DM",
              "Exports paths to a 3DM file with separate layers (DXF manually from Rhino)",
              "JEDI", "Export")
        { }

        protected override System.Drawing.Bitmap Icon => Resource1.DXF;
        protected override void RegisterInputParams(GH_InputParamManager p)
        {
            p.AddGenericParameter("Paths", "Paths", "List of LaserPathData to export", GH_ParamAccess.list);
            p.AddTextParameter("Folder", "Folder", "Destination folder", GH_ParamAccess.item);
            p.AddTextParameter("File name", "Filename", "File name without extension", GH_ParamAccess.item);
            p.AddBooleanParameter("Export", "Go", "Triggers the export", GH_ParamAccess.item);
        }

        protected override void RegisterOutputParams(GH_OutputParamManager p)
        {
            p.AddTextParameter("File", "File", "Full path of the generated file", GH_ParamAccess.item);
            p.AddTextParameter("Status", "Status", "Export status message", GH_ParamAccess.item);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            List<LaserPathData> paths = new List<LaserPathData>();
            string folder = "";
            string filename = "";
            bool export = false;

            if (!DA.GetDataList(0, paths)) return;
            if (!DA.GetData(1, ref folder)) return;
            if (!DA.GetData(2, ref filename)) return;
            if (!DA.GetData(3, ref export)) return;

            if (!export)
            {
                DA.SetData(1, "Export disabled");
                return;
            }

            var file3dm = new File3dm();
            var grouped = paths.GroupBy(p => p.Calque);
            Dictionary<string, int> layerIndices = new Dictionary<string, int>();

            foreach (var group in grouped)
            {
                string layerName = SanitizeLayerName(group.Key);
                var newLayer = new Layer { Name = layerName, Color = Color.Black };
                file3dm.AllLayers.Add(newLayer);
                int layerIndex = file3dm.AllLayers.Count - 1;
                layerIndices[layerName] = layerIndex;

                foreach (var path in group)
                {
                    if (path.Curve != null && path.Curve.IsValid)
                    {
                        var attr = new ObjectAttributes { LayerIndex = layerIndex };
                        file3dm.Objects.AddCurve(path.Curve, attr);
                    }
                }
            }

            string fullPath = Path.Combine(folder, filename + ".3dm");

            bool success = file3dm.Write(fullPath, new File3dmWriteOptions());

            if (success)
            {
                DA.SetData(0, fullPath);
                DA.SetData(1, ".3dm export successful. Open in Rhino then use _Export to DXF/DWG");
            }
            else
            {
                DA.SetData(0, "");
                DA.SetData(1, "Export error");
            }
        }

        private string SanitizeLayerName(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return "Default";
            var invalids = Path.GetInvalidFileNameChars().Concat(new[] { ' ', '.', ':', '/', '\\', '?', '*', '"', '<', '>', '|' });
            foreach (var c in invalids)
                name = name.Replace(c, '_');
            return name.Trim('_');
        }

        public override Guid ComponentGuid => new Guid("4E8B4F17-F13D-4B3C-9855-1D1A6D1B7F2F");
        
    }
}
