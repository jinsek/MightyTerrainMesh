// -----------------------------------------------------------------------
// <copyright file="Triangle.cs" company="">
// Original Triangle code by Jonathan Richard Shewchuk, http://www.cs.cmu.edu/~quake/triangle.html
// Triangle.NET code by Christian Woltering, http://triangle.codeplex.com/
// </copyright>
// -----------------------------------------------------------------------

namespace TriangleNet.Data
{
    using System;
    using TriangleNet.Geometry;

    /// <summary>
    /// The triangle data structure.
    /// </summary>
    /// <remarks>
    /// Each triangle contains three pointers to adjoining triangles, plus three 
    /// pointers to vertices, plus three pointers to subsegments (declared below;
    /// these pointers are usually 'dummysub'). It may or may not also contain 
    /// user-defined attributes and/or a floating-point "area constraint".
    /// </remarks>
    public class Triangle : ITriangle
    {
        // Hash for dictionary. Will be set by mesh instance.
        internal int hash;

        // The ID is only used for mesh output.
        internal int id;

        internal Otri[] neighbors;
        internal Vertex[] vertices;
        internal Osub[] subsegs;
        internal int region;
        internal float area;
        internal bool infected;

        public Triangle()
        {
            // Initialize the three adjoining triangles to be "outer space".
            neighbors = new Otri[3];
            neighbors[0].triangle = Mesh.dummytri;
            neighbors[1].triangle = Mesh.dummytri;
            neighbors[2].triangle = Mesh.dummytri;

            // Three NULL vertices.
            vertices = new Vertex[3];

            // TODO: if (Behavior.UseSegments)
            {
                // Initialize the three adjoining subsegments to be the
                // omnipresent subsegment.
                subsegs = new Osub[3];
                subsegs[0].seg = Mesh.dummysub;
                subsegs[1].seg = Mesh.dummysub;
                subsegs[2].seg = Mesh.dummysub;
            }

            // TODO:
            //if (Behavior.VarArea)
            //{
            //    area = -1.0;
            //}
        }

        #region Public properties

        /// <summary>
        /// Gets the triangle id.
        /// </summary>
        public int ID
        {
            get { return this.id; }
        }

        /// <summary>
        /// Gets the first corners vertex id.
        /// </summary>
        public int P0
        {
            get { return this.vertices[0] == null ? -1 : this.vertices[0].id; }
        }

        /// <summary>
        /// Gets the seconds corners vertex id.
        /// </summary>
        public int P1
        {
            get { return this.vertices[1] == null ? -1 : this.vertices[1].id; }
        }

        /// <summary>
        /// Gets the specified corners vertex.
        /// </summary>
        public Vertex GetVertex(int index)
        {
            return this.vertices[index]; // TODO: Check range?
        }

        /// <summary>
        /// Gets the third corners vertex id.
        /// </summary>
        public int P2
        {
            get { return this.vertices[2] == null ? -1 : this.vertices[2].id; }
        }

        public bool SupportsNeighbors
        {
            get { return true; }
        }

        /// <summary>
        /// Gets the first neighbors id.
        /// </summary>
        public int N0
        {
            get { return this.neighbors[0].triangle.id; }
        }

        /// <summary>
        /// Gets the second neighbors id.
        /// </summary>
        public int N1
        {
            get { return this.neighbors[1].triangle.id; }
        }

        /// <summary>
        /// Gets the third neighbors id.
        /// </summary>
        public int N2
        {
            get { return this.neighbors[2].triangle.id; }
        }

        /// <summary>
        /// Gets a triangles' neighbor.
        /// </summary>
        /// <param name="index">The neighbor index (0, 1 or 2).</param>
        /// <returns>The neigbbor opposite of vertex with given index.</returns>
        public ITriangle GetNeighbor(int index)
        {
            return neighbors[index].triangle == Mesh.dummytri ? null : neighbors[index].triangle;
        }

        /// <summary>
        /// Gets a triangles segment.
        /// </summary>
        /// <param name="index">The vertex index (0, 1 or 2).</param>
        /// <returns>The segment opposite of vertex with given index.</returns>
        public ISegment GetSegment(int index)
        {
            return subsegs[index].seg == Mesh.dummysub ? null : subsegs[index].seg;
        }

        /// <summary>
        /// Gets the triangle area constraint.
        /// </summary>
        public float Area
        {
            get { return this.area; }
            set { this.area = value; }
        }

        /// <summary>
        /// Region ID the triangle belongs to.
        /// </summary>
        public int Region
        {
            get { return this.region; }
        }

        #endregion

        public override int GetHashCode()
        {
            return this.hash;
        }

        public override string ToString()
        {
            return String.Format("TID {0}", hash);
        }
    }
}
