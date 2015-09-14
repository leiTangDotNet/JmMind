using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;

namespace JsmMind.Helpers
{
    class CurveTools
    { 
        /// <summary>
        /// 抛物线拟合。
        /// </summary>
        /// <param name="points">实测数据。</param>
        /// <returns>拟合数据。</returns>
        public static List<Point> ParaCurveFitting(List<Point> points)
        {
            if (points.Count < 2) return points;

            //构造线性方程组
            //a + bx + cx^2 = f(x)
            int n = points.Count;
            double sx = (double)points.Sum(c => c.X);
            double sx2 = (double)points.Sum(c => Math.Pow(c.X, 2));
            double sx3 = (double)points.Sum(c => Math.Pow(c.X, 3));
            double sx4 = (double)points.Sum(c => Math.Pow(c.X, 4));
            double sy = (double)points.Sum(c => c.Y);
            double sxy = (double)points.Sum(c => c.X * c.Y);
            double sx2y = (double)points.Sum(c => Math.Pow(c.X, 2) * c.Y);

            //高斯消去法求解方程组
            var result = Gauss(new double[,] { { n, sx, sx2, sy }, { sx, sx2, sx3, sxy }, { sx2, sx3, sx4, sx2y } });

            //拟合后的抛物线数据
            List<Point> fittingPoints = new List<Point>();
            if (result != null && result.Length >= 2)
            {
                double a = result[0];
                double b = result[1];
                double c = result[2];

                for (int x = points[0].X; x <= points[points.Count - 1].X; x += 10)
                {
                    fittingPoints.Add(new Point(x, (int)(a + b * x + c * x * x)));
                }
                //foreach (var pt in points)
                //{
                //    fittingPoints.Add(new Point(pt.X, (int)(a + b * pt.X + c * pt.X * pt.X)));
                //}
            }
            return fittingPoints;
        }

        /// <summary>
        /// 高斯消去法求线性方程组的解。
        /// </summary>
        /// <param name="matrix">增广系数矩阵。</param>
        /// <returns>方程组的解（从低次到高次）。如果无解，则返回空引用；如果有无穷解，则返回空数组。</returns>
        /// <exception cref="ArgumentNullException">如果matrix为空引用，则抛出该异常。</exception>
        static double[] Gauss(double[,] matrix)
        {
            if (matrix == null) throw new ArgumentNullException("matrix");

            //无解
            int cols = matrix.GetLength(1);
            if (cols <= 1) return null;

            //有无穷解
            int rows = matrix.GetLength(0);
            if (rows < cols - 1) return new double[] { };

            //转换为行阶梯
            for (int i = 0; i < rows - 1; i++)
            {
                //选取主元（提高计算精度）
                GaussPivoting(matrix, i, rows, cols);

                //消去一列
                for (int k = i + 1; k < rows; k++)
                {
                    if (matrix[k, i] != 0)
                    {
                        double t = matrix[i, i] / matrix[k, i];
                        for (int j = i; j < cols - 1; j++)
                        {
                            matrix[k, j] = matrix[i, j] - matrix[k, j] * t;
                        }
                        matrix[k, cols - 1] = matrix[i, cols - 1] - matrix[k, cols - 1] * t;
                    }
                }
            }

            //检查秩，判断是否有唯一解
            int rank1 = 0, rank2 = 0;
            for (int i = 0; i < rows; i++)
            {
                bool isZeroRow = true;
                for (int j = i; j < cols - 1; j++)
                {
                    if (matrix[i, j] != 0)
                    {
                        isZeroRow = false;
                        break;
                    }
                }
                if (!isZeroRow) rank1++;
                if (!isZeroRow || matrix[i, cols - 1] != 0) rank2++;
            }

            //如果矩阵的秩小于增广矩阵的秩，则无解
            if (rank1 < rank2) return null;

            //如果矩阵的秩小于方程组的数量，则有无穷解
            if (rank1 < cols - 1) return new double[] { };

            //从底往上依次求解
            double[] result = new double[cols - 1];
            for (int i = rows - 1; i >= 0; i--)
            {
                double y = matrix[i, cols - 1];
                for (int j = i + 1; j < cols - 1; j++)
                {
                    y -= matrix[i, j] * result[j];
                }
                result[i] = matrix[i, i] != 0 ? Math.Round(y / matrix[i, i], 3) : 0;
            }

            return result;
        }
        /// <summary>
        /// 选主元。
        /// </summary>
        /// <param name="matrix">增广系数矩阵。</param>
        /// <param name="i">当前行。</param>
        static void GaussPivoting(double[,] matrix, int i, int rows, int cols)
        {
            //选主元
            int pivotRow = i;
            for (int k = i + 1; k < rows; k++)
            {
                if (Math.Abs(matrix[k, i]) > Math.Abs(matrix[pivotRow, i]))
                    pivotRow = k;
            }

            //交换行
            if (pivotRow != i)
            {
                double tmp = 0;
                for (int j = 0; j < cols; j++)
                {
                    tmp = matrix[i, j];
                    matrix[i, j] = matrix[pivotRow, j];
                    matrix[pivotRow, j] = tmp;
                }
            }

            //除以主元，当前主元变为1
            double pivot = matrix[i, i];
            if (pivot != 0)
            {
                for (int j = i; j < cols; j++)
                    matrix[i, j] /= pivot;
            }
        }
    }
}
