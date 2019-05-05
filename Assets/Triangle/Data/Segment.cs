// -----------------------------------------------------------------------
// <copyright file="Segment.cs" company="">
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
    using TriangleNet.Geometry;

    /// <summary>
    /// The subsegment data structure.
    /// </summary>
    /// <remarks>
    /// Each subsegment contains two pointers to adjoining subsegments, plus
    /// four pointers to vertices, plus two pointers to adjoining triangles,
    /// plus one boundary marker.
    /// </remarks>
    public class Segment : ISegment
    {
        // Hash for dictionary. Will be set by mesh instance.
        internal int hash;

        internal Osub[] subsegs;
        internal Vertex[] vertices;
        internal Otri[] triangles;
        internal int boundary;

        public Segment()
        {
            // Initialize the two adjoining subsegments to be the omnipresent
            // subsegment.
            subsegs = new Osub[2];
            subsegs[0].seg = Mesh.dummysub;
            subsegs[1].seg = Mesh.dummysub;

            // Four NULL vertices.
            vertices = new Vertex[4];

            // Initialize the two adjoining triangles to be "outer space."
            triangles = new Otri[2];
            triangles[0].triangle = Mesh.dummytri;
            triangles[1].triangle = Mesh.dummytri;

            // Set the boundary marker to zero.
            boundary = 0;
        }

        #region Public properties

        /// <summary>
        /// Gets the first endpoints vertex id.
        /// </summary>
        public int P0
        {
            get { return this.vertices[0].id; }
        }

        /// <summary>
        /// Gets the seconds endpoints vertex id.
        /// </summary>
        public int P1
        {
            get { return this.vertices[1].id; }
        }

        /// <summary>
        /// Gets the segment boundary mark.
        /// </summary>
        public int Boundary
        {
            get { return this.boundary; }
        }

        #endregion

        /// <summary>
        /// Gets the segments endpoint.
        /// </summary>
        public Vertex GetVertex(int index)
        {
            return this.vertices[index]; // TODO: Check range?
        }

        /// <summary>
        /// Gets an adjoining triangle.
        /// </summary>
        public ITriangle GetTriangle(int index)
        {
            return triangles[index].triangle == Mesh.dummytri ? null : triangles[index].triangle;
        }

        public override int GetHashCode()
        {
            return this.hash;
        }

        public override string ToString()
        {
            return String.Format("SID {0}", hash);
        }
    }
}
