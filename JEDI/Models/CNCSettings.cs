using Rhino.Geometry;

namespace JEDI.Models
{
    public class CNCSettings
    {
        public double WidthX { get; set; }
        public double DepthY { get; set; }
        public double HeightZ { get; set; }
        public double MaxSpeed { get; set; }
        public double MaxPower { get; set; }

        public Rectangle3d WorkArea
        {
            get
            {
                return new Rectangle3d(Plane.WorldXY, new Interval(0, WidthX), new Interval(0, DepthY));
            }
        }

        public CNCSettings(double widthX, double depthY, double heightZ, double maxSpeed, double maxPower)
        {
            WidthX = widthX;
            DepthY = depthY;
            HeightZ = heightZ;
            MaxSpeed = maxSpeed;
            MaxPower = maxPower;
        }

        public override string ToString()
        {
            return $"CNC: {WidthX}x{DepthY} mm (Z:{HeightZ}) | Vmax: {MaxSpeed} mm/s | Imax: {MaxPower}%";
        }
    }
}
