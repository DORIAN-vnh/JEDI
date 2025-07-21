using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using Grasshopper.Kernel;
using Rhino.Geometry;
using JEDI.Models;
using JEDI.Resources;

namespace JEDI.Components
{
    public class VisualGroupPreviewComponent : GH_Component
    {
        public VisualGroupPreviewComponent()
          : base("Visual Group Preview", "PathPreview",
              "Displays a grouped visualization of journeys with filter, color and legend options",
              "JEDI", "Visualization")
        { }

        protected override void RegisterInputParams(GH_InputParamManager p)
        {
            p.AddGenericParameter("Paths", "Paths", "List of LaserPathData to display", GH_ParamAccess.list);
            p.AddTextParameter("Mode", "Mode", "Grouping mode: 'Layer', 'Intensity', 'Speed'", GH_ParamAccess.item, "Calque");
            p.AddTextParameter("Filter", "Filter", "Layer name or value to filter (leave blank for all)", GH_ParamAccess.item, "");
            p.AddNumberParameter("Legend Size", "Size", "Legend text size in Rhino", GH_ParamAccess.item, 12.0);
        }

        protected override void RegisterOutputParams(GH_OutputParamManager p)
        {
            p.AddCurveParameter("Preview", "Preview", "Displayed route curve", GH_ParamAccess.list);
            p.AddTextParameter("Legend", "Legend", "Summary of displayed groups", GH_ParamAccess.list);
            p.AddColourParameter("Colors", "Colors", "Color of each group for display", GH_ParamAccess.list);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            List<LaserPathData> paths = new List<LaserPathData>();
            string mode = "Calque";
            string filtre = "";
            double tailleLegende = 12.0;

            if (!DA.GetDataList(0, paths)) return;
            DA.GetData(1, ref mode);
            DA.GetData(2, ref filtre);
            DA.GetData(3, ref tailleLegende);

            var courbes = new List<Curve>();
            var resume = new List<string>();
            var couleurs = new List<Color>();

            IEnumerable<IGrouping<string, LaserPathData>> groupes;
            switch (mode.ToLower())
            {
                case "intensite":
                    groupes = paths.GroupBy(p => p.Intensite.ToString());
                    break;
                case "vitesse":
                    groupes = paths.GroupBy(p => p.Vitesse.ToString());
                    break;
                default:
                    groupes = paths.GroupBy(p => p.Calque);
                    break;
            }

            int colorSeed = 0;
            foreach (var group in groupes)
            {
                if (!string.IsNullOrEmpty(filtre) && !group.Key.Equals(filtre, StringComparison.OrdinalIgnoreCase))
                    continue;

                double totalLength = group.Sum(p => p.Curve?.GetLength() ?? 0);
                resume.Add($"{mode}: {group.Key} | {group.Count()} éléments | {totalLength:0.##} mm");

                Color groupColor = ColorFromIndex(colorSeed++);

                foreach (var p in group)
                {
                    if (p.Curve != null && p.Curve.IsValid)
                    {
                        courbes.Add(p.Curve);
                        couleurs.Add(groupColor);
                    }
                }
            }

            DA.SetDataList(0, courbes);
            DA.SetDataList(1, resume);
            DA.SetDataList(2, couleurs);

            // Pour affichage dans Rhino : code custom à ajouter côté display conduit
            // Utilise tailleLegende pour échelle texte de légende si tu fais un DisplayConduit
        }

        private Color ColorFromIndex(int index)
        {
            Color[] palette = new Color[]
            {
                Color.Red, Color.Blue, Color.Green, Color.Orange,
                Color.Purple, Color.Cyan, Color.Magenta, Color.Brown,
                Color.Teal, Color.DarkRed
            };
            return palette[index % palette.Length];
        }

        public override Guid ComponentGuid => new Guid("A1D9C8F1-BC67-412E-9F0E-308C6E8A3E7F");
        protected override System.Drawing.Bitmap Icon => Resource1.EXPORT;
    }
}