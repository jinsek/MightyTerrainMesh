// -----------------------------------------------------------------------
// <copyright file="InputGeometry.cs" company="">
// Triangle.NET code by Christian Woltering, http://triangle.codeplex.com/
// </copyright>
// -----------------------------------------------------------------------

namespace TriangleNet.Geometry
{
    using System;
    using System.Collections.Generic;
    using TriangleNet.Data;

    /// <summary>
    /// The input geometry which will be triangulated. May represent a 
    /// pointset or a planar straight line graph.
    /// </summary>
    public class InputGeometry
    {
        internal List<Vertex> points;
        internal List<Edge> segments;
        internal List<Point> holes;
        internal List<RegionPointer> regions;

        BoundingBox bounds;

        // Used to check consitent use of point attributes.
        private int pointAttributes = -1;

        /// <summary>
        /// Initializes a new instance of the <see cref="InputGeometry" /> class.
        /// </summary>
        public InputGeometry()
            : this(3)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="InputGeometry" /> class. 
        /// The point list will be initialized with a given capacity.
        /// </summary>
        /// <param name="capacity">Point list capacity.</param>
        public InputGeometry(int capacity)
        {
            points = new List<Vertex>(capacity);
            segments = new List<Edge>();
            holes = new List<Point>();
            regions = new List<RegionPointer>();

            bounds = new BoundingBox();

            pointAttributes = -1;
        }

        /// <summary>
        /// Gets the bounding box of the input geometry.
        /// </summary>
        public BoundingBox Bounds
        {
            get { return bounds; }
        }

        /// <summary>
        /// Indicates, whether the geometry should be treated as a PSLG.
        /// </summary>
        public bool HasSegments
        {
            get { return segments.Count > 0; }
        }

        /// <summary>
        /// Gets the number of points.
        /// </summary>
        public int Count
        {
            get { return points.Count; }
        }

        /// <summary>
        /// Gets the list of input points.
        /// </summary>
        public IEnumerable<Point> Points
        {
            get { return (IEnumerable<Point>) points; }
        }

        /// <summary>
        /// Gets the list of input segments.
        /// </summary>
        public ICollection<Edge> Segments
        {
            get { return segments; }
        }

        /// <summary>
        /// Gets the list of input holes.
        /// </summary>
        public ICollection<Point> Holes
        {
            get { return holes; }
        }

        /// <summary>
        /// Gets the list of regions.
        /// </summary>
        public ICollection<RegionPointer> Regions
        {
            get { return regions; }
        }

        /// <summary>
        /// Clear input geometry.
        /// </summary>
        public void Clear()
        {
            points.Clear();
            segments.Clear();
            holes.Clear();
            regions.Clear();

            pointAttributes = -1;
        }

        /// <summary>
        /// Adds a point to the geometry.
        /// </summary>
        /// <param name="x">X coordinate.</param>
        /// <param name="y">Y coordinate.</param>
        public void AddPoint(float x, float y)
        {
            AddPoint(x, y, 0);
        }

        /// <summary>
        /// Adds a point to the geometry.
        /// </summary>
        /// <param name="x">X coordinate.</param>
        /// <param name="y">Y coordinate.</param>
        /// <param name="boundary">Boundary marker.</param>
        public void AddPoint(float x, float y, int boundary)
        {
            points.Add(new Vertex(x, y, boundary));

            bounds.Update(x, y);
        }

        /// <summary>
        /// Adds a point to the geometry.
        /// </summary>
        /// <param name="x">X coordinate.</param>
        /// <param name="y">Y coordinate.</param>
        /// <param name="boundary">Boundary marker.</param>
        /// <param name="attribute">Point attribute.</param>
        public void AddPoint(float x, float y, int boundary, float attribute)
        {
            AddPoint(x, y, 0, new float[] { attribute });
        }

        /// <summary>
        /// Adds a point to the geometry.
        /// </summary>
        /// <param name="x">X coordinate.</param>
        /// <param name="y">Y coordinate.</param>
        /// <param name="boundary">Boundary marker.</param>
        /// <param name="attribs">Point attributes.</param>
        public void AddPoint(float x, float y, int boundary, float[] attribs)
        {
            if (pointAttributes < 0)
            {
                pointAttributes = attribs == null ? 0 : attribs.Length;
            }
            else if (attribs == null && pointAttributes > 0)
            {
                throw new ArgumentException("Inconsitent use of point attributes.");
            }
            else if (attribs != null && pointAttributes != attribs.Length)
            {
                throw new ArgumentException("Inconsitent use of point attributes.");
            }

            points.Add(new Vertex(x, y, boundary) { attributes = attribs });

            bounds.Update(x, y);
        }

        /// <summary>
        /// Adds a hole location to the geometry.
        /// </summary>
        /// <param name="x">X coordinate of the hole.</param>
        /// <param name="y">Y coordinate of the hole.</param>
        public void AddHole(float x, float y)
        {
            holes.Add(new Point(x, y));
        }

        /// <summary>
        /// Adds a hole location to the geometry.
        /// </summary>
        /// <param name="x">X coordinate of the hole.</param>
        /// <param name="y">Y coordinate of the hole.</param>
        /// <param name="id">The region id.</param>
        public void AddRegion(float x, float y, int id)
        {
            regions.Add(new RegionPointer(x, y, id));
        }

        /// <summary>
        /// Adds a segment to the geometry.
        /// </summary>
        /// <param name="p0">First endpoint.</param>
        /// <param name="p1">Second endpoint.</param>
        public void AddSegment(int p0, int p1)
        {
            AddSegment(p0, p1, 0);
        }

        /// <summary>
        /// Adds a segment to the geometry.
        /// </summary>
        /// <param name="p0">First endpoint.</param>
        /// <param name="p1">Second endpoint.</param>
        /// <param name="boundary">Segment marker.</param>
        public void AddSegment(int p0, int p1, int boundary)
        {
            if (p0 == p1 || p0 < 0 || p1 < 0)
            {
                throw new NotSupportedException("Invalid endpoints.");
            }

            segments.Add(new Edge(p0, p1, boundary));
        }
    }
}
