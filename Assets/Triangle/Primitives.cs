// -----------------------------------------------------------------------
// <copyright file="Primitives.cs">
// Original Triangle code by Jonathan Richard Shewchuk, http://www.cs.cmu.edu/~quake/triangle.html
// Triangle.NET code by Christian Woltering, http://triangle.codeplex.com/
// </copyright>
// -----------------------------------------------------------------------

namespace TriangleNet
{
    using System;
    using TriangleNet.Data;
    using TriangleNet.Geometry;
    using TriangleNet.Tools;

    /// <summary>
    /// Provides some primitives regularly used in computational geometry.
    /// </summary>
    public static class Primitives
    {
        static float splitter;       // Used to split float factors for exact multiplication.
        static float epsilon;        // Floating-point machine epsilon.
        //static float resulterrbound;
        static float ccwerrboundA; // ccwerrboundB, ccwerrboundC;
        static float iccerrboundA; // iccerrboundB, iccerrboundC;

        /// <summary>
        /// Initialize the variables used for exact arithmetic.
        /// </summary>
        /// <remarks>
        /// 'epsilon' is the largest power of two such that 1.0 + epsilon = 1.0 in
        /// floating-point arithmetic. 'epsilon' bounds the relative roundoff
        /// error. It is used for floating-point error analysis.
        ///
        /// 'splitter' is used to split floating-point numbers into two half-
        /// length significands for exact multiplication.
        ///
        /// I imagine that a highly optimizing compiler might be too smart for its
        /// own good, and somehow cause this routine to fail, if it pretends that
        /// floating-point arithmetic is too much like real arithmetic.
        ///
        /// Don't change this routine unless you fully understand it.
        /// </remarks>
        public static void ExactInit()
        {
            float half;
            float check, lastcheck;
            bool every_other;

            every_other = true;
            half = 0.5f;
            epsilon = 1.0f;
            splitter = 1.0f;
            check = 1.0f;
            // Repeatedly divide 'epsilon' by two until it is too small to add to
            // one without causing roundoff. (Also check if the sum is equal to
            // the previous sum, for machines that round up instead of using exact
            // rounding.  Not that these routines will work on such machines.)
            do
            {
                lastcheck = check;
                epsilon *= half;
                if (every_other)
                {
                    splitter *= 2.0f;
                }
                every_other = !every_other;
                check = 1.0f + epsilon;
            } while ((check != 1.0f) && (check != lastcheck));
            splitter += 1.0f;
            // Error bounds for orientation and incircle tests.
            //resulterrbound = (3.0 + 8.0 * epsilon) * epsilon;
            ccwerrboundA = (3.0f + 16.0f * epsilon) * epsilon;
            //ccwerrboundB = (2.0 + 12.0 * epsilon) * epsilon;
            //ccwerrboundC = (9.0 + 64.0 * epsilon) * epsilon * epsilon;
            iccerrboundA = (10.0f + 96.0f * epsilon) * epsilon;
            //iccerrboundB = (4.0 + 48.0 * epsilon) * epsilon;
            //iccerrboundC = (44.0 + 576.0 * epsilon) * epsilon * epsilon;
        }

        /// <summary>
        /// Check, if the three points appear in counterclockwise order. The result is 
        /// also a rough approximation of twice the signed area of the triangle defined 
        /// by the three points.
        /// </summary>
        /// <param name="pa">Point a.</param>
        /// <param name="pb">Point b.</param>
        /// <param name="pc">Point c.</param>
        /// <returns>Return a positive value if the points pa, pb, and pc occur in 
        /// counterclockwise order; a negative value if they occur in clockwise order; 
        /// and zero if they are collinear.</returns>
        /// <remarks>
        /// Uses exact arithmetic if necessary to ensure a correct answer. The
        /// result returned is the determinant of a matrix. This determinant is
        /// computed adaptively, in the sense that exact arithmetic is used only to
        /// the degree it is needed to ensure that the returned value has the
        /// correct sign. Hence, this function is usually quite fast, but will run
        /// more slowly when the input points are collinear or nearly so.
        ///
        /// See Robust Predicates paper for details.
        /// </remarks>
        public static float CounterClockwise(Point pa, Point pb, Point pc)
        {
            float detleft, detright, det;
            float detsum, errbound;

            Statistic.CounterClockwiseCount++;

            detleft = (pa.x - pc.x) * (pb.y - pc.y);
            detright = (pa.y - pc.y) * (pb.x - pc.x);
            det = detleft - detright;

            if (Behavior.NoExact)
            {
                return det;
            }

            if (detleft > 0.0)
            {
                if (detright <= 0.0)
                {
                    return det;
                }
                else
                {
                    detsum = detleft + detright;
                }
            }
            else if (detleft < 0.0)
            {
                if (detright >= 0.0)
                {
                    return det;
                }
                else
                {
                    detsum = -detleft - detright;
                }
            }
            else
            {
                return det;
            }

            errbound = ccwerrboundA * detsum;
            if ((det >= errbound) || (-det >= errbound))
            {
                return det;
            }

            return (float)CounterClockwiseDecimal(pa, pb, pc);
        }

        private static decimal CounterClockwiseDecimal(Point pa, Point pb, Point pc)
        {
            Statistic.CounterClockwiseCountDecimal++;

            decimal detleft, detright, det, detsum;

            detleft = ((decimal)pa.x - (decimal)pc.x) * ((decimal)pb.y - (decimal)pc.y);
            detright = ((decimal)pa.y - (decimal)pc.y) * ((decimal)pb.x - (decimal)pc.x);
            det = detleft - detright;

            if (detleft > 0.0m)
            {
                if (detright <= 0.0m)
                {
                    return det;
                }
                else
                {
                    detsum = detleft + detright;
                }
            }
            else if (detleft < 0.0m)
            {
                if (detright >= 0.0m)
                {
                    return det;
                }
                else
                {
                    detsum = -detleft - detright;
                }
            }

            return det;
        }

        /// <summary>
        /// Check if the point pd lies inside the circle passing through pa, pb, and pc. The 
        /// points pa, pb, and pc must be in counterclockwise order, or the sign of the result 
        /// will be reversed.
        /// </summary>
        /// <param name="pa">Point a.</param>
        /// <param name="pb">Point b.</param>
        /// <param name="pc">Point c.</param>
        /// <param name="pd">Point d.</param>
        /// <returns>Return a positive value if the point pd lies inside the circle passing through 
        /// pa, pb, and pc; a negative value if it lies outside; and zero if the four points 
        /// are cocircular.</returns>
        /// <remarks>
        /// Uses exact arithmetic if necessary to ensure a correct answer.  The
        /// result returned is the determinant of a matrix.  This determinant is
        /// computed adaptively, in the sense that exact arithmetic is used only to
        /// the degree it is needed to ensure that the returned value has the
        /// correct sign.  Hence, this function is usually quite fast, but will run
        /// more slowly when the input points are cocircular or nearly so.
        ///
        /// See Robust Predicates paper for details.
        /// </remarks>
        public static float InCircle(Point pa, Point pb, Point pc, Point pd)
        {
            float adx, bdx, cdx, ady, bdy, cdy;
            float bdxcdy, cdxbdy, cdxady, adxcdy, adxbdy, bdxady;
            float alift, blift, clift;
            float det;
            float permanent, errbound;

            Statistic.InCircleCount++;

            adx = pa.x - pd.x;
            bdx = pb.x - pd.x;
            cdx = pc.x - pd.x;
            ady = pa.y - pd.y;
            bdy = pb.y - pd.y;
            cdy = pc.y - pd.y;

            bdxcdy = bdx * cdy;
            cdxbdy = cdx * bdy;
            alift = adx * adx + ady * ady;

            cdxady = cdx * ady;
            adxcdy = adx * cdy;
            blift = bdx * bdx + bdy * bdy;

            adxbdy = adx * bdy;
            bdxady = bdx * ady;
            clift = cdx * cdx + cdy * cdy;

            det = alift * (bdxcdy - cdxbdy)
                + blift * (cdxady - adxcdy)
                + clift * (adxbdy - bdxady);

            if (Behavior.NoExact)
            {
                return det;
            }

            permanent = (UnityEngine.Mathf.Abs(bdxcdy) + UnityEngine.Mathf.Abs(cdxbdy)) * alift
                      + (UnityEngine.Mathf.Abs(cdxady) + UnityEngine.Mathf.Abs(adxcdy)) * blift
                      + (UnityEngine.Mathf.Abs(adxbdy) + UnityEngine.Mathf.Abs(bdxady)) * clift;
            errbound = iccerrboundA * permanent;
            if ((det > errbound) || (-det > errbound))
            {
                return det;
            }

            return (float)InCircleDecimal(pa, pb, pc, pd);
        }

        private static decimal InCircleDecimal(Point pa, Point pb, Point pc, Point pd)
        {
            Statistic.InCircleCountDecimal++;

            decimal adx, bdx, cdx, ady, bdy, cdy;
            decimal bdxcdy, cdxbdy, cdxady, adxcdy, adxbdy, bdxady;
            decimal alift, blift, clift;

            adx = (decimal)pa.x - (decimal)pd.x;
            bdx = (decimal)pb.x - (decimal)pd.x;
            cdx = (decimal)pc.x - (decimal)pd.x;
            ady = (decimal)pa.y - (decimal)pd.y;
            bdy = (decimal)pb.y - (decimal)pd.y;
            cdy = (decimal)pc.y - (decimal)pd.y;

            bdxcdy = bdx * cdy;
            cdxbdy = cdx * bdy;
            alift = adx * adx + ady * ady;

            cdxady = cdx * ady;
            adxcdy = adx * cdy;
            blift = bdx * bdx + bdy * bdy;

            adxbdy = adx * bdy;
            bdxady = bdx * ady;
            clift = cdx * cdx + cdy * cdy;

            return alift * (bdxcdy - cdxbdy)
                + blift * (cdxady - adxcdy)
                + clift * (adxbdy - bdxady);
        }

        /// <summary>
        /// Return a positive value if the point pd is incompatible with the circle 
        /// or plane passing through pa, pb, and pc (meaning that pd is inside the 
        /// circle or below the plane); a negative value if it is compatible; and 
        /// zero if the four points are cocircular/coplanar. The points pa, pb, and 
        /// pc must be in counterclockwise order, or the sign of the result will be 
        /// reversed.
        /// </summary>
        /// <param name="pa">Point a.</param>
        /// <param name="pb">Point b.</param>
        /// <param name="pc">Point c.</param>
        /// <param name="pd">Point d.</param>
        /// <returns>Return a positive value if the point pd lies inside the circle passing through 
        /// pa, pb, and pc; a negative value if it lies outside; and zero if the four points 
        /// are cocircular.</returns>
        public static float NonRegular(Point pa, Point pb, Point pc, Point pd)
        {
            return InCircle(pa, pb, pc, pd);
        }

        /// <summary>
        /// Find the circumcenter of a triangle.
        /// </summary>
        /// <param name="torg">Triangle point.</param>
        /// <param name="tdest">Triangle point.</param>
        /// <param name="tapex">Triangle point.</param>
        /// <param name="xi">Relative coordinate of new location.</param>
        /// <param name="eta">Relative coordinate of new location.</param>
        /// <param name="offconstant">Off-center constant.</param>
        /// <returns>Coordinates of the circumcenter (or off-center)</returns>
        public static Point FindCircumcenter(Point torg, Point tdest, Point tapex,
                              ref float xi, ref float eta, float offconstant)
        {
            float xdo, ydo, xao, yao;
            float dodist, aodist, dadist;
            float denominator;
            float dx, dy, dxoff, dyoff;

            Statistic.CircumcenterCount++;

            // Compute the circumcenter of the triangle.
            xdo = tdest.x - torg.x;
            ydo = tdest.y - torg.y;
            xao = tapex.x - torg.x;
            yao = tapex.y - torg.y;
            dodist = xdo * xdo + ydo * ydo;
            aodist = xao * xao + yao * yao;
            dadist = (tdest.x - tapex.x) * (tdest.x - tapex.x) +
                     (tdest.y - tapex.y) * (tdest.y - tapex.y);

            if (Behavior.NoExact)
            {
                denominator = 0.5f / (xdo * yao - xao * ydo);
            }
            else
            {
                // Use the counterclockwise() routine to ensure a positive (and
                // reasonably accurate) result, avoiding any possibility of
                // division by zero.
                denominator = 0.5f / CounterClockwise(tdest, tapex, torg);
                // Don't count the above as an orientation test.
                Statistic.CounterClockwiseCount--;
            }

            dx = (yao * dodist - ydo * aodist) * denominator;
            dy = (xdo * aodist - xao * dodist) * denominator;

            // Find the (squared) length of the triangle's shortest edge.  This
            // serves as a conservative estimate of the insertion radius of the
            // circumcenter's parent. The estimate is used to ensure that
            // the algorithm terminates even if very small angles appear in
            // the input PSLG.
            if ((dodist < aodist) && (dodist < dadist))
            {
                if (offconstant > 0.0f)
                {
                    // Find the position of the off-center, as described by Alper Ungor.
                    dxoff = 0.5f * xdo - offconstant * ydo;
                    dyoff = 0.5f * ydo + offconstant * xdo;
                    // If the off-center is closer to the origin than the
                    // circumcenter, use the off-center instead.
                    if (dxoff * dxoff + dyoff * dyoff < dx * dx + dy * dy)
                    {
                        dx = dxoff;
                        dy = dyoff;
                    }
                }
            }
            else if (aodist < dadist)
            {
                if (offconstant > 0.0f)
                {
                    dxoff = 0.5f * xao + offconstant * yao;
                    dyoff = 0.5f * yao - offconstant * xao;
                    // If the off-center is closer to the origin than the
                    // circumcenter, use the off-center instead.
                    if (dxoff * dxoff + dyoff * dyoff < dx * dx + dy * dy)
                    {
                        dx = dxoff;
                        dy = dyoff;
                    }
                }
            }
            else
            {
                if (offconstant > 0.0f)
                {
                    dxoff = 0.5f * (tapex.x - tdest.x) - offconstant * (tapex.y - tdest.y);
                    dyoff = 0.5f * (tapex.y - tdest.y) + offconstant * (tapex.x - tdest.x);
                    // If the off-center is closer to the destination than the
                    // circumcenter, use the off-center instead.
                    if (dxoff * dxoff + dyoff * dyoff <
                        (dx - xdo) * (dx - xdo) + (dy - ydo) * (dy - ydo))
                    {
                        dx = xdo + dxoff;
                        dy = ydo + dyoff;
                    }
                }
            }

            // To interpolate vertex attributes for the new vertex inserted at
            // the circumcenter, define a coordinate system with a xi-axis,
            // directed from the triangle's origin to its destination, and
            // an eta-axis, directed from its origin to its apex.
            // Calculate the xi and eta coordinates of the circumcenter.
            xi = (yao * dx - xao * dy) * (2.0f * denominator);
            eta = (xdo * dy - ydo * dx) * (2.0f * denominator);

            return new Point(torg.x + dx, torg.y + dy);
        }

        /// <summary>
        /// Find the circumcenter of a triangle.
        /// </summary>
        /// <param name="torg">Triangle point.</param>
        /// <param name="tdest">Triangle point.</param>
        /// <param name="tapex">Triangle point.</param>
        /// <param name="xi">Relative coordinate of new location.</param>
        /// <param name="eta">Relative coordinate of new location.</param>
        /// <returns>Coordinates of the circumcenter</returns>
        /// <remarks>
        /// The result is returned both in terms of x-y coordinates and xi-eta
        /// (barycentric) coordinates. The xi-eta coordinate system is defined in
        /// terms of the triangle: the origin of the triangle is the origin of the
        /// coordinate system; the destination of the triangle is one unit along the
        /// xi axis; and the apex of the triangle is one unit along the eta axis.
        /// This procedure also returns the square of the length of the triangle's
        /// shortest edge.
        /// </remarks>
        public static Point FindCircumcenter(Point torg, Point tdest, Point tapex,
                              ref float xi, ref float eta)
        {
            float xdo, ydo, xao, yao;
            float dodist, aodist;
            float denominator;
            float dx, dy;

            Statistic.CircumcenterCount++;

            // Compute the circumcenter of the triangle.
            xdo = tdest.x - torg.x;
            ydo = tdest.y - torg.y;
            xao = tapex.x - torg.x;
            yao = tapex.y - torg.y;
            dodist = xdo * xdo + ydo * ydo;
            aodist = xao * xao + yao * yao;

            if (Behavior.NoExact)
            {
                denominator = 0.5f / (xdo * yao - xao * ydo);
            }
            else
            {
                // Use the counterclockwise() routine to ensure a positive (and
                // reasonably accurate) result, avoiding any possibility of
                // division by zero.
                denominator = 0.5f / CounterClockwise(tdest, tapex, torg);
                // Don't count the above as an orientation test.
                Statistic.CounterClockwiseCount--;
            }

            dx = (yao * dodist - ydo * aodist) * denominator;
            dy = (xdo * aodist - xao * dodist) * denominator;

            // To interpolate vertex attributes for the new vertex inserted at
            // the circumcenter, define a coordinate system with a xi-axis,
            // directed from the triangle's origin to its destination, and
            // an eta-axis, directed from its origin to its apex.
            // Calculate the xi and eta coordinates of the circumcenter.
            xi = (yao * dx - xao * dy) * (2.0f * denominator);
            eta = (xdo * dy - ydo * dx) * (2.0f * denominator);

            return new Point(torg.x + dx, torg.y + dy);
        }
    }
}
