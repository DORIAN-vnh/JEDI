// GCodeCommandBuilder : Builds multi-line GCode blocks with optional return to origin
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;
using Grasshopper.Kernel;
using JEDI.Resources;

namespace JEDI.Components
{
    public class GCodeCommandBuilder : GH_Component
    {
        public GCodeCommandBuilder() : base("GCode Command Builder", "CmdBuilder",
            "Builds custom multi-line GCode blocks with optional headers, comments, return to origin and formatting",
            "JEDI", "Export")
        { }

        public override Guid ComponentGuid => new Guid("CDEFE120-3333-4AC2-8D00-ABCDEF002345");

        protected override Bitmap Icon => Resource1.EXPORT;

        protected override void RegisterInputParams(GH_InputParamManager p)
        {
            p.AddTextParameter("Commands", "Cmds", "List of GCode commands (e.g., M3, G1)", GH_ParamAccess.list);
            p.AddTextParameter("Parameters", "Params", "List of parameters per command (e.g., X0 Y0 P10)", GH_ParamAccess.list);
            p.AddTextParameter("Comments", "Comments", "Optional list of comments per line", GH_ParamAccess.list);
            p.AddBooleanParameter("Enabled", "Enable", "Enable or disable the entire block", GH_ParamAccess.item, true);
            p.AddBooleanParameter("New Line Before", "NewLn", "Insert a blank line before the block", GH_ParamAccess.item, true);
            p.AddTextParameter("Block Name", "Block", "Optional block name to label start/end", GH_ParamAccess.item, "");
            p.AddBooleanParameter("Return to Origin", "RTO", "Add 'G0 X0 Y0 ; Return to origin' at the end", GH_ParamAccess.item, false); // Nouveau
        }

        protected override void RegisterOutputParams(GH_OutputParamManager p)
        {
            p.AddTextParameter("GCode Block", "Block", "Final GCode block as text", GH_ParamAccess.item);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            var cmds = new List<string>();
            var parameters = new List<string>();
            var comments = new List<string>();
            bool enabled = true;
            bool newline = true;
            string blockName = "";
            bool returnToOrigin = false;

            if (!DA.GetDataList(0, cmds)) return;
            DA.GetDataList(1, parameters);
            DA.GetDataList(2, comments);
            DA.GetData(3, ref enabled);
            DA.GetData(4, ref newline);
            DA.GetData(5, ref blockName);
            DA.GetData(6, ref returnToOrigin);

            if (!enabled)
            {
                DA.SetData(0, "");
                return;
            }

            var sb = new StringBuilder();

            if (newline)
                sb.AppendLine();

            // Bloc START
            if (!string.IsNullOrWhiteSpace(blockName))
                sb.AppendLine($"; ---- START OF {blockName.Trim().ToUpper()} ----");

            int count = cmds.Count;
            for (int i = 0; i < count; i++)
            {
                string cmd = i < cmds.Count ? cmds[i].Trim().ToUpper() : "";
                string param = i < parameters.Count ? parameters[i].Trim() : "";
                string comment = i < comments.Count ? comments[i].Trim() : "";

                if (string.IsNullOrWhiteSpace(cmd))
                    continue;

                sb.Append(cmd);
                if (!string.IsNullOrWhiteSpace(param))
                    sb.Append(" " + param);
                if (!string.IsNullOrWhiteSpace(comment))
                    sb.Append(" ; " + comment);
                sb.AppendLine();
            }

            if (returnToOrigin)
                sb.AppendLine("G0 X0 Y0 ; Return to origin");

            // Bloc END
            if (!string.IsNullOrWhiteSpace(blockName))
                sb.AppendLine($"; ---- END OF {blockName.Trim().ToUpper()} ----");

            DA.SetData(0, sb.ToString());
        }
    }
}
