// -----------------------------------------------------------------------
// <copyright file="BadTriangle.cs" company="">
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
    /// A queue used to store bad triangles.
    /// </summary>
    /// <remarks>
    /// The key is the square of the cosine of the smallest angle of the triangle.
    /// Each triangle's vertices are stored so that one can check whether a
    /// triangle is still the same.
    /// </remarks>
    class BadTriangle
    {
        public static int OTID = 0;
        public int ID = 0;

        public Otri poortri; // A skinny or too-large triangle.
        public float key;       // cos^2 of smallest (apical) angle.
        public Vertex triangorg, triangdest, triangapex; // Its three vertices.
        public BadTriangle nexttriang; // Pointer to next bad triangle.

        public BadTriangle()
        {
            ID = OTID++;
        }
        public override string ToString()
        {
            return String.Format("B-TID {0}", poortri.triangle.hash);
        }
    }
}
