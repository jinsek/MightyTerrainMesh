// -----------------------------------------------------------------------
// <copyright file="Otri.cs">
// Original Triangle code by Jonathan Richard Shewchuk, http://www.cs.cmu.edu/~quake/triangle.html
// Triangle.NET code by Christian Woltering, http://triangle.codeplex.com/
// </copyright>
// -----------------------------------------------------------------------

namespace TriangleNet.Data
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;

    /// <summary>
    /// An oriented triangle.
    /// </summary>
    /// <remarks>
    /// Includes a pointer to a triangle and orientation.
    /// The orientation denotes an edge of the triangle. Hence, there are
    /// three possible orientations. By convention, each edge always points
    /// counterclockwise about the corresponding triangle.
    /// </remarks>
    struct Otri
    {
        public Triangle triangle;
        public int orient; // Ranges from 0 to 2.

        public override string ToString()
        {
            if (triangle == null)
            {
                return "O-TID [null]";
            }
            return String.Format("O-TID {0}", triangle.hash);
        }

        #region Otri primitives

        // For fast access
        static readonly int[] plus1Mod3 = { 1, 2, 0 };
        static readonly int[] minus1Mod3 = { 2, 0, 1 };

        // The following handle manipulation primitives are all described by Guibas
        // and Stolfi. However, Guibas and Stolfi use an edge-based data structure,
        // whereas I use a triangle-based data structure.

        /// <summary>
        /// Find the abutting triangle; same edge. [sym(abc) -> ba*]
        /// </summary>
        /// <remarks>
        /// Note that the edge direction is necessarily reversed, because the handle specified 
        /// by an oriented triangle is directed counterclockwise around the triangle.
        /// </remarks>
        public void Sym(ref Otri o2)
        {
            //o2 = tri.triangles[orient];
            // decode(ptr, otri2);

            o2.triangle = triangle.neighbors[orient].triangle;
            o2.orient = triangle.neighbors[orient].orient;
        }

        /// <summary>
        /// Find the abutting triangle; same edge. [sym(abc) -> ba*]
        /// </summary>
        public void SymSelf()
        {
            //this = tri.triangles[orient];
            // decode(ptr, otri);

            int tmp = orient;
            orient = triangle.neighbors[tmp].orient;
            triangle = triangle.neighbors[tmp].triangle;
        }
        // lnext() finds the next edge (counterclockwise) of a triangle.

        /// <summary>
        /// Find the next edge (counterclockwise) of a triangle. [lnext(abc) -> bca]
        /// </summary>
        public void Lnext(ref Otri o2)
        {
            o2.triangle = triangle;
            o2.orient = plus1Mod3[orient];
        }

        /// <summary>
        /// Find the next edge (counterclockwise) of a triangle. [lnext(abc) -> bca]
        /// </summary>
        public void LnextSelf()
        {
            orient = plus1Mod3[orient];
        }

        /// <summary>
        /// Find the previous edge (clockwise) of a triangle. [lprev(abc) -> cab]
        /// </summary>
        public void Lprev(ref Otri o2)
        {
            o2.triangle = triangle;
            o2.orient = minus1Mod3[orient];
        }

        /// <summary>
        /// Find the previous edge (clockwise) of a triangle. [lprev(abc) -> cab]
        /// </summary>
        public void LprevSelf()
        {
            orient = minus1Mod3[orient];
        }

        /// <summary>
        /// Find the next edge counterclockwise with the same origin. [onext(abc) -> ac*]
        /// </summary>
        /// <remarks>onext() spins counterclockwise around a vertex; that is, it finds 
        /// the next edge with the same origin in the counterclockwise direction. This
        /// edge is part of a different triangle.
        /// </remarks>
        public void Onext(ref Otri o2)
        {
            //Lprev(ref o2);
            o2.triangle = triangle;
            o2.orient = minus1Mod3[orient];

            //o2.SymSelf();
            int tmp = o2.orient;
            o2.orient = o2.triangle.neighbors[tmp].orient;
            o2.triangle = o2.triangle.neighbors[tmp].triangle;
        }

        /// <summary>
        /// Find the next edge counterclockwise with the same origin. [onext(abc) -> ac*]
        /// </summary>
        public void OnextSelf()
        {
            //LprevSelf();
            orient = minus1Mod3[orient];

            //SymSelf();
            int tmp = orient;
            orient = triangle.neighbors[tmp].orient;
            triangle = triangle.neighbors[tmp].triangle;
        }

        /// <summary>
        /// Find the next edge clockwise with the same origin. [oprev(abc) -> a*b]
        /// </summary>
        /// <remarks>oprev() spins clockwise around a vertex; that is, it finds the 
        /// next edge with the same origin in the clockwise direction.  This edge is 
        /// part of a different triangle.
        /// </remarks>
        public void Oprev(ref Otri o2)
        {
            //Sym(ref o2);
            o2.triangle = triangle.neighbors[orient].triangle;
            o2.orient = triangle.neighbors[orient].orient;

            //o2.LnextSelf();
            o2.orient = plus1Mod3[o2.orient];
        }

        /// <summary>
        /// Find the next edge clockwise with the same origin. [oprev(abc) -> a*b]
        /// </summary>
        public void OprevSelf()
        {
            //SymSelf();
            int tmp = orient;
            orient = triangle.neighbors[tmp].orient;
            triangle = triangle.neighbors[tmp].triangle;

            //LnextSelf();
            orient = plus1Mod3[orient];
        }

        /// <summary>
        /// Find the next edge counterclockwise with the same destination. [dnext(abc) -> *ba]
        /// </summary>
        /// <remarks>dnext() spins counterclockwise around a vertex; that is, it finds 
        /// the next edge with the same destination in the counterclockwise direction.
        /// This edge is part of a different triangle.
        /// </remarks>
        public void Dnext(ref Otri o2)
        {
            //Sym(ref o2);
            o2.triangle = triangle.neighbors[orient].triangle;
            o2.orient = triangle.neighbors[orient].orient;

            //o2.LprevSelf();
            o2.orient = minus1Mod3[o2.orient];
        }

        /// <summary>
        /// Find the next edge counterclockwise with the same destination. [dnext(abc) -> *ba]
        /// </summary>
        public void DnextSelf()
        {
            //SymSelf();
            int tmp = orient;
            orient = triangle.neighbors[tmp].orient;
            triangle = triangle.neighbors[tmp].triangle;

            //LprevSelf();
            orient = minus1Mod3[orient];
        }

        /// <summary>
        /// Find the next edge clockwise with the same destination. [dprev(abc) -> cb*]
        /// </summary>
        /// <remarks>dprev() spins clockwise around a vertex; that is, it finds the 
        /// next edge with the same destination in the clockwise direction. This edge 
        /// is part of a different triangle.
        /// </remarks>
        public void Dprev(ref Otri o2)
        {
            //Lnext(ref o2);
            o2.triangle = triangle;
            o2.orient = plus1Mod3[orient];

            //o2.SymSelf();
            int tmp = o2.orient;
            o2.orient = o2.triangle.neighbors[tmp].orient;
            o2.triangle = o2.triangle.neighbors[tmp].triangle;
        }

        /// <summary>
        /// Find the next edge clockwise with the same destination. [dprev(abc) -> cb*]
        /// </summary>
        public void DprevSelf()
        {
            //LnextSelf();
            orient = plus1Mod3[orient];

            //SymSelf();
            int tmp = orient;
            orient = triangle.neighbors[tmp].orient;
            triangle = triangle.neighbors[tmp].triangle;
        }

        /// <summary>
        /// Find the next edge (counterclockwise) of the adjacent triangle. [rnext(abc) -> *a*]
        /// </summary>
        /// <remarks>rnext() moves one edge counterclockwise about the adjacent 
        /// triangle. (It's best understood by reading Guibas and Stolfi. It 
        /// involves changing triangles twice.)
        /// </remarks>
        public void Rnext(ref Otri o2)
        {
            //Sym(ref o2);
            o2.triangle = triangle.neighbors[orient].triangle;
            o2.orient = triangle.neighbors[orient].orient;

            //o2.LnextSelf();
            o2.orient = plus1Mod3[o2.orient];

            //o2.SymSelf();
            int tmp = o2.orient;
            o2.orient = o2.triangle.neighbors[tmp].orient;
            o2.triangle = o2.triangle.neighbors[tmp].triangle;
        }

        /// <summary>
        /// Find the next edge (counterclockwise) of the adjacent triangle. [rnext(abc) -> *a*]
        /// </summary>
        public void RnextSelf()
        {
            //SymSelf();
            int tmp = orient;
            orient = triangle.neighbors[tmp].orient;
            triangle = triangle.neighbors[tmp].triangle;

            //LnextSelf();
            orient = plus1Mod3[orient];

            //SymSelf();
            tmp = orient;
            orient = triangle.neighbors[tmp].orient;
            triangle = triangle.neighbors[tmp].triangle;
        }

        /// <summary>
        /// Find the previous edge (clockwise) of the adjacent triangle. [rprev(abc) -> b**]
        /// </summary>
        /// <remarks>rprev() moves one edge clockwise about the adjacent triangle.
        /// (It's best understood by reading Guibas and Stolfi.  It involves
        /// changing triangles twice.)
        /// </remarks>
        public void Rprev(ref Otri o2)
        {
            //Sym(ref o2);
            o2.triangle = triangle.neighbors[orient].triangle;
            o2.orient = triangle.neighbors[orient].orient;

            //o2.LprevSelf();
            o2.orient = minus1Mod3[o2.orient];

            //o2.SymSelf();
            int tmp = o2.orient;
            o2.orient = o2.triangle.neighbors[tmp].orient;
            o2.triangle = o2.triangle.neighbors[tmp].triangle;
        }

        /// <summary>
        /// Find the previous edge (clockwise) of the adjacent triangle. [rprev(abc) -> b**]
        /// </summary>
        public void RprevSelf()
        {
            //SymSelf();
            int tmp = orient;
            orient = triangle.neighbors[tmp].orient;
            triangle = triangle.neighbors[tmp].triangle;

            //LprevSelf();
            orient = minus1Mod3[orient];

            //SymSelf();
            tmp = orient;
            orient = triangle.neighbors[tmp].orient;
            triangle = triangle.neighbors[tmp].triangle;
        }

        /// <summary>
        /// Origin [org(abc) -> a]
        /// </summary>
        public Vertex Org()
        {
            return triangle.vertices[plus1Mod3[orient]];
        }

        /// <summary>
        /// Destination [dest(abc) -> b]
        /// </summary>
        public Vertex Dest()
        {
            return triangle.vertices[minus1Mod3[orient]];
        }

        /// <summary>
        /// Apex [apex(abc) -> c]
        /// </summary>
        public Vertex Apex()
        {
            return triangle.vertices[orient];
        }

        /// <summary>
        /// Set Origin
        /// </summary>
        public void SetOrg(Vertex ptr)
        {
            triangle.vertices[plus1Mod3[orient]] = ptr;
        }

        /// <summary>
        /// Set Destination
        /// </summary>
        public void SetDest(Vertex ptr)
        {
            triangle.vertices[minus1Mod3[orient]] = ptr;
        }

        /// <summary>
        /// Set Apex
        /// </summary>
        public void SetApex(Vertex ptr)
        {
            triangle.vertices[orient] = ptr;
        }

        /// <summary>
        /// Bond two triangles together at the resepective handles. [bond(abc, bad)]
        /// </summary>
        public void Bond(ref Otri o2)
        {
            //triangle.neighbors[orient]= o2;
            //o2.triangle.neighbors[o2.orient] = this;

            triangle.neighbors[orient].triangle = o2.triangle;
            triangle.neighbors[orient].orient = o2.orient;

            o2.triangle.neighbors[o2.orient].triangle = this.triangle;
            o2.triangle.neighbors[o2.orient].orient = this.orient;
        }

        /// <summary>
        /// Dissolve a bond (from one side).  
        /// </summary>
        /// <remarks>Note that the other triangle will still think it's connected to 
        /// this triangle. Usually, however, the other triangle is being deleted 
        /// entirely, or bonded to another triangle, so it doesn't matter.
        /// </remarks>
        public void Dissolve()
        {
            triangle.neighbors[orient].triangle = Mesh.dummytri;
            triangle.neighbors[orient].orient = 0;
        }

        /// <summary>
        /// Copy an oriented triangle.
        /// </summary>
        public void Copy(ref Otri o2)
        {
            o2.triangle = triangle;
            o2.orient = orient;
        }

        /// <summary>
        /// Test for equality of oriented triangles.
        /// </summary>
        public bool Equal(Otri o2)
        {
            return ((triangle == o2.triangle) && (orient == o2.orient));
        }

        /// <summary>
        /// Infect a triangle with the virus.
        /// </summary>
        public void Infect()
        {
            triangle.infected = true;
        }

        /// <summary>
        /// Cure a triangle from the virus.
        /// </summary>
        public void Uninfect()
        {
            triangle.infected = false;
        }

        /// <summary>
        /// Test a triangle for viral infection.
        /// </summary>
        public bool IsInfected()
        {
            return triangle.infected;
        }

        /// <summary>
        /// Check a triangle's deallocation.
        /// </summary>
        public static bool IsDead(Triangle tria)
        {
            return tria.neighbors[0].triangle == null;
        }

        /// <summary>
        /// Set a triangle's deallocation.
        /// </summary>
        public static void Kill(Triangle tria)
        {
            tria.neighbors[0].triangle = null;
            tria.neighbors[2].triangle = null;
        }

        /// <summary>
        /// Finds a subsegment abutting a triangle.
        /// </summary>
        public void SegPivot(ref Osub os)
        {
            os = triangle.subsegs[orient];
            //sdecode(sptr, osub)
        }

        /// <summary>
        /// Bond a triangle to a subsegment.
        /// </summary>
        public void SegBond(ref Osub os)
        {
            triangle.subsegs[orient] = os;
            os.seg.triangles[os.orient] = this;
        }

        /// <summary>
        /// Dissolve a bond (from the triangle side).
        /// </summary>
        public void SegDissolve()
        {
            triangle.subsegs[orient].seg = Mesh.dummysub;
        }

        #endregion
    }
}
