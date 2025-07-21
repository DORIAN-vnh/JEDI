// Visualizer based on LaserPathData (not GCode string)
using System;
using System.Collections.Generic;
using System.Drawing;
using Grasshopper.Kernel;
using Rhino.Geometry;
using JEDI.Models;
using JEDI.Resources;

namespace JEDI.Components
{
    public class GCodeColorPreviewComponent : GH_Component
    {
        public GCodeColorPreviewComponent() : base("Toolpath Color Preview", "PathPreview",
            "Displays toolpaths with color based on laser intensity and slider progress",
            "JEDI", "Visualisation")
        { }

        protected override System.Drawing.Bitmap Icon => Resource1.OPTI;

        public override Guid ComponentGuid => new Guid("F413F893-27D6-466F-B3AF-75FC4E9DF7E0");

        protected override void RegisterInputParams(GH_InputParamManager p)
        {
            p.AddGenericParameter("Toolpaths", "Paths", "List of LaserPathData", GH_ParamAccess.list);
            p.AddNumberParameter("Progress", "%", "Progress slider (0-100%)", GH_ParamAccess.item, 100.0);
            p.AddPointParameter("Origin", "O", "Origin point for bar placement", GH_ParamAccess.item, new Point3d(0, 0, 0));
            p.AddNumberParameter("Bar Height", "H", "Height of the gradient bar", GH_ParamAccess.item, 100.0);
            p.AddNumberParameter("Bar Width", "W", "Width of the gradient bar", GH_ParamAccess.item, 5.0);
        }

        protected override void RegisterOutputParams(GH_OutputParamManager p)
        {
            p.AddCurveParameter("Curves", "C", "Toolpath curves", GH_ParamAccess.list);
            p.AddColourParameter("Colors", "Col", "Curve colors based on intensity", GH_ParamAccess.list);
            p.AddTextParameter("Legend", "Legend", "Legend text", GH_ParamAccess.list);
            p.AddMeshParameter("Bar", "Bar", "Gradient mesh bar", GH_ParamAccess.item);
            p.AddTextParameter("Labels", "Labels", "Intensity labels", GH_ParamAccess.list);
            p.AddPointParameter("LabelPts", "Pts", "Label points", GH_ParamAccess.list);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            List<LaserPathData> toolpaths = new List<LaserPathData>();
            double progress = 100.0;
            Point3d origin = new Point3d(0, 0, 0);
            double height = 100.0, width = 5.0;

            if (!DA.GetDataList(0, toolpaths)) return;
            if (!DA.GetData(1, ref progress)) return;
            DA.GetData(2, ref origin);
            DA.GetData(3, ref height);
            DA.GetData(4, ref width);

            var curves = new List<Curve>();
            var colors = new List<Color>();
            var legend = new List<string>();
            var labelPts = new List<Point3d>();
            var labelTxt = new List<string>();

            int max = (int)(toolpaths.Count * (progress / 100.0));

            for (int i = 0; i < toolpaths.Count && i < max; i++)
            {
                var path = toolpaths[i];
                curves.Add(path.Curve);

                int value = (int)(255 - Math.Min(path.Intensite, 100) * 2.55);
                Color col = Color.FromArgb(value, value, value);
                colors.Add(col);
                legend.Add($"{path.Intensite:0}% → RGB({value},{value},{value})");
            }

            var mesh = new Mesh();
            int steps = 100;
            for (int i = 0; i <= steps; i++)
            {
                double t = i / (double)steps;
                double y = t * height;
                int v = (int)(255 - t * 255);
                Color c = Color.FromArgb(v, v, v);

                mesh.Vertices.Add(origin.X, origin.Y + y, origin.Z);
                mesh.Vertices.Add(origin.X + width, origin.Y + y, origin.Z);
                mesh.VertexColors.Add(c);
                mesh.VertexColors.Add(c);

                if (i > 0)
                {
                    int a = (i - 1) * 2;
                    int b = (i - 1) * 2 + 1;
                    int c0 = i * 2;
                    int d = i * 2 + 1;
                    mesh.Faces.AddFace(a, b, d, c0);
                }

                if (i % 10 == 0 || i == 1 || i == 99)
                {
                    labelPts.Add(new Point3d(origin.X + width + 2, origin.Y + y, origin.Z));
                    labelTxt.Add($"{(int)(t * 100)}%");
                }
            }
            mesh.Normals.ComputeNormals();

            DA.SetDataList(0, curves);
            DA.SetDataList(1, colors);
            DA.SetDataList(2, legend);
            DA.SetData(3, mesh);
            DA.SetDataList(4, labelTxt);
            DA.SetDataList(5, labelPts);
        }
    }
}