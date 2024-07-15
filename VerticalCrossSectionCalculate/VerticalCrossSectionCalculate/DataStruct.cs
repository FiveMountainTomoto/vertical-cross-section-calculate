using static System.Math;

namespace VerticalCrossSectionCalculate
{
    public class Point
    {
        public string Name { get; set; }
        public double X { get; set; }
        public double Y { get; set; }
        public double H { get; set; }
        // 计算与另一点的距离
        public double GetDistance(Point p)
        {
            return Sqrt(Pow(p.X - X, 2) + Pow(p.Y - Y, 2));
        }
    }
    public class Angle
    {
        public double D { get; set; }
        public double M { get; set; }
        public double S { get; set; }
        public double Dms { get; set; }
        public double Rad { get; set; }

        public Angle(double rad)
        {
            Rad = rad;
            Dms = rad * 180 / PI;
            D = (int)Dms;
            M = (int)((Dms - D) * 60);
            S = (Dms - D - M / 60) * 3600;
        }
    }
}
