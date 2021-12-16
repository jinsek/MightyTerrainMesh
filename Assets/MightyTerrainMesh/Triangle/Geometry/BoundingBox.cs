// -----------------------------------------------------------------------
// <copyright file="BoundingBox.cs" company="">
// Triangle.NET code by Christian Woltering, http://triangle.codeplex.com/
// </copyright>
// -----------------------------------------------------------------------

namespace TriangleNet.Geometry
{
    using System;

    /// <summary>
    /// A simple bounding box class.
    /// </summary>
    public class BoundingBox
    {
        float xmin, ymin, xmax, ymax;

        /// <summary>
        /// Initializes a new instance of the <see cref="BoundingBox" /> class.
        /// </summary>
        public BoundingBox()
        {
            xmin = float.MaxValue;
            ymin = float.MaxValue;
            xmax = -float.MaxValue;
            ymax = -float.MaxValue;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="BoundingBox" /> class
        /// with predefined bounds.
        /// </summary>
        /// <param name="xmin">Minimum x value.</param>
        /// <param name="ymin">Minimum y value.</param>
        /// <param name="xmax">Maximum x value.</param>
        /// <param name="ymax">Maximum y value.</param>
        public BoundingBox(float xmin, float ymin, float xmax, float ymax)
        {
            this.xmin = xmin;
            this.ymin = ymin;
            this.xmax = xmax;
            this.ymax = ymax;
        }

        /// <summary>
        /// Gets the minimum x value (left boundary).
        /// </summary>
        public float Xmin
        {
            get { return xmin; }
        }

        /// <summary>
        /// Gets the minimum y value (bottom boundary).
        /// </summary>
        public float Ymin
        {
            get { return ymin; }
        }

        /// <summary>
        /// Gets the maximum x value (right boundary).
        /// </summary>
        public float Xmax
        {
            get { return xmax; }
        }

        /// <summary>
        /// Gets the maximum y value (top boundary).
        /// </summary>
        public float Ymax
        {
            get { return ymax; }
        }

        /// <summary>
        /// Gets the width of the bounding box.
        /// </summary>
        public float Width
        {
            get { return xmax - xmin; }
        }

        /// <summary>
        /// Gets the height of the bounding box.
        /// </summary>
        public float Height
        {
            get { return ymax - ymin; }
        }

        /// <summary>
        /// Update bounds.
        /// </summary>
        /// <param name="x">X coordinate.</param>
        /// <param name="y">Y coordinate.</param>
        public void Update(float x, float y)
        {
            xmin = UnityEngine.Mathf.Min(xmin, x);
            ymin = UnityEngine.Mathf.Min(ymin, y);
            xmax = UnityEngine.Mathf.Max(xmax, x);
            ymax = UnityEngine.Mathf.Max(ymax, y);
        }

        /// <summary>
        /// Scale bounds.
        /// </summary>
        /// <param name="dx">Add dx to left and right bounds.</param>
        /// <param name="dy">Add dy to top and bottom bounds.</param>
        public void Scale(float dx, float dy)
        {
            xmin -= dx;
            xmax += dx;
            ymin -= dy;
            ymax += dy;
        }

        /// <summary>
        /// Check if given point is inside bounding box.
        /// </summary>
        /// <param name="pt">Point to check.</param>
        /// <returns>Return true, if bounding box contains given point.</returns>
        public bool Contains(Point pt)
        {
            return ((pt.x >= xmin) && (pt.x <= xmax) && (pt.y >= ymin) && (pt.y <= ymax));
        }
    }
}
