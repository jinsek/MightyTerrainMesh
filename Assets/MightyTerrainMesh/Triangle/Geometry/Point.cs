// -----------------------------------------------------------------------
// <copyright file="Point.cs" company="">
// Triangle.NET code by Christian Woltering, http://triangle.codeplex.com/
// </copyright>
// -----------------------------------------------------------------------

namespace TriangleNet.Geometry
{
    using UnityEngine;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;

    /// <summary>
    /// Represents a 2D point.
    /// </summary>
    public class Point : IComparable<Point>, IEquatable<Point>
    {
        internal int id;
        public Vector2 coordinates;
        public float x
        {
            get
            {
                return coordinates.x;
            }
            set
            {
                coordinates.x = value;
            }
        }

        public float y
        {
            get
            {
                return coordinates.y;
            }
            set
            {
                coordinates.y = value;
            }
        }

        internal int mark;
        internal float[] attributes;

        public Point()
            : this(0, 0, 0)
        {
        }

        public Point(float x, float y)
            : this(x, y, 0)
        {
        }

        public Point(float x, float y, int mark)
        {
            this.coordinates.x = x;
            this.coordinates.y = y;
            this.mark = mark;
        }

        public Point(Vector2 coordinates)
        {
            this.coordinates = coordinates;
        }

        #region Public properties

        /// <summary>
        /// Gets the vertex id.
        /// </summary>
        public int ID
        {
            get { return this.id; }
        }

        /// <summary>
        /// Gets the vertex x coordinate.
        /// </summary>
        public float X
        {
            get { return this.x; }
        }

        /// <summary>
        /// Gets the vertex y coordinate.
        /// </summary>
        public float Y
        {
            get { return this.y; }
        }

        /// <summary>
        /// Gets the vertex boundary mark.
        /// </summary>
        public int Boundary
        {
            get { return this.mark; }
        }

        /// <summary>
        /// Gets the vertex attributes (may be null).
        /// </summary>
        public float[] Attributes
        {
            get { return this.attributes; }
        }

        #endregion

        #region Operator overloading / overriding Equals

        // Compare "Guidelines for Overriding Equals() and Operator =="
        // http://msdn.microsoft.com/en-us/library/ms173147.aspx

        public static bool operator ==(Point a, Point b)
        {
            // If both are null, or both are same instance, return true.
            if (System.Object.ReferenceEquals(a, b))
            {
                return true;
            }

            // If one is null, but not both, return false.
            if (((object)a == null) || ((object)b == null))
            {
                return false;
            }

            return a.Equals(b);
        }

        public static bool operator !=(Point a, Point b)
        {
            return !(a == b);
        }

        public override bool Equals(object obj)
        {
            // If parameter is null return false.
            if (obj == null)
            {
                return false;
            }

            Point p = obj as Point;

            if ((object)p == null)
            {
                return false;
            }

            return (x == p.x) && (y == p.y);
        }

        public bool Equals(Point p)
        {
            // If vertex is null return false.
            if ((object)p == null)
            {
                return false;
            }

            // Return true if the fields match:
            return (x == p.x) && (y == p.y);
        }

        #endregion

        public int CompareTo(Point other)
        {
            if (x == other.x && y == other.y)
            {
                return 0;
            }

            return (x < other.x || (x == other.x && y < other.y)) ? -1 : 1;
        }

        public override int GetHashCode()
        {
            return x.GetHashCode() ^ y.GetHashCode();
        }

        public override string ToString()
        {
            return String.Format("[{0},{1}]", x, y);
        }
    }
}
