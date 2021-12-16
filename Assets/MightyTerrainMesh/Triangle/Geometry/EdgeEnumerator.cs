// -----------------------------------------------------------------------
// <copyright file="EdgeEnumerator.cs" company="">
// Triangle.NET code by Christian Woltering, http://triangle.codeplex.com/
// </copyright>
// -----------------------------------------------------------------------

namespace TriangleNet.Geometry
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using TriangleNet.Data;

    /// <summary>
    /// Enumerates the edges of a triangulation.
    /// </summary>
    public class EdgeEnumerator : IEnumerator<Edge>
    {
        IEnumerator<Triangle> triangles;
        Otri tri = default(Otri);
        Otri neighbor = default(Otri);
        Osub sub = default(Osub);
        Edge current;
        Vertex p1, p2;

        /// <summary>
        /// Initializes a new instance of the <see cref="EdgeEnumerator" /> class.
        /// </summary>
        public EdgeEnumerator(Mesh mesh)
        {
            triangles = mesh.triangles.Values.GetEnumerator();
            triangles.MoveNext();

            tri.triangle = triangles.Current;
            tri.orient = 0;
        }

        public Edge Current
        {
            get { return current; }
        }

        public void Dispose()
        {
            this.triangles.Dispose();
        }

        object System.Collections.IEnumerator.Current
        {
            get { return current; }
        }

        public bool MoveNext()
        {
            if (tri.triangle == null)
            {
                return false;
            }

            current = null;

            while (current == null)
            {
                if (tri.orient == 3)
                {
                    if (triangles.MoveNext())
                    {
                        tri.triangle = triangles.Current;
                        tri.orient = 0;
                    }
                    else
                    {
                        // Finally no more triangles
                        return false;
                    }
                }

                tri.Sym(ref neighbor);

                if ((tri.triangle.id < neighbor.triangle.id) || (neighbor.triangle == Mesh.dummytri))
                {
                    p1 = tri.Org();
                    p2 = tri.Dest();

                    tri.SegPivot(ref sub);

                    // Boundary mark of dummysub is 0, so we don't need to worry about that.
                    current = new Edge(p1.id, p2.id, sub.seg.boundary);
                }

                tri.orient++;
            }

            return true;
        }

        public void Reset()
        {
            this.triangles.Reset();
        }
    }
}
