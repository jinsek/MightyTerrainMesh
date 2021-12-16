// -----------------------------------------------------------------------
// <copyright file="Triangle.cs" company="">
// Triangle.NET code by Christian Woltering, http://triangle.codeplex.com/
// </copyright>
// -----------------------------------------------------------------------

namespace TriangleNet.IO
{
    using TriangleNet.Data;
    using TriangleNet.Geometry;

    /// <summary>
    /// Simple triangle class for input.
    /// </summary>
    public class InputTriangle : ITriangle
    {
        internal int[] vertices;
        internal int region;
        internal float area;

        public InputTriangle(int p0, int p1, int p2)
        {
            this.vertices = new int[] { p0, p1, p2 };
        }

        #region Public properties

        /// <summary>
        /// Gets the triangle id.
        /// </summary>
        public int ID
        {
            get { return 0; }
        }

        /// <summary>
        /// Gets the first corners vertex id.
        /// </summary>
        public int P0
        {
            get { return this.vertices[0]; }
        }

        /// <summary>
        /// Gets the seconds corners vertex id.
        /// </summary>
        public int P1
        {
            get { return this.vertices[1]; }
        }

        /// <summary>
        /// Gets the third corners vertex id.
        /// </summary>
        public int P2
        {
            get { return this.vertices[2]; }
        }

        /// <summary>
        /// Gets the specified corners vertex.
        /// </summary>
        public Vertex GetVertex(int index)
        {
            return null; // TODO: throw NotSupportedException?
        }

        public bool SupportsNeighbors
        {
            get { return false; }
        }

        public int N0
        {
            get { return -1; }
        }

        public int N1
        {
            get { return -1; }
        }

        public int N2
        {
            get { return -1; }
        }

        public ITriangle GetNeighbor(int index)
        {
            return null;
        }

        public ISegment GetSegment(int index)
        {
            return null;
        }

        /// <summary>
        /// Gets the triangle area constraint.
        /// </summary>
        public float Area
        {
            get { return area; }
            set { area = value; }
        }

        /// <summary>
        /// Region ID the triangle belongs to.
        /// </summary>
        public int Region
        {
            get { return region; }
            set { region = value; }
        }

        #endregion
    }
}
