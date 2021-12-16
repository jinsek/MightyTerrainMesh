// -----------------------------------------------------------------------
// <copyright file="VoronoiRegion.cs" company="">
// TODO: Update copyright text.
// </copyright>
// -----------------------------------------------------------------------

namespace TriangleNet.Tools
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using TriangleNet.Geometry;
    using TriangleNet.Data;

    /// <summary>
    /// Represents a region in the Voronoi diagram.
    /// </summary>
    public class VoronoiRegion
    {
        int id;
        Point generator;
        List<Point> vertices;
        bool bounded;

        /// <summary>
        /// Gets the Voronoi region id (which is the same as the generators vertex id).
        /// </summary>
        public int ID
        {
            get { return id; }
        }

        /// <summary>
        /// Gets the Voronoi regions generator.
        /// </summary>
        public Point Generator
        {
            get { return generator; }
        }

        /// <summary>
        /// Gets the Voronoi vertices on the regions boundary.
        /// </summary>
        public ICollection<Point> Vertices
        {
            get { return vertices; }
        }

        /// <summary>
        /// Gets or sets whether the Voronoi region is bounded.
        /// </summary>
        public bool Bounded
        {
            get { return bounded; }
            set { bounded = value; }
        }

        public VoronoiRegion(Vertex generator)
        {
            this.id = generator.id;
            this.generator = generator;
            this.vertices = new List<Point>();
            this.bounded = true;
        }

        public void Add(Point point)
        {
            this.vertices.Add(point);
        }

        public void Add(List<Point> points)
        {
            this.vertices.AddRange(points);
        }

        public override string ToString()
        {
            return String.Format("R-ID {0}", id);
        }
    }
}
