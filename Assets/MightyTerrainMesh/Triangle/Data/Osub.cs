// -----------------------------------------------------------------------
// <copyright file="Osub.cs">
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
    /// An oriented subsegment.
    /// </summary>
    /// <remarks>
    /// Iincludes a pointer to a subsegment and an orientation. The orientation
    /// denotes a side of the edge.  Hence, there are two possible orientations.
    /// By convention, the edge is always directed so that the "side" denoted
    /// is the right side of the edge.
    /// </remarks>
    struct Osub
    {
        public Segment seg;
        public int orient; // Ranges from 0 to 1.

        public override string ToString()
        {
            if (seg == null)
            {
                return "O-TID [null]";
            }
            return String.Format("O-SID {0}", seg.hash);
        }

        #region Osub primitives

        /// <summary>
        /// Reverse the orientation of a subsegment. [sym(ab) -> ba]
        /// </summary>
        /// <remarks>ssym() toggles the orientation of a subsegment.
        /// </remarks>
        public void Sym(ref Osub o2)
        {
            o2.seg = seg;
            o2.orient = 1 - orient;
        }

        /// <summary>
        /// Reverse the orientation of a subsegment. [sym(ab) -> ba]
        /// </summary>
        public void SymSelf()
        {
            orient = 1 - orient;
        }

        /// <summary>
        /// Find adjoining subsegment with the same origin. [pivot(ab) -> a*]
        /// </summary>
        /// <remarks>spivot() finds the other subsegment (from the same segment) 
        /// that shares the same origin.
        /// </remarks>
        public void Pivot(ref Osub o2)
        {
            o2 = seg.subsegs[orient];
            //sdecode(sptr, o2);
        }

        /// <summary>
        /// Find adjoining subsegment with the same origin. [pivot(ab) -> a*]
        /// </summary>
        public void PivotSelf()
        {
            this = seg.subsegs[orient];
            //sdecode(sptr, osub);
        }

        /// <summary>
        /// Find next subsegment in sequence. [next(ab) -> b*]
        /// </summary>
        /// <remarks>snext() finds the next subsegment (from the same segment) in 
        /// sequence; one whose origin is the input subsegment's destination.
        /// </remarks>
        public void Next(ref Osub o2)
        {
            o2 = seg.subsegs[1 - orient];
            //sdecode(sptr, o2);
        }

        /// <summary>
        /// Find next subsegment in sequence. [next(ab) -> b*]
        /// </summary>
        public void NextSelf()
        {
            this = seg.subsegs[1 - orient];
            //sdecode(sptr, osub);
        }

        /// <summary>
        /// Get the origin of a subsegment
        /// </summary>
        public Vertex Org()
        {
            return seg.vertices[orient];
        }

        /// <summary>
        /// Get the destination of a subsegment
        /// </summary>
        public Vertex Dest()
        {
            return seg.vertices[1 - orient];
        }

        /// <summary>
        /// Set the origin or destination of a subsegment.
        /// </summary>
        public void SetOrg(Vertex ptr)
        {
            seg.vertices[orient] = ptr;
        }

        /// <summary>
        /// Set destination of a subsegment.
        /// </summary>
        public void SetDest(Vertex ptr)
        {
            seg.vertices[1 - orient] = ptr;
        }

        /// <summary>
        /// Get the origin of the segment that includes the subsegment.
        /// </summary>
        public Vertex SegOrg()
        {
            return seg.vertices[2 + orient];
        }

        /// <summary>
        /// Get the destination of the segment that includes the subsegment.
        /// </summary>
        public Vertex SegDest()
        {
            return seg.vertices[3 - orient];
        }

        /// <summary>
        /// Set the origin of the segment that includes the subsegment.
        /// </summary>
        public void SetSegOrg(Vertex ptr)
        {
            seg.vertices[2 + orient] = ptr;
        }

        /// <summary>
        /// Set the destination of the segment that includes the subsegment.
        /// </summary>
        public void SetSegDest(Vertex ptr)
        {
            seg.vertices[3 - orient] = ptr;
        }

        /// <summary>
        /// Read a boundary marker.
        /// </summary>
        /// <remarks>Boundary markers are used to hold user-defined tags for 
        /// setting boundary conditions in finite element solvers.</remarks>
        public int Mark()
        {
            return seg.boundary;
        }

        /// <summary>
        /// Set a boundary marker.
        /// </summary>
        public void SetMark(int value)
        {
            seg.boundary = value;
        }

        /// <summary>
        /// Bond two subsegments together. [bond(abc, ba)]
        /// </summary>
        public void Bond(ref Osub o2)
        {
            seg.subsegs[orient] = o2;
            o2.seg.subsegs[o2.orient] = this;
        }

        /// <summary>
        /// Dissolve a subsegment bond (from one side).
        /// </summary>
        /// <remarks>Note that the other subsegment will still think it's 
        /// connected to this subsegment.</remarks>
        public void Dissolve()
        {
            seg.subsegs[orient].seg = Mesh.dummysub;
        }

        /// <summary>
        /// Copy a subsegment.
        /// </summary>
        public void Copy(ref Osub o2)
        {
            o2.seg = seg;
            o2.orient = orient;
        }

        /// <summary>
        /// Test for equality of subsegments.
        /// </summary>
        public bool Equal(Osub o2)
        {
            return ((seg == o2.seg) && (orient == o2.orient));
        }

        /// <summary>
        /// Check a subsegment's deallocation.
        /// </summary>
        public static bool IsDead(Segment sub)
        {
            return sub.subsegs[0].seg == null;
        }

        /// <summary>
        /// Set a subsegment's deallocation.
        /// </summary>
        public static void Kill(Segment sub)
        {
            sub.subsegs[0].seg = null;
            sub.subsegs[1].seg = null;
        }

        /// <summary>
        /// Finds a triangle abutting a subsegment.
        /// </summary>
        public void TriPivot(ref Otri ot)
        {
            ot = seg.triangles[orient];
            //decode(ptr, otri)
        }

        /// <summary>
        /// Dissolve a bond (from the subsegment side).
        /// </summary>
        public void TriDissolve()
        {
            seg.triangles[orient].triangle = Mesh.dummytri;
        }

        #endregion
    }
}
