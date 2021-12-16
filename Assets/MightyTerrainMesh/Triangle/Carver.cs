// -----------------------------------------------------------------------
// <copyright file="Carver.cs">
// Original Triangle code by Jonathan Richard Shewchuk, http://www.cs.cmu.edu/~quake/triangle.html
// Triangle.NET code by Christian Woltering, http://triangle.codeplex.com/
// </copyright>
// -----------------------------------------------------------------------

namespace TriangleNet
{
    using TriangleNet.Data;
    using System;
    using TriangleNet.Geometry;
    using System.Collections.Generic;
    using TriangleNet.Tools;

    /// <summary>
    /// Carves holes into the triangulation.
    /// </summary>
    class Carver
    {
        Mesh mesh;
        List<Triangle> viri;

        public Carver(Mesh mesh)
        {
            this.mesh = mesh;
            this.viri = new List<Triangle>();
        }

        /// <summary>
        /// Virally infect all of the triangles of the convex hull that are not 
        /// protected by subsegments. Where there are subsegments, set boundary 
        /// markers as appropriate.
        /// </summary>
        private void InfectHull()
        {
            Otri hulltri = default(Otri);
            Otri nexttri = default(Otri);
            Otri starttri = default(Otri);
            Osub hullsubseg = default(Osub);
            Vertex horg, hdest;

            // Find a triangle handle on the hull.
            hulltri.triangle = Mesh.dummytri;
            hulltri.orient = 0;
            hulltri.SymSelf();
            // Remember where we started so we know when to stop.
            hulltri.Copy(ref starttri);
            // Go once counterclockwise around the convex hull.
            do
            {
                // Ignore triangles that are already infected.
                if (!hulltri.IsInfected())
                {
                    // Is the triangle protected by a subsegment?
                    hulltri.SegPivot(ref hullsubseg);
                    if (hullsubseg.seg == Mesh.dummysub)
                    {
                        // The triangle is not protected; infect it.
                        if (!hulltri.IsInfected())
                        {
                            hulltri.Infect();
                            viri.Add(hulltri.triangle);
                        }
                    }
                    else
                    {
                        // The triangle is protected; set boundary markers if appropriate.
                        if (hullsubseg.seg.boundary == 0)
                        {
                            hullsubseg.seg.boundary = 1;
                            horg = hulltri.Org();
                            hdest = hulltri.Dest();
                            if (horg.mark == 0)
                            {
                                horg.mark = 1;
                            }
                            if (hdest.mark == 0)
                            {
                                hdest.mark = 1;
                            }
                        }
                    }
                }
                // To find the next hull edge, go clockwise around the next vertex.
                hulltri.LnextSelf();
                hulltri.Oprev(ref nexttri);
                while (nexttri.triangle != Mesh.dummytri)
                {
                    nexttri.Copy(ref hulltri);
                    hulltri.Oprev(ref nexttri);
                }

            } while (!hulltri.Equal(starttri));
        }

        /// <summary>
        /// Spread the virus from all infected triangles to any neighbors not 
        /// protected by subsegments. Delete all infected triangles.
        /// </summary>
        /// <remarks>
        /// This is the procedure that actually creates holes and concavities.
        ///
        /// This procedure operates in two phases. The first phase identifies all
        /// the triangles that will die, and marks them as infected. They are
        /// marked to ensure that each triangle is added to the virus pool only
        /// once, so the procedure will terminate.
        ///
        /// The second phase actually eliminates the infected triangles. It also
        /// eliminates orphaned vertices.
        /// </remarks>
        void Plague()
        {
            Otri testtri = default(Otri);
            Otri neighbor = default(Otri);
            Osub neighborsubseg = default(Osub);
            Vertex testvertex;
            Vertex norg, ndest;

            bool killorg;

            // Loop through all the infected triangles, spreading the virus to
            // their neighbors, then to their neighbors' neighbors.
            for (int i = 0; i < viri.Count; i++)
            {
                // WARNING: Don't use foreach, mesh.viri list may get modified.

                testtri.triangle = viri[i];
                // A triangle is marked as infected by messing with one of its pointers
                // to subsegments, setting it to an illegal value.  Hence, we have to
                // temporarily uninfect this triangle so that we can examine its
                // adjacent subsegments.
                // TODO: Not true in the C# version (so we could skip this).
                testtri.Uninfect();

                // Check each of the triangle's three neighbors.
                for (testtri.orient = 0; testtri.orient < 3; testtri.orient++)
                {
                    // Find the neighbor.
                    testtri.Sym(ref neighbor);
                    // Check for a subsegment between the triangle and its neighbor.
                    testtri.SegPivot(ref neighborsubseg);
                    // Check if the neighbor is nonexistent or already infected.
                    if ((neighbor.triangle == Mesh.dummytri) || neighbor.IsInfected())
                    {
                        if (neighborsubseg.seg != Mesh.dummysub)
                        {
                            // There is a subsegment separating the triangle from its
                            // neighbor, but both triangles are dying, so the subsegment
                            // dies too.
                            mesh.SubsegDealloc(neighborsubseg.seg);
                            if (neighbor.triangle != Mesh.dummytri)
                            {
                                // Make sure the subsegment doesn't get deallocated again
                                // later when the infected neighbor is visited.
                                neighbor.Uninfect();
                                neighbor.SegDissolve();
                                neighbor.Infect();
                            }
                        }
                    }
                    else
                    {   // The neighbor exists and is not infected.
                        if (neighborsubseg.seg == Mesh.dummysub)
                        {
                            // There is no subsegment protecting the neighbor, so
                            // the neighbor becomes infected.
                            neighbor.Infect();
                            // Ensure that the neighbor's neighbors will be infected.
                            viri.Add(neighbor.triangle);
                        }
                        else
                        {
                            // The neighbor is protected by a subsegment.
                            // Remove this triangle from the subsegment.
                            neighborsubseg.TriDissolve();
                            // The subsegment becomes a boundary.  Set markers accordingly.
                            if (neighborsubseg.seg.boundary == 0)
                            {
                                neighborsubseg.seg.boundary = 1;
                            }
                            norg = neighbor.Org();
                            ndest = neighbor.Dest();
                            if (norg.mark == 0)
                            {
                                norg.mark = 1;
                            }
                            if (ndest.mark == 0)
                            {
                                ndest.mark = 1;
                            }
                        }
                    }
                }
                // Remark the triangle as infected, so it doesn't get added to the
                // virus pool again.
                testtri.Infect();
            }

            foreach (var virus in viri)
            {
                testtri.triangle = virus;

                // Check each of the three corners of the triangle for elimination.
                // This is done by walking around each vertex, checking if it is
                // still connected to at least one live triangle.
                for (testtri.orient = 0; testtri.orient < 3; testtri.orient++)
                {
                    testvertex = testtri.Org();
                    // Check if the vertex has already been tested.
                    if (testvertex != null)
                    {
                        killorg = true;
                        // Mark the corner of the triangle as having been tested.
                        testtri.SetOrg(null);
                        // Walk counterclockwise about the vertex.
                        testtri.Onext(ref neighbor);
                        // Stop upon reaching a boundary or the starting triangle.
                        while ((neighbor.triangle != Mesh.dummytri) &&
                               (!neighbor.Equal(testtri)))
                        {
                            if (neighbor.IsInfected())
                            {
                                // Mark the corner of this triangle as having been tested.
                                neighbor.SetOrg(null);
                            }
                            else
                            {
                                // A live triangle.  The vertex survives.
                                killorg = false;
                            }
                            // Walk counterclockwise about the vertex.
                            neighbor.OnextSelf();
                        }
                        // If we reached a boundary, we must walk clockwise as well.
                        if (neighbor.triangle == Mesh.dummytri)
                        {
                            // Walk clockwise about the vertex.
                            testtri.Oprev(ref neighbor);
                            // Stop upon reaching a boundary.
                            while (neighbor.triangle != Mesh.dummytri)
                            {
                                if (neighbor.IsInfected())
                                {
                                    // Mark the corner of this triangle as having been tested.
                                    neighbor.SetOrg(null);
                                }
                                else
                                {
                                    // A live triangle.  The vertex survives.
                                    killorg = false;
                                }
                                // Walk clockwise about the vertex.
                                neighbor.OprevSelf();
                            }
                        }
                        if (killorg)
                        {
                            // Deleting vertex
                            testvertex.type = VertexType.UndeadVertex;
                            mesh.undeads++;
                        }
                    }
                }

                // Record changes in the number of boundary edges, and disconnect
                // dead triangles from their neighbors.
                for (testtri.orient = 0; testtri.orient < 3; testtri.orient++)
                {
                    testtri.Sym(ref neighbor);
                    if (neighbor.triangle == Mesh.dummytri)
                    {
                        // There is no neighboring triangle on this edge, so this edge
                        // is a boundary edge. This triangle is being deleted, so this
                        // boundary edge is deleted.
                        mesh.hullsize--;
                    }
                    else
                    {
                        // Disconnect the triangle from its neighbor.
                        neighbor.Dissolve();
                        // There is a neighboring triangle on this edge, so this edge
                        // becomes a boundary edge when this triangle is deleted.
                        mesh.hullsize++;
                    }
                }
                // Return the dead triangle to the pool of triangles.
                mesh.TriangleDealloc(testtri.triangle);
            }

            // Empty the virus pool.
            viri.Clear();
        }

        /// <summary>
        /// Find the holes and infect them. Find the area constraints and infect 
        /// them. Infect the convex hull. Spread the infection and kill triangles. 
        /// Spread the area constraints.
        /// </summary>
        public void CarveHoles()
        {
            Otri searchtri = default(Otri);
            Vertex searchorg, searchdest;
            LocateResult intersect;

            Triangle[] regionTris = null;

            if (!mesh.behavior.Convex)
            {
                // Mark as infected any unprotected triangles on the boundary.
                // This is one way by which concavities are created.
                InfectHull();
            }

            if (!mesh.behavior.NoHoles)
            {
                // Infect each triangle in which a hole lies.
                foreach (var hole in mesh.holes)
                {
                    // Ignore holes that aren't within the bounds of the mesh.
                    if (mesh.bounds.Contains(hole))
                    {
                        // Start searching from some triangle on the outer boundary.
                        searchtri.triangle = Mesh.dummytri;
                        searchtri.orient = 0;
                        searchtri.SymSelf();
                        // Ensure that the hole is to the left of this boundary edge;
                        // otherwise, locate() will falsely report that the hole
                        // falls within the starting triangle.
                        searchorg = searchtri.Org();
                        searchdest = searchtri.Dest();
                        if (Primitives.CounterClockwise(searchorg, searchdest, hole) > 0.0)
                        {
                            // Find a triangle that contains the hole.
                            intersect = mesh.locator.Locate(hole, ref searchtri);
                            if ((intersect != LocateResult.Outside) && (!searchtri.IsInfected()))
                            {
                                // Infect the triangle. This is done by marking the triangle
                                // as infected and including the triangle in the virus pool.
                                searchtri.Infect();
                                viri.Add(searchtri.triangle);
                            }
                        }
                    }
                }
            }

            // Now, we have to find all the regions BEFORE we carve the holes, because locate() won't
            // work when the triangulation is no longer convex. (Incidentally, this is the reason why
            // regional attributes and area constraints can't be used when refining a preexisting mesh,
            // which might not be convex; they can only be used with a freshly triangulated PSLG.)
            if (mesh.regions.Count > 0)
            {
                int i = 0;

                regionTris = new Triangle[mesh.regions.Count];

                // Find the starting triangle for each region.
                foreach (var region in mesh.regions)
                {
                    regionTris[i] = Mesh.dummytri;
                    // Ignore region points that aren't within the bounds of the mesh.
                    if (mesh.bounds.Contains(region.point))
                    {
                        // Start searching from some triangle on the outer boundary.
                        searchtri.triangle = Mesh.dummytri;
                        searchtri.orient = 0;
                        searchtri.SymSelf();
                        // Ensure that the region point is to the left of this boundary
                        // edge; otherwise, locate() will falsely report that the
                        // region point falls within the starting triangle.
                        searchorg = searchtri.Org();
                        searchdest = searchtri.Dest();
                        if (Primitives.CounterClockwise(searchorg, searchdest, region.point) > 0.0)
                        {
                            // Find a triangle that contains the region point.
                            intersect = mesh.locator.Locate(region.point, ref searchtri);
                            if ((intersect != LocateResult.Outside) && (!searchtri.IsInfected()))
                            {
                                // Record the triangle for processing after the
                                // holes have been carved.
                                regionTris[i] = searchtri.triangle;
                                regionTris[i].region = region.id;
                            }
                        }
                    }

                    i++;
                }
            }

            if (viri.Count > 0)
            {
                // Carve the holes and concavities.
                Plague();
            }

            if (regionTris != null)
            {
                var iterator = new RegionIterator(mesh);

                for (int i = 0; i < regionTris.Length; i++)
                {
                    if (regionTris[i] != Mesh.dummytri)
                    {
                        // Make sure the triangle under consideration still exists.
                        // It may have been eaten by the virus.
                        if (!Otri.IsDead(regionTris[i]))
                        {
                            // Apply one region's attribute and/or area constraint.
                            iterator.Process(regionTris[i]);
                        }
                    }
                }
            }

            // Free up memory (virus pool should be empty anyway).
            viri.Clear();
        }
    }
}
