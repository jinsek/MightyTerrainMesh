// -----------------------------------------------------------------------
// <copyright file="BadSubseg.cs" company="">
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
    /// A queue used to store encroached subsegments.
    /// </summary>
    /// <remarks>
    /// Each subsegment's vertices are stored so that we can check whether a 
    /// subsegment is still the same.
    /// </remarks>
    class BadSubseg
    {
        private static int hashSeed = 0;
        internal int Hash;

        public Osub encsubseg; // An encroached subsegment.
        public Vertex subsegorg, subsegdest; // Its two vertices.

        public BadSubseg()
        {
            this.Hash = hashSeed++;
        }

        public override int GetHashCode()
        {
            return this.Hash;
        }

        public override string ToString()
        {
            return String.Format("B-SID {0}", encsubseg.seg.hash);
        }
    };
}
