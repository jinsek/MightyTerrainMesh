// -----------------------------------------------------------------------
// <copyright file="Vertex.cs" company="">
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
    /// The vertex data structure.
    /// </summary>
    public class Vertex : Point
    {
        // Hash for dictionary. Will be set by mesh instance.
        internal int hash;

        internal VertexType type;
        internal Otri tri;

        /// <summary>
        /// Initializes a new instance of the <see cref="Vertex" /> class.
        /// </summary>
        public Vertex()
            : this(0, 0, 0, 0)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Vertex" /> class.
        /// </summary>
        /// <param name="x">The x coordinate of the vertex.</param>
        /// <param name="y">The y coordinate of the vertex.</param>
        public Vertex(float x, float y)
            : this(x, y, 0, 0)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Vertex" /> class.
        /// </summary>
        /// <param name="x">The x coordinate of the vertex.</param>
        /// <param name="y">The y coordinate of the vertex.</param>
        /// <param name="mark">The boundary mark.</param>
        public Vertex(float x, float y, int mark)
            : this(x, y, mark, 0)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Vertex" /> class.
        /// </summary>
        /// <param name="x">The x coordinate of the vertex.</param>
        /// <param name="y">The y coordinate of the vertex.</param>
        /// <param name="mark">The boundary mark.</param>
        /// <param name="attribs">The number of point attributes.</param>
        public Vertex(float x, float y, int mark, int attribs)
            : base(x, y, mark)
        {
            this.type = VertexType.InputVertex;

            if (attribs > 0)
            {
                this.attributes = new float[attribs];
            }
        }

        #region Public properties

        /// <summary>
        /// Gets the vertex type.
        /// </summary>
        public VertexType Type
        {
            get { return this.type; }
        }

        /// <summary>
        /// Gets the specified coordinate of the vertex.
        /// </summary>
        /// <param name="i">Coordinate index.</param>
        /// <returns>X coordinate, if index is 0, Y coordinate, if index is 1.</returns>
        public float this[int i]
        {
            get
            {
                if (i == 0)
                {
                    return x;
                }

                if (i == 1)
                {
                    return y;
                }

                throw new ArgumentOutOfRangeException("Index must be 0 or 1.");
            }
        }

        #endregion

        public override int GetHashCode()
        {
            return this.hash;
        }
    }
}
