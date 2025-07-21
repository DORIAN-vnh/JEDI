using System;
using Grasshopper.Kernel;
using JEDI.Models;
using JEDI.Resources;

namespace JEDI.Components
{
    public class MachineProfileComponent : GH_Component
    {
        public MachineProfileComponent()
          : base("Machine Profile", "CNC_Profile",
              "Crée un profil machine GCode configurable avec options avancées",
              "JEDI", "Configuration")
        { }

        protected override System.Drawing.Bitmap Icon => Resource1.MACHINE;

        protected override void RegisterInputParams(GH_InputParamManager p)
        {
            p.AddBooleanParameter("Supporte G2/G3", "G2G3", "Activer les arcs G2/G3", GH_ParamAccess.item, true);
            p.AddTextParameter("Name", "Name", "profil name", GH_ParamAccess.item, "Generic Laser");
            p.AddTextParameter("Laser ON", "M3", "Command M3 S...", GH_ParamAccess.item, "M3 S{0}");
            p.AddTextParameter("Laser OFF", "M5", "Command M5", GH_ParamAccess.item, "M5");
            p.AddTextParameter("Fast move", "G0", "Command G0", GH_ParamAccess.item, "G0 X{0:0.###} Y{1:0.###}");
            p.AddTextParameter("Linear move", "G1", "Command G1", GH_ParamAccess.item, "G1 X{0:0.###} Y{1:0.###} F{2:0.##}");
            p.AddTextParameter("Move arc", "G2/G3", "Command G2/G3", GH_ParamAccess.item, "G2 X{0:0.###} Y{1:0.###} I{2:0.###} J{3:0.###}");
            p.AddTextParameter("Header", "Header", "Code initial (G21, G90...)", GH_ParamAccess.item, "G21\nG90");
            p.AddTextParameter("Footer", "Footer", "Code end (M5, return to origin...)", GH_ParamAccess.item, "M5\nG0 X0 Y0");
        }

        protected override void RegisterOutputParams(GH_OutputParamManager p)
        {
            p.AddGenericParameter("Profil machine", "Profile", "Object MachineProfile completed", GH_ParamAccess.item);
            p.AddTextParameter("Resume", "Infos", "Resume profil", GH_ParamAccess.item);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            bool g2g3 = true;
            string nom = "", m3 = "", m5 = "", g0 = "", g1 = "", g2 = "", header = "", footer = "";

            DA.GetData(0, ref g2g3);
            DA.GetData(1, ref nom);
            DA.GetData(2, ref m3);
            DA.GetData(3, ref m5);
            DA.GetData(4, ref g0);
            DA.GetData(5, ref g1);
            DA.GetData(6, ref g2);
            DA.GetData(7, ref header);
            DA.GetData(8, ref footer);

            MachineProfile profile = new MachineProfile
            {
                Name = nom,
                SupporteG2G3 = g2g3,
                CommandLaserOn = m3,
                CommandLaserOff = m5,
                CommandMoveRapid = g0,
                CommandMoveLinear = g1,
                CommandMoveArc = g2,
                Header = header,
                Footer = footer
            };

            DA.SetData(0, profile);
            DA.SetData(1, profile.ToString());
        }

        public override Guid ComponentGuid => new Guid("A88F8A6A-60BB-4ED5-9A2D-C6F6BF64BFD9");

        
    }
}
