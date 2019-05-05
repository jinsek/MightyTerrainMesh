// -----------------------------------------------------------------------
// <copyright file="Incremental.cs">
// Original Triangle code by Jonathan Richard Shewchuk, http://www.cs.cmu.edu/~quake/triangle.html
// Triangle.NET code by Christian Woltering, http://triangle.codeplex.com/
// </copyright>
// -----------------------------------------------------------------------

namespace TriangleNet.Algorithm
{
    using TriangleNet.Data;
    using TriangleNet.Log;
    using TriangleNet.Geometry;

    /// <summary>
    /// Builds a delaunay triangulation using the incremental algorithm.
    /// </summary>
    class Incremental
    {
        Mesh mesh;

        /// <summary>
        /// Form an "infinite" bounding triangle to insert vertices into.
        /// </summary>
        /// <remarks>
        /// The vertices at "infinity" are assigned finite coordinates, which are
        /// used by the point location routines, but (mostly) ignored by the
        /// Delaunay edge flip routines.
        /// </remarks>
        void GetBoundingBox()
        {
            Otri inftri = default(Otri); // Handle for the triangular bounding box.
            BoundingBox box = mesh.bounds;

            // Find the width (or height, whichever is larger) of the triangulation.
            float width = box.Width;
            if (box.Height > width)
            {
                width = box.Height;
            }
            if (width == 0.0f)
            {
                width = 1.0f;
            }
            // Create the vertices of the bounding box.
            mesh.infvertex1 = new Vertex(box.Xmin - 50.0f * width, box.Ymin - 40.0f * width);
            mesh.infvertex2 = new Vertex(box.Xmax + 50.0f * width, box.Ymin - 40.0f * width);
            mesh.infvertex3 = new Vertex(0.5f * (box.Xmin + box.Xmax), box.Ymax + 60.0f * width);

            // Create the bounding box.
            mesh.MakeTriangle(ref inftri);
            inftri.SetOrg(mesh.infvertex1);
            inftri.SetDest(mesh.infvertex2);
            inftri.SetApex(mesh.infvertex3);
            // Link dummytri to the bounding box so we can always find an
            // edge to begin searching (point location) from.
            Mesh.dummytri.neighbors[0] = inftri;
        }

        /// <summary>
        /// Remove the "infinite" bounding triangle, setting boundary markers as appropriate.
        /// </summary>
        /// <returns>Returns the number of edges on the convex hull of the triangulation.</returns>
        /// <remarks>
        /// The triangular bounding box has three boundary triangles (one for each
        /// side of the bounding box), and a bunch of triangles fanning out from
        /// the three bounding box vertices (one triangle for each edge of the
        /// convex hull of the inner mesh).  This routine removes these triangles.
        /// </remarks>
        int RemoveBox()
        {
            Otri deadtriangle = default(Otri);
            Otri searchedge = default(Otri);
            Otri checkedge = default(Otri);
            Otri nextedge = default(Otri), finaledge = default(Otri), dissolveedge = default(Otri);
            Vertex markorg;
            int hullsize;

            bool noPoly = !mesh.behavior.Poly;

            // Find a boundary triangle.
            nextedge.triangle = Mesh.dummytri;
            nextedge.orient = 0;
            nextedge.SymSelf();
            // Mark a place to stop.
            nextedge.Lprev(ref finaledge);
            nextedge.LnextSelf();
            nextedge.SymSelf();
            // Find a triangle (on the boundary of the vertex set) that isn't
            // a bounding box triangle.
            nextedge.Lprev(ref searchedge);
            searchedge.SymSelf();
            // Check whether nextedge is another boundary triangle
            // adjacent to the first one.
            nextedge.Lnext(ref checkedge);
            checkedge.SymSelf();
            if (checkedge.triangle == Mesh.dummytri)
            {
                // Go on to the next triangle.  There are only three boundary
                // triangles, and this next triangle cannot be the third one,
                // so it's safe to stop here.
                searchedge.LprevSelf();
                searchedge.SymSelf();
            }
            // Find a new boundary edge to search from, as the current search
            // edge lies on a bounding box triangle and will be deleted.
            Mesh.dummytri.neighbors[0] = searchedge;
            hullsize = -2;
            while (!nextedge.Equal(finaledge))
            {
                hullsize++;
                nextedge.Lprev(ref dissolveedge);
                dissolveedge.SymSelf();
                // If not using a PSLG, the vertices should be marked now.
                // (If using a PSLG, markhull() will do the job.)
                if (noPoly)
                {
                    // Be careful!  One must check for the case where all the input
                    // vertices are collinear, and thus all the triangles are part of
                    // the bounding box.  Otherwise, the setvertexmark() call below
                    // will cause a bad pointer reference.
                    if (dissolveedge.triangle != Mesh.dummytri)
                    {
                        markorg = dissolveedge.Org();
                        if (markorg.mark == 0)
                        {
                            markorg.mark = 1;
                        }
                    }
                }
                // Disconnect the bounding box triangle from the mesh triangle.
                dissolveedge.Dissolve();
                nextedge.Lnext(ref deadtriangle);
                deadtriangle.Sym(ref nextedge);
                // Get rid of the bounding box triangle.
                mesh.TriangleDealloc(deadtriangle.triangle);
                // Do we need to turn the corner?
                if (nextedge.triangle == Mesh.dummytri)
                {
                    // Turn the corner.
                    dissolveedge.Copy(ref nextedge);
                }
            }
            mesh.TriangleDealloc(finaledge.triangle);

            return hullsize;
        }

        /// <summary>
        /// Form a Delaunay triangulation by incrementally inserting vertices.
        /// </summary>
        /// <returns>Returns the number of edges on the convex hull of the 
        /// triangulation.</returns>
        public int Triangulate(Mesh mesh)
        {
            this.mesh = mesh;

            Otri starttri = new Otri();

            // Create a triangular bounding box.
            GetBoundingBox();

            foreach (var v in mesh.vertices.Values)
            {
                starttri.triangle = Mesh.dummytri;
                Osub tmp = default(Osub);
                if (mesh.InsertVertex(v, ref starttri, ref tmp, false, false) == InsertVertexResult.Duplicate)
                {
                    if (Behavior.Verbose)
                    {
                        SimpleLog.Instance.Warning("A duplicate vertex appeared and was ignored.", 
                            "Incremental.IncrementalDelaunay()");
                    }
                    v.type = VertexType.UndeadVertex;
                    mesh.undeads++;
                }
            }
            // Remove the bounding box.
            return RemoveBox();
        }
    }
}
