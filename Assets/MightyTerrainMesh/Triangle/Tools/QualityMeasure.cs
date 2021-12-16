// -----------------------------------------------------------------------
// <copyright file="QualityMeasure.cs" company="">
// Original Matlab code by John Burkardt, Florida State University
// Triangle.NET code by Christian Woltering, http://triangle.codeplex.com/
// </copyright>
// -----------------------------------------------------------------------

namespace TriangleNet.Tools
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using TriangleNet.Geometry;

    /// <summary>
    /// Provides mesh quality information.
    /// </summary>
    /// <remarks>
    /// Given a triangle abc with points A (ax, ay), B (bx, by), C (cx, cy).
    /// 
    /// The side lengths are given as
    ///   a = sqrt((cx - bx)^2 + (cy - by)^2) -- side BC opposite of A
    ///   b = sqrt((cx - ax)^2 + (cy - ay)^2) -- side CA opposite of B
    ///   c = sqrt((ax - bx)^2 + (ay - by)^2) -- side AB opposite of C
    ///   
    /// The angles are given as
    ///   ang_a = acos((b^2 + c^2 - a^2)  / (2 * b * c)) -- angle at A
    ///   ang_b = acos((c^2 + a^2 - b^2)  / (2 * c * a)) -- angle at B
    ///   ang_c = acos((a^2 + b^2 - c^2)  / (2 * a * b)) -- angle at C
    ///   
    /// The semiperimeter is given as
    ///   s = (a + b + c) / 2
    ///   
    /// The area is given as
    ///   D = abs(ax * (by - cy) + bx * (cy - ay) + cx * (ay - by)) / 2
    ///     = sqrt(s * (s - a) * (s - b) * (s - c))
    ///      
    /// The inradius is given as
    ///   r = D / s
    ///   
    /// The circumradius is given as
    ///   R = a * b * c / (4 * D)
    /// 
    /// The altitudes are given as
    ///   alt_a = 2 * D / a -- altitude above side a
    ///   alt_b = 2 * D / b -- altitude above side b
    ///   alt_c = 2 * D / c -- altitude above side c
    /// 
    /// The aspect ratio may be given as the ratio of the longest to the
    /// shortest edge or, more commonly as the ratio of the circumradius 
    /// to twice the inradius
    ///   ar = R / (2 * r)
    ///      = a * b * c / (8 * (s - a) * (s - b) * (s - c))
    ///      = a * b * c / ((b + c - a) * (c + a - b) * (a + b - c))
    /// </remarks>
    public class QualityMeasure
    {
        AreaMeasure areaMeasure;
        AlphaMeasure alphaMeasure;
        Q_Measure qMeasure;

        Mesh mesh;

        public QualityMeasure()
        {
            areaMeasure = new AreaMeasure();
            alphaMeasure = new AlphaMeasure();
            qMeasure = new Q_Measure();
        }

        #region Public properties

        /// <summary>
        /// Minimum triangle area.
        /// </summary>
        public float AreaMinimum
        {
            get { return areaMeasure.area_min; }
        }

        /// <summary>
        /// Maximum triangle area.
        /// </summary>
        public float AreaMaximum
        {
            get { return areaMeasure.area_max; }
        }

        /// <summary>
        /// Ratio of maximum and minimum triangle area.
        /// </summary>
        public float AreaRatio
        {
            get { return areaMeasure.area_max / areaMeasure.area_min; }
        }

        /// <summary>
        /// Smallest angle.
        /// </summary>
        public float AlphaMinimum
        {
            get { return alphaMeasure.alpha_min; }
        }

        /// <summary>
        /// Maximum smallest angle.
        /// </summary>
        public float AlphaMaximum
        {
            get { return alphaMeasure.alpha_max; }
        }

        /// <summary>
        /// Average angle.
        /// </summary>
        public float AlphaAverage
        {
            get { return alphaMeasure.alpha_ave; }
        }

        /// <summary>
        /// Average angle weighted by area.
        /// </summary>
        public float AlphaArea
        {
            get { return alphaMeasure.alpha_area; }
        }

        /// <summary>
        /// Smallest aspect ratio.
        /// </summary>
        public float Q_Minimum
        {
            get { return qMeasure.q_min; }
        }

        /// <summary>
        /// Largest aspect ratio.
        /// </summary>
        public float Q_Maximum
        {
            get { return qMeasure.q_max; }
        }

        /// <summary>
        /// Average aspect ratio.
        /// </summary>
        public float Q_Average
        {
            get { return qMeasure.q_ave; }
        }

        /// <summary>
        /// Average aspect ratio weighted by area.
        /// </summary>
        public float Q_Area
        {
            get { return qMeasure.q_area; }
        }

        #endregion

        public void Update(Mesh mesh)
        {
            this.mesh = mesh;

            // Reset all measures.
            areaMeasure.Reset();
            alphaMeasure.Reset();
            qMeasure.Reset();

            Compute();
        }

        private void Compute()
        {
            Point a, b, c;
            float ab, bc, ca;
            float lx, ly;
            float area;

            int n = 0;

            foreach (var tri in mesh.triangles.Values)
            {
                n++;

                a = tri.vertices[0];
                b = tri.vertices[1];
                c = tri.vertices[2];

                lx = a.x - b.x;
                ly = a.y - b.y;
                ab = UnityEngine.Mathf.Sqrt(lx * lx + ly * ly);
                lx = b.x - c.x;
                ly = b.y - c.y;
                bc = UnityEngine.Mathf.Sqrt(lx * lx + ly * ly);
                lx = c.x - a.x;
                ly = c.y - a.y;
                ca = UnityEngine.Mathf.Sqrt(lx * lx + ly * ly);

                area = areaMeasure.Measure(a, b, c);
                alphaMeasure.Measure(ab, bc, ca, area);
                qMeasure.Measure(ab, bc, ca, area);
            }

            // Normalize measures
            alphaMeasure.Normalize(n, areaMeasure.area_total);
            qMeasure.Normalize(n, areaMeasure.area_total);
        }

        /// <summary>
        /// Determines the bandwidth of the coefficient matrix.
        /// </summary>
        /// <returns>Bandwidth of the coefficient matrix.</returns>
        /// <remarks>
        /// The quantity computed here is the "geometric" bandwidth determined
        /// by the finite element mesh alone.
        ///
        /// If a single finite element variable is associated with each node
        /// of the mesh, and if the nodes and variables are numbered in the
        /// same way, then the geometric bandwidth is the same as the bandwidth
        /// of a typical finite element matrix.
        ///
        /// The bandwidth M is defined in terms of the lower and upper bandwidths:
        ///
        ///   M = ML + 1 + MU
        ///
        /// where 
        ///
        ///   ML = maximum distance from any diagonal entry to a nonzero
        ///   entry in the same row, but earlier column,
        ///
        ///   MU = maximum distance from any diagonal entry to a nonzero
        ///   entry in the same row, but later column.
        ///
        /// Because the finite element node adjacency relationship is symmetric,
        /// we are guaranteed that ML = MU.
        /// </remarks>
        public int Bandwidth()
        {
            if (mesh == null) return 0;

            // Lower and upper bandwidth of the matrix
            int ml = 0, mu = 0;

            int gi, gj;

            foreach (var tri in mesh.triangles.Values)
            {
                for (int j = 0; j < 3; j++)
                {
                    gi = tri.GetVertex(j).id;

                    for (int k = 0; k < 3; k++)
                    {
                        gj = tri.GetVertex(k).id;

                        mu = UnityEngine.Mathf.Max(mu, gj - gi);
                        ml = UnityEngine.Mathf.Max(ml, gi - gj);
                    }
                }
            }

            return ml + 1 + mu;
        }

        class AreaMeasure
        {
            // Minimum area
            public float area_min = float.MaxValue;
            // Maximum area
            public float area_max = -float.MaxValue;
            // Total area of geometry
            public float area_total = 0;
            // Nmber of triangles with zero area
            public int area_zero = 0;

            /// <summary>
            /// Reset all values.
            /// </summary>
            public void Reset()
            {
                area_min = float.MaxValue;
                area_max = -float.MaxValue;
                area_total = 0;
                area_zero = 0;
            }

            /// <summary>
            /// Compute the area of given triangle.
            /// </summary>
            /// <param name="a">Triangle corner a.</param>
            /// <param name="b">Triangle corner b.</param>
            /// <param name="c">Triangle corner c.</param>
            /// <returns>Triangle area.</returns>
            public float Measure(Point a, Point b, Point c)
            {
                float area = 0.5f * UnityEngine.Mathf.Abs(a.x * (b.y - c.y) + b.x * (c.y - a.y) + c.x * (a.y - b.y));

                area_min = UnityEngine.Mathf.Min(area_min, area);
                area_max = UnityEngine.Mathf.Max(area_max, area);
                area_total += area;

                if (area == 0.0f)
                {
                    area_zero = area_zero + 1;
                }

                return area;
            }
        }

        /// <summary>
        /// The alpha measure determines the triangulated pointset quality.
        /// </summary>
        /// <remarks>
        /// The alpha measure evaluates the uniformity of the shapes of the triangles
        /// defined by a triangulated pointset.
        ///
        /// We compute the minimum angle among all the triangles in the triangulated
        /// dataset and divide by the maximum possible value (which, in degrees,
        /// is 60). The best possible value is 1, and the worst 0. A good
        /// triangulation should have an alpha score close to 1.
        /// </remarks>
        class AlphaMeasure
        {
            // Minimum value over all triangles
            public float alpha_min;
            // Maximum value over all triangles
            public float alpha_max;
            // Value averaged over all triangles
            public float alpha_ave;
            // Value averaged over all triangles and weighted by area
            public float alpha_area;

            /// <summary>
            /// Reset all values.
            /// </summary>
            public void Reset()
            {
                alpha_min = float.MaxValue;
                alpha_max = -float.MaxValue;
                alpha_ave = 0;
                alpha_area = 0;
            }

            float acos(float c)
            {
                if (c <= -1.0f)
                {
                    return UnityEngine.Mathf.PI;
                }
                else if (1.0f <= c)
                {
                    return 0.0f;
                }
                else
                {
                    return UnityEngine.Mathf.Acos(c);
                }
            }

            /// <summary>
            /// Compute q value of given triangle.
            /// </summary>
            /// <param name="ab">Side length ab.</param>
            /// <param name="bc">Side length bc.</param>
            /// <param name="ca">Side length ca.</param>
            /// <param name="area">Triangle area.</param>
            /// <returns></returns>
            public float Measure(float ab, float bc, float ca, float area)
            {
                float alpha = float.MaxValue;

                float ab2 = ab * ab;
                float bc2 = bc * bc;
                float ca2 = ca * ca;

                float a_angle;
                float b_angle;
                float c_angle;

                // Take care of a ridiculous special case.
                if (ab == 0.0f && bc == 0.0f && ca == 0.0f)
                {
                    a_angle = 2.0f * UnityEngine.Mathf.PI / 3.0f;
                    b_angle = 2.0f * UnityEngine.Mathf.PI / 3.0f;
                    c_angle = 2.0f * UnityEngine.Mathf.PI / 3.0f;
                }
                else
                {
                    if (ca == 0.0f || ab == 0.0f)
                    {
                        a_angle = UnityEngine.Mathf.PI;
                    }
                    else
                    {
                        a_angle = acos((ca2 + ab2 - bc2) / (2.0f * ca * ab));
                    }

                    if (ab == 0.0f || bc == 0.0f)
                    {
                        b_angle = UnityEngine.Mathf.PI;
                    }
                    else
                    {
                        b_angle = acos((ab2 + bc2 - ca2) / (2.0f * ab * bc));
                    }

                    if (bc == 0.0f || ca == 0.0f)
                    {
                        c_angle = UnityEngine.Mathf.PI;
                    }
                    else
                    {
                        c_angle = acos((bc2 + ca2 - ab2) / (2.0f * bc * ca));
                    }
                }

                alpha = UnityEngine.Mathf.Min(alpha, a_angle);
                alpha = UnityEngine.Mathf.Min(alpha, b_angle);
                alpha = UnityEngine.Mathf.Min(alpha, c_angle);

                // Normalize angle from [0,pi/3] radians into qualities in [0,1].
                alpha = alpha * 3.0f / UnityEngine.Mathf.PI;

                alpha_ave += alpha;
                alpha_area += area * alpha;

                alpha_min = UnityEngine.Mathf.Min(alpha, alpha_min);
                alpha_max = UnityEngine.Mathf.Max(alpha, alpha_max);

                return alpha;
            }

            /// <summary>
            /// Normalize values.
            /// </summary>
            public void Normalize(int n, float area_total)
            {
                if (n > 0)
                {
                    alpha_ave /= n;
                }
                else
                {
                    alpha_ave = 0.0f;
                }

                if (0.0f < area_total)
                {
                    alpha_area /= area_total;
                }
                else
                {
                    alpha_area = 0.0f;
                }
            }
        }

        /// <summary>
        /// The Q measure determines the triangulated pointset quality.
        /// </summary>
        /// <remarks>
        /// The Q measure evaluates the uniformity of the shapes of the triangles
        /// defined by a triangulated pointset. It uses the aspect ratio
        ///
        ///    2 * (incircle radius) / (circumcircle radius)
        ///
        /// In an ideally regular mesh, all triangles would have the same
        /// equilateral shape, for which Q = 1. A good mesh would have
        /// 0.5 &lt; Q.
        /// </remarks>
        class Q_Measure
        {
            // Minimum value over all triangles
            public float q_min;
            // Maximum value over all triangles
            public float q_max;
            // Average value
            public float q_ave;
            // Average value weighted by the area of each triangle
            public float q_area;

            /// <summary>
            /// Reset all values.
            /// </summary>
            public void Reset()
            {
                q_min = float.MaxValue;
                q_max = -float.MaxValue;
                q_ave = 0;
                q_area = 0;
            }

            /// <summary>
            /// Compute q value of given triangle.
            /// </summary>
            /// <param name="ab">Side length ab.</param>
            /// <param name="bc">Side length bc.</param>
            /// <param name="ca">Side length ca.</param>
            /// <param name="area">Triangle area.</param>
            /// <returns></returns>
            public float Measure(float ab, float bc, float ca, float area)
            {
                float q = (bc + ca - ab) * (ca + ab - bc) * (ab + bc - ca) / (ab * bc * ca);

                q_min = UnityEngine.Mathf.Min(q_min, q);
                q_max = UnityEngine.Mathf.Max(q_max, q);

                q_ave += q;
                q_area += q * area;

                return q;
            }

            /// <summary>
            /// Normalize values.
            /// </summary>
            public void Normalize(int n, float area_total)
            {
                if (n > 0)
                {
                    q_ave /= n;
                }
                else
                {
                    q_ave = 0.0f;
                }

                if (area_total > 0.0f)
                {
                    q_area /= area_total;
                }
                else
                {
                    q_area = 0.0f;
                }
            }
        }
    }
}
