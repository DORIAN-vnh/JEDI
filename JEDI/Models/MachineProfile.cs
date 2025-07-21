namespace JEDI.Models
{
    public class MachineProfile
    {
        public string Name { get; set; } = "Generic Laser";

        public string CommandLaserOn { get; set; } = "M3 S{0}";
        public string CommandLaserOff { get; set; } = "M5";

        public string CommandMoveRapid { get; set; } = "G0 X{0:0.###} Y{1:0.###}";
        public string CommandMoveLinear { get; set; } = "G1 X{0:0.###} Y{1:0.###} F{2:0.##}";
        public string CommandMoveArc { get; set; } = "G2 X{0:0.###} Y{1:0.###} I{2:0.###} J{3:0.###}";

        public string Header { get; set; } = "G21\nG90";
        public string Footer { get; set; } = "M5\nG0 X0 Y0";
        public string CommandRetourOrigin { get; set; } = "G0 X0 Y0";

        public string Unite { get; set; } = "G21";

        public bool SupporteG2G3 { get; set; } = true;

        public double DécalageZAllumageLaser { get; set; } = 0.0;

        public override string ToString()
        {
            return $"Profil : {Name}\n" +
                   $"Laser ON : {CommandLaserOn}\n" +
                   $"Laser OFF : {CommandLaserOff}\n" +
                   $"Déplacement rapide : {CommandMoveRapid}\n" +
                   $"Déplacement linéaire : {CommandMoveLinear}\n" +
                   $"Déplacement arc : {CommandMoveArc}\n" +
                   $"Supporte G2/G3 : {SupporteG2G3}";
        }
    }
}
