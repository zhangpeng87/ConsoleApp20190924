using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleApp20190924
{
    /// <summary>
    /// 表示一点坐标。
    /// </summary>
    public class Point
    {
        public double X { get; set; }

        public double Y { get; set; }

        public Point()
        {

        }

        public Point(double x, double y)
        {
            this.X = x;
            this.Y = y;
        }

        public override string ToString()
        {
            return $"X = {X}, Y = {Y}";
        }
    }

    /// <summary>
    /// 表示一条线段。
    /// </summary>
    public class Segment
    {
        public Point start { get; set; }

        public Point end { get; set; }
    }

    /// <summary>
    /// 直线的一般式ABC
    /// </summary>
    public class ABC
    {
        public double A { get; set; }

        public double B { get; set; }

        public double C { get; set; }
    }

    /// <summary>
    /// 推土监控区域类。
    /// </summary>
    public class JianKongQuYu
    {
        public int ZhuangHaoID_Start { get; set; }
        public int ZhuangHaoID_End { get; set; }
        public int XuHao { get; set; }
        public string SHAPE2 { get; set; }

        public string GetSaveValueOfShape2()
        {
            if (string.IsNullOrEmpty(this.SHAPE2)) return "POLYGON EMPTY";

            string strTemp = this.SHAPE2;
            string sWKT = "POLYGON ((";
            string[] xycoords = strTemp.Split(',');

            if (xycoords.Length < 6) return "";

            for (int i = 0; i < xycoords.Length; i += 2)
            {
                if (i == 0)
                {
                    sWKT += xycoords[i] + " " + xycoords[i + 1];
                }
                else
                {
                    sWKT += "," + xycoords[i] + " " + xycoords[i + 1];
                }
            }

            sWKT += "))";

            return sWKT;
        }

        public string GetRawValueOfShape2(string saveValue)
        {
            if (!string.IsNullOrEmpty(saveValue) &&
                !"POLYGON EMPTY".Equals(saveValue.ToUpper()))
            {
                string sWKT = saveValue;
                string sWKT1 = sWKT.Replace("POLYGON ((", "");
                sWKT = sWKT1.Replace("))", "");
                sWKT1 = sWKT.Replace(", ", ",");
                sWKT = sWKT1.Replace(" ", ",");
                return sWKT;
            }
            else
            {
                return "";
            }
        }
    }

    /// <summary>
    /// 桩号类。
    /// </summary>
    public class ZhuangHaoInfo
    {
        public int ID { get; set; }
        public string ZhuangHao { get; set; }
        public double MID_Y { get; set; }
        public double MID_X { get; set; }
        public double L_Y { get; set; }
        public double L_X { get; set; }
        public double R_Y { get; set; }
        public double R_X { get; set; }
    }

    public class ZhuangHaoInfoComp : IComparer<ZhuangHaoInfo>
    {
        public int Compare(ZhuangHaoInfo x, ZhuangHaoInfo y)
        {
            double x_No = Convert.ToDouble(x.ZhuangHao.Split('+')[1]);
            double y_No = Convert.ToDouble(y.ZhuangHao.Split('+')[1]);

            if (x_No > y_No) return 1;
            else if (x_No < y_No) return -1;
            else return 0;
        }
    }

    public class Program
    {
        public static readonly string ConnectionString = @"Data Source=DESKTOP-MU5UCUS\MSSQLSERVER2012;Initial Catalog=YNRoad;Persist Security Info=True;User ID=sa;Password=sw";

        static Program()
        {

        }

        static void Main(string[] args)
        {
            List<ZhuangHaoInfo> zhuangHaoList = new List<ZhuangHaoInfo>();

            // 查询数据库获取桩号数据
            using (SqlConnection connection = new SqlConnection(Program.ConnectionString))
            {
                string cmdText = "SELECT ID, ZhuangHao, MID_Y, MID_X, L_Y, L_X, R_Y, R_X FROM ZhuangHaoInfo WHERE CengHao = 0;";
                SqlCommand command = new SqlCommand(cmdText, connection);

                connection.Open();
                var reader = command.ExecuteReader(CommandBehavior.CloseConnection);
                while (reader.Read())
                {
                    ZhuangHaoInfo hao = new ZhuangHaoInfo()
                    {
                        ID = Convert.ToInt32(reader["ID"]),
                        ZhuangHao = reader["ZhuangHao"].ToString(),
                        MID_Y = Convert.ToDouble(reader["MID_Y"]),
                        MID_X = Convert.ToDouble(reader["MID_X"]),
                        L_Y = Convert.ToDouble(reader["L_Y"]),
                        L_X = Convert.ToDouble(reader["L_X"]),
                        R_Y = Convert.ToDouble(reader["R_Y"]),
                        R_X = Convert.ToDouble(reader["R_X"])
                    };

                    zhuangHaoList.Add(hao);
                }
            }
            // 升序
            var query = zhuangHaoList.OrderBy(e => e, new ZhuangHaoInfoComp());

            // 右顶点、中间顶点、左顶点
            Point rightTop, midTop, leftTop;
            // 右下点、中间底点、左下点
            Point rightBottom, midBottom, leftBottom;
            // 两个三等分点（上面的点、下面的点）
            Point first_SanFen, second_SanFen;

            rightTop = midTop = leftTop = null;
            rightBottom = midBottom = leftBottom = null;
            // 线段
            Segment segment = null;
            
            for (int i = 0; i < query.Count() - 1; i ++)
            {
                ZhuangHaoInfo zhuangTop = query.ElementAt(i);
                ZhuangHaoInfo zhuangBottom = query.ElementAt(i + 1);

                rightTop = new Point() { X = zhuangTop.R_X, Y = zhuangTop.R_Y };
                midTop = new Point() { X = zhuangTop.MID_X, Y = zhuangTop.MID_Y};
                leftTop = new Point() { X = zhuangTop.L_X, Y = zhuangTop.L_Y };

                rightBottom = new Point() { X = zhuangBottom.R_X, Y = zhuangBottom.R_Y };
                midBottom = new Point() { X = zhuangBottom.MID_X, Y = zhuangBottom.MID_Y};
                leftBottom = new Point() { X = zhuangBottom.L_X, Y = zhuangBottom.L_Y }; 

                // 计算两个三等分点坐标
                segment = new Segment { start = midTop, end = midBottom };
                SanDengFen(segment, out first_SanFen, out second_SanFen);

                // 计算两条垂线
                segment = new Segment { start = midTop, end = second_SanFen };
                ABC first_ChuiXian = ABC_ZhongChuiXian(segment);

                segment = new Segment { start = first_SanFen, end = midBottom };
                ABC second_ChuiXian = ABC_ZhongChuiXian(segment);

                // 计算两条边线
                ABC rightLine = ABC_ZhiXian(rightTop, rightBottom);
                ABC leftLine = ABC_ZhiXian(leftTop, leftBottom);

                // 计算垂线与边线交点
                Point first_ChuiXian_right = GetIntersectPointofLines(rightLine, first_ChuiXian);       // 第一条垂线与右边线的交点
                Point first_ChuiXian_left = GetIntersectPointofLines(leftLine, first_ChuiXian);         // 第一条垂线与左边线的交点
                Point second_ChuiXian_right = GetIntersectPointofLines(rightLine, second_ChuiXian);     // 第二条垂线与右边线的交点
                Point second_ChuiXian_left = GetIntersectPointofLines(leftLine, second_ChuiXian);       // 第二条垂线与右边线的交点
                // 分为三个监控区域
                JianKongQuYu[] yus = new JianKongQuYu[]
                {
                    new JianKongQuYu{ ZhuangHaoID_Start = zhuangTop.ID, ZhuangHaoID_End = zhuangBottom.ID, XuHao = 1, SHAPE2 = Format2POLYGON(leftTop, midTop, rightTop, first_ChuiXian_right, first_ChuiXian_left, leftTop) },
                    new JianKongQuYu{ ZhuangHaoID_Start = zhuangTop.ID, ZhuangHaoID_End = zhuangBottom.ID, XuHao = 2, SHAPE2 = Format2POLYGON(first_ChuiXian_left, first_ChuiXian_right, second_ChuiXian_right, second_ChuiXian_left, first_ChuiXian_left) },
                    new JianKongQuYu{ ZhuangHaoID_Start = zhuangTop.ID, ZhuangHaoID_End = zhuangBottom.ID, XuHao = 3, SHAPE2 = Format2POLYGON(second_ChuiXian_left, second_ChuiXian_right, rightBottom, midBottom, leftBottom, second_ChuiXian_left) }
                };

                using (SqlConnection connection = new SqlConnection(ConnectionString))
                {
                    string cmdText = @"INSERT INTO JianKongQuYuGeo (ZhuangHaoID_Start, ZhuangHaoID_End, XuHao, SHAPE2) VALUES (@ZhuangHaoID_Start, @ZhuangHaoID_End, @XuHao, @SHAPE2);";

                    SqlCommand command = new SqlCommand(cmdText, connection);
                    command.Parameters.Add("@ZhuangHaoID_Start", SqlDbType.Int);
                    command.Parameters.Add("@ZhuangHaoID_End", SqlDbType.Int);
                    command.Parameters.Add("@XuHao", SqlDbType.Int);
                    command.Parameters.Add("@SHAPE2", SqlDbType.NVarChar);

                    connection.Open();
                    foreach (JianKongQuYu item in yus)
                    {
                        command.Parameters["@ZhuangHaoID_Start"].Value = item.ZhuangHaoID_Start;
                        command.Parameters["@ZhuangHaoID_End"].Value = item.ZhuangHaoID_End;
                        command.Parameters["@XuHao"].Value = item.XuHao;
                        command.Parameters["@SHAPE2"].Value = item.SHAPE2;

                        int r = command.ExecuteNonQuery();

                        Console.WriteLine($"向数据库表中插入{r}行数据。桩号：{zhuangTop.ZhuangHao} - {zhuangBottom.ZhuangHao}，序号：{item.XuHao}。");
                    }
                    
                }
            }
        }

        /// <summary>
        /// 将线段三等分，返回两分点坐标。
        /// </summary>
        /// <param name="p1"></param>
        /// <param name="p2"></param>
        /// <param name="first"></param>
        /// <param name="second"></param>
        public static void SanDengFen(Segment segment, out Point first, out Point second)
        {
            first = second = null;

            double delta_x = (segment.start.X - segment.end.X) / 3;
            double delta_y = (segment.start.Y - segment.end.Y) / 3;

            first = new Point()
            {
                X = segment.start.X - delta_x,
                Y = segment.start.Y - delta_y
            };

            second = new Point()
            {
                X = segment.end.X + delta_x,
                Y = segment.end.Y + delta_y
            };
        }

        /// <summary>
        /// 已知直线上的两点坐标，返回ABC
        /// </summary>
        /// <param name="first"></param>
        /// <param name="second"></param>
        /// <returns></returns>
        public static ABC ABC_ZhiXian(Point first, Point second)
        {
            ABC result = new ABC
            {
                A = second.Y - first.Y,
                B = first.X - second.X,
                C = second.X * first.Y - first.X * second.Y
            };

            return result;
        }

        /// <summary>
        /// 已知一条线段，返回中垂线ABC
        /// </summary>
        /// <param name="segment"></param>
        /// <returns></returns>
        public static ABC ABC_ZhongChuiXian(Segment segment)
        {
            // 此线段的中点坐标
            Point midPoint = new Point()
            {
                X = (segment.start.X + segment.end.X) / 2,
                Y = (segment.start.Y + segment.end.Y) / 2
            };

            // 计算中垂线斜率
            double k = default(double);

            double k1 = (segment.start.Y - segment.end.Y) / (segment.start.X - segment.end.X);
            k = -1 / k1;

            // 点斜式计算
            // kx-y+(-kx0+y0)＝0
            ABC result = new ABC()
            {
                A = k,
                B = -1,
                C = midPoint.Y - k * midPoint.X
            };

            return result;
        }

        /// <summary>
        /// 已知两条直线的ABC，返回其交点坐标
        /// </summary>
        /// <param name="zhiXian1"></param>
        /// <param name="zhiXian2"></param>
        /// <returns></returns>
        public static Point GetIntersectPointofLines(ABC zhiXian1, ABC zhiXian2)
        {
            Point result = null;

            double m = zhiXian1.A * zhiXian2.B - zhiXian2.A * zhiXian1.B;
            if (m == 0) return result;

            result = new Point()
            {
                X = (zhiXian2.C * zhiXian1.B - zhiXian1.C * zhiXian2.B) / m,
                Y = (zhiXian1.C * zhiXian2.A - zhiXian2.C * zhiXian1.A) / m
            };

            return result;
        }

        /// <summary>
        /// 返回POLYGON格式，例如 POLYGON((1 1, 3 3, 3 1, 1 1))
        /// </summary>
        /// <param name="points"></param>
        /// <returns></returns>
        public static string Format2POLYGON(params Point[] points)
        {
            string result = "POLYGON EMPTY";
            if (points == null || points.Length == 0) return result;

            string sub = string.Empty;
            foreach (Point item in points)
            {
                if (!string.IsNullOrEmpty(sub))
                {
                    sub += $", {item.X} {item.Y}";
                }
                else
                {
                    sub += $"{item.X} {item.Y}";
                }
            }

            result = $"POLYGON(({sub}))";

            return result;
        }
    }
}
