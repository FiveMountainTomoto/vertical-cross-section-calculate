using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using static System.Math;

namespace VerticalCrossSectionCalculate
{
    public class SectionCalculate
    {
        public Dictionary<string, Point> Pts { get; set; }
        private List<string> results;
        private const double H0 = 15;
        private Point A, B;

        public SectionCalculate(string path)
        {
            Pts = new Dictionary<string, Point>();
            ReadingData(path);
        }

        public List<string> CalculateResult()
        {
            results = new List<string>();
            // 1.1
            Angle ab = GetAzimuth(A, B);
            results.Add($"1 A,B两点的坐标方位角 {ab.D}.{ab.M}{ab.S * 100:F0}");
            // 1.2
            A.H = GetInsPoiHeight(A, out IEnumerable<dynamic> aNearby);
            IEnumerable<string> aStrs = from p in aNearby select $"点号:{p.Name},距离:{p.Distance:F3}";
            string a = "\nA的最近5个点：\n" + string.Join("\n", aStrs);
            results.Add($"2  A的内插高程 {A.H:F3}{a}");

            B.H = GetInsPoiHeight(B, out IEnumerable<dynamic> bNearby);
            IEnumerable<string> bStrs = from p in bNearby select $"点号:{p.Name},距离:{p.Distance:F3}";
            string b = "\nB的最近5个点：\n" + string.Join("\n", bStrs);
            results.Add($"3  B的内插高程 {B.H:F3}{b}");
            // 1.3
            double areaAB = GetAllSectionArea(new Point[] { A, B });
            results.Add($"4  以A、B为梯形的两个端点的梯形面积 {areaAB:F3}");
            // 2.1
            results.Add($"5  纵断面的总长度 {GetCrossSecLength():F3}");
            // 2.2
            List<Point> croInsPts = GetCrossInsertPoints();
            IEnumerable<string> insPtsStr = from p in croInsPts where p.Name[0] == 'N' select $"{p.Name}\t{p.X:F3}\t{p.Y:F3}\t{p.H:F3}";
            results.Add("纵断面上的内插点序列：\n" + string.Join("\n", insPtsStr));
            // 2.3
            double crossArea = GetAllSectionArea(croInsPts);
            results.Add($"9 纵断面面积 {crossArea:F3}");
            // 3.1
            Point M0 = GetCenterPoint(Pts["K0"], Pts["K1"]);
            M0.Name = "M0";
            Point M1 = GetCenterPoint(Pts["K1"], Pts["K2"]);
            M1.Name = "M1";
            results.Add("横断面中心点：\n" +
                $"{M0.Name}\t{M0.X:F3}\t{M0.Y:F3}\t{M0.H:F3}\n" +
                $"{M1.Name}\t{M1.X:F3}\t{M1.Y:F3}\t{M1.H:F3}");
            // 3.2
            // 没输出结果到result，非要写成屎山代码来输出，真是rz题目
            List<Point> ver1InsPts = GetVertcInsertPoints(Pts["K0"], Pts["K1"], out double[] cor1);
            List<Point> ver2InsPts = GetVertcInsertPoints(Pts["K1"], Pts["K2"], out double[] cor2);
            results.Add($"12 第2个横断面的坐标方位角 {cor2[0]:F6}");
            results.Add($"13 第1个横断面中的j=3的内插点的坐标x {cor1[1]:F3}");
            results.Add($"14 第1个横断面中的j=3的内插点的坐标y {cor1[2]:F3}");
            results.Add($"15 第1个横断面中的j=3的内插点的高程 {cor1[3]:F3}");
            results.Add($"16 第2个横断面中的j=-3的内插点的坐标x {cor2[4]:F3}");
            results.Add($"17 第2个横断面中的j=-3的内插点的坐标y {cor2[5]:F3}");
            results.Add($"18 第2个横断面中的j=-3的内插点的高程 {cor2[6]:F3}");
            // 3.3
            double ver1Area = GetAllSectionArea(ver1InsPts);
            double ver2Area = GetAllSectionArea(ver2InsPts);
            results.Add($"19 第1个横断面的面积 {ver1Area:F3}");
            results.Add($"20 第2个横断面的面积 {ver2Area:F3}");
            // 4.1
            double alpha01 = GetAzimuth(Pts["K0"], Pts["K1"]).Rad + PI / 2;
            double alpha12 = GetAzimuth(Pts["K1"], Pts["K2"]).Rad + PI / 2;
            var ver1AllArea = from p in croInsPts.Take(14)
                              select GetVertcInsertPoints(p, alpha01)
                              into insPts
                              select new { Area = GetAllSectionArea(insPts), Center = insPts[5] };
            var ver2AllArea = from p in croInsPts.Skip(13) select GetVertcInsertPoints(p, alpha12) into insPts select new { Area = GetAllSectionArea(insPts), Center = insPts[5] };
            // 4.2
            List<double> ver1Vols = GetVolumes(ver1AllArea);
            List<double> ver2Vols = GetVolumes(ver2AllArea);
            // 4.3
            double sumVolK01 = 0, sumVolK12 = 0;
            foreach (double v in ver1Vols)
            {
                sumVolK01 += v;
            }
            foreach (double v in ver2Vols)
            {
                sumVolK12 += v;
            }
            results.Add($"21 第1个纵断面的土石方总量 {sumVolK01:F3}");
            results.Add($"22 第2个纵断面的土石方总量 {sumVolK12:F3}");
            return results;
        }

        // 读取数据
        private void ReadingData(string path)
        {
            Func<string, Point> point = line =>
            {
                string[] split = line.Split(',');
                Point p = new Point
                {
                    Name = split[0],
                    X = double.Parse(split[1]),
                    Y = double.Parse(split[2]),
                };
                try
                {
                    p.H = double.Parse(split[3]);
                }
                catch (IndexOutOfRangeException)
                {
                    p.H = 0;
                }
                return p;
            };

            using (StreamReader sr = new StreamReader(path))
            {
                sr.ReadLine(); sr.ReadLine();
                A = point(sr.ReadLine());
                B = point(sr.ReadLine());

                while (!sr.EndOfStream)
                {
                    string line = sr.ReadLine();
                    if (string.IsNullOrEmpty(line))
                    {
                        continue;
                    }
                    Point p = point(line);
                    Pts.Add(p.Name, p);
                }
            }
        }

        // 计算方位角
        private static Angle GetAzimuth(Point a, Point b)
        {
            double dy = b.Y - a.Y;
            double dx = b.X - a.X;
            if (dx == 0 && dy == 0)
            {
                return null;
            }

            double alpha = Atan(dy / dx);
            if (dy >= 0)
            {
                if (dx > 0)
                {
                    ;
                }
                else if (dx < 0)
                {
                    alpha += PI;
                }
                else
                {
                    alpha = PI / 2;
                }
            }
            else
            {
                if (dx > 0)
                {
                    alpha += 2 * PI;
                }
                else if (dx < 0)
                {
                    alpha += PI;
                }
                else
                {
                    alpha = PI * 3 / 2;
                }
            }
            return new Angle(alpha);
        }

        // 计算内插点高程
        private double GetInsPoiHeight(Point ins, out IEnumerable<dynamic> poisNearby)
        {
            poisNearby = Pts.Select(p => new { p.Value.H, Distance = ins.GetDistance(p.Value), Name = p.Key }).Where(p => p.Distance != 0).OrderBy(p => p.Distance).Take(5);
            double fenzi = 0, fenmu = 0;// 分子和分母
            foreach (dynamic p in poisNearby)
            {
                fenzi += p.H / p.Distance;
                fenmu += 1 / p.Distance;
            }
            return fenzi / fenmu;
        }

        // 断面面积的计算
        private double GetAllSectionArea(IEnumerable<Point> pois)
        {
            Func<Point, Point, double> area = (p0, p1) =>
            (p0.H + p1.H - 2 * H0) / 2 * (p0.GetDistance(p1));
            double sumArea = 0;
            IEnumerator<Point> enu = pois.GetEnumerator();
            enu.MoveNext();
            while (true)
            {
                Point p0 = enu.Current;
                if (!enu.MoveNext())
                {
                    break;
                }

                Point p1 = enu.Current;
                sumArea += area(p0, p1);
            }
            return sumArea;
        }

        // 计算纵断面的长度
        private double GetCrossSecLength()
        {
            double d01 = Pts["K0"].GetDistance(Pts["K1"]);
            double d12 = Pts["K1"].GetDistance(Pts["K2"]);
            return d01 + d12;
        }

        // 计算纵断面上的内插点序列
        private List<Point> GetCrossInsertPoints()
        {
            List<Point> insPts = new List<Point>() { Pts["K0"] };
            double alpha01 = GetAzimuth(Pts["K0"], Pts["K1"]).Rad;
            int num = 1;
            for (double L = 10; L < Pts["K0"].GetDistance(Pts["K1"]); L += 10)
            {
                Point insP = new Point()
                {
                    Name = "N" + num++,
                    X = Pts["K0"].X + L * Cos(alpha01),
                    Y = Pts["K0"].Y + L * Sin(alpha01)
                };
                insP.H = GetInsPoiHeight(insP, out _);
                insPts.Add(insP);
            }
            insPts.Add(Pts["K1"]);

            double alpha12 = GetAzimuth(Pts["K1"], Pts["K2"]).Rad;
            for (double L = 10; L < Pts["K1"].GetDistance(Pts["K2"]); L += 10)
            {
                Point insP = new Point()
                {
                    Name = "N" + num++,
                    X = Pts["K1"].X + L * Cos(alpha12),
                    Y = Pts["K1"].Y + L * Sin(alpha12)
                };
                insP.H = GetInsPoiHeight(insP, out _);
                insPts.Add(insP);
            }
            insPts.Add(Pts["K2"]);
            return insPts;
        }

        // 计算横断面中心点
        private Point GetCenterPoint(Point p1, Point p2)
        {
            Point p = new Point()
            {
                X = (p1.X + p2.X) / 2,
                Y = (p1.Y + p2.Y) / 2,
            };
            p.H = GetInsPoiHeight(p, out _);
            return p;
        }

        // 计算横断面上的内插点序列
        private List<Point> GetVertcInsertPoints(Point K0, Point K1, out double[] correct)
        {
            Point cenPt = GetCenterPoint(K0, K1);
            double alpha = GetAzimuth(K0, K1).Rad + PI / 2;
            List<Point> insPois = new List<Point>();
            Point j3 = new Point(), j_3 = new Point();
            for (int i = 5; i >= -5; i--)
            {
                if (i != 0)
                {
                    Point p1 = new Point
                    {
                        X = cenPt.X + i * 5 * Cos(alpha),
                        Y = cenPt.Y + i * 5 * Sin(alpha)
                    };
                    p1.H = GetInsPoiHeight(p1, out _);
                    insPois.Add(p1);
                    if (i == 3)
                    {
                        j3 = p1;
                    }
                    else if (i == -3)
                    {
                        j_3 = p1;
                    }
                }
                else
                {
                    insPois.Add(cenPt);
                }
            }
            correct = new double[]
            {
                alpha,j3.X,j3.Y,j3.H,j_3.X,j_3.Y,j_3.H
            };
            return insPois;
        }
        private List<Point> GetVertcInsertPoints(Point cenPt, double azimuth)
        {
            List<Point> insPois = new List<Point>();
            for (int i = 5; i >= -5; i--)
            {
                if (i != 0)
                {
                    Point p1 = new Point
                    {
                        X = cenPt.X + i * 5 * Cos(azimuth),
                        Y = cenPt.Y + i * 5 * Sin(azimuth)
                    };
                    p1.H = GetInsPoiHeight(p1, out _);
                    insPois.Add(p1);
                }
                else
                {
                    insPois.Add(cenPt);
                }
            }
            return insPois;
        }

        // 路基土石方量计算
        private List<double> GetVolumes(IEnumerable<dynamic> verAllArea)
        {
            List<double> vols = new List<double>();
            IEnumerator<dynamic> enu = verAllArea.GetEnumerator();
            enu.MoveNext();
            while (true)
            {
                dynamic ver1 = enu.Current;
                if (!enu.MoveNext())
                {
                    break;
                }
                dynamic ver2 = enu.Current;
                double vol = (ver1.Area + ver2.Area) / 2 * ver1.Center.GetDistance(ver2.Center);
                vols.Add(vol);
            }
            return vols;
        }
    }
}
