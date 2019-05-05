// -----------------------------------------------------------------------
// <copyright file="BoundedVoronoi.cs" company="">
// Triangle.NET code by Christian Woltering, http://triangle.codeplex.com/
// </copyright>
// -----------------------------------------------------------------------

namespace TriangleNet.Tools
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using TriangleNet.Data;
    using TriangleNet.Geometry;

    /// <summary>
    /// The Bounded Voronoi Diagram is the dual of a PSLG triangulation.
    /// </summary>
    /// <remarks>
    /// 2D Centroidal Voronoi Tessellations with Constraints, 2010,
    /// Jane Tournois, Pierre Alliez and Olivier Devillers
    /// </remarks>
    public class BoundedVoronoi : IVoronoi
    {
        Mesh mesh;

        Point[] points;
        List<VoronoiRegion> regions;

        int segIndex;

        Dictionary<int, Segment> subsegMap;

        bool includeBoundary = true;

        /// <summary>
        /// Initializes a new instance of the <see cref="BoundedVoronoi" /> class.
        /// </summary>
        /// <param name="mesh">Mesh instance.</param>
        public BoundedVoronoi(Mesh mesh)
            : this(mesh, true)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="BoundedVoronoi" /> class.
        /// </summary>
        /// <param name="mesh">Mesh instance.</param>
        public BoundedVoronoi(Mesh mesh, bool includeBoundary)
        {
            this.mesh = mesh;
            this.includeBoundary = includeBoundary;

            Generate();
        }

        /// <summary>
        /// Gets the list of Voronoi vertices.
        /// </summary>
        public Point[] Points
        {
            get { return points; }
        }

        /// <summary>
        /// Gets the list of Voronoi regions.
        /// </summary>
        public List<VoronoiRegion> Regions
        {
            get { return regions; }
        }

        /// <summary>
        /// Computes the bounded voronoi diagram.
        /// </summary>
        private void Generate()
        {
            mesh.Renumber();
            mesh.MakeVertexMap();

            // Allocate space for voronoi diagram
            this.points = new Point[mesh.triangles.Count + mesh.subsegs.Count * 5]; // This is an upper bound.
            this.regions = new List<VoronoiRegion>(mesh.vertices.Count);

            ComputeCircumCenters();

            TagBlindTriangles();

            foreach (var v in mesh.vertices.Values)
            {
                // TODO: Need a reliable way to check if a vertex is on a segment
                if (v.type == VertexType.FreeVertex || v.Boundary == 0)
                {
                    ConstructBvdCell(v);
                }
                else if (includeBoundary)
                {
                    ConstructBoundaryBvdCell(v);
                }
            }
        }

        private void ComputeCircumCenters()
        {
            Otri tri = default(Otri);
            float xi = 0, eta = 0;
            Point pt;

            // Compue triangle circumcenters
            foreach (var item in mesh.triangles.Values)
            {
                tri.triangle = item;

                pt = Primitives.FindCircumcenter(tri.Org(), tri.Dest(), tri.Apex(), ref xi, ref eta);
                pt.id = item.id;

                points[item.id] = pt;
            }
        }

        /// <summary>
        /// Tag all blind triangles.
        /// </summary>
        /// <remarks>
        /// A triangle is said to be blind if the triangle and its circumcenter
        /// lie on two different sides of a constrained edge.
        /// </remarks>
        private void TagBlindTriangles()
        {
            int blinded = 0;

            Stack<Triangle> triangles;
            subsegMap = new Dictionary<int, Segment>();

            Otri f = default(Otri);
            Otri f0 = default(Otri);
            Osub e = default(Osub);
            Osub sub1 = default(Osub);

            // Tag all triangles non-blind
            foreach (var t in mesh.triangles.Values)
            {
                // Use the infected flag for 'blinded' attribute.
                t.infected = false;
            }

            // for each constrained edge e of cdt do
            foreach (var ss in mesh.subsegs.Values)
            {
                // Create a stack: triangles
                triangles = new Stack<Triangle>();

                // for both adjacent triangles fe to e tagged non-blind do
                // Push fe into triangles
                e.seg = ss;
                e.orient = 0;
                e.TriPivot(ref f);

                if (f.triangle != Mesh.dummytri && !f.triangle.infected)
                {
                    triangles.Push(f.triangle);
                }

                e.SymSelf();
                e.TriPivot(ref f);

                if (f.triangle != Mesh.dummytri && !f.triangle.infected)
                {
                    triangles.Push(f.triangle);
                }

                // while triangles is non-empty
                while (triangles.Count > 0)
                {
                    // Pop f from stack triangles
                    f.triangle = triangles.Pop();
                    f.orient = 0;

                    // if f is blinded by e (use P) then
                    if (TriangleIsBlinded(ref f, ref e))
                    {
                        // Tag f as blinded by e
                        f.triangle.infected = true;
                        blinded++;

                        // Store association triangle -> subseg
                        subsegMap.Add(f.triangle.hash, e.seg);

                        // for each adjacent triangle f0 to f do
                        for (f.orient = 0; f.orient < 3; f.orient++)
                        {
                            f.Sym(ref f0);

                            f0.SegPivot(ref sub1);

                            // if f0 is finite and tagged non-blind & the common edge 
                            // between f and f0 is unconstrained then
                            if (f0.triangle != Mesh.dummytri && !f0.triangle.infected && sub1.seg == Mesh.dummysub)
                            {
                                // Push f0 into triangles.
                                triangles.Push(f0.triangle);
                            }
                        }
                    }
                }
            }

            blinded = 0;
        }

        /// <summary>
        /// Check if given triangle is blinded by given segment.
        /// </summary>
        /// <param name="tri">Triangle.</param>
        /// <param name="seg">Segments</param>
        /// <returns>Returns true, if the triangle is blinded.</returns>
        private bool TriangleIsBlinded(ref Otri tri, ref Osub seg)
        {
            Point c, pt;

            Vertex torg = tri.Org();
            Vertex tdest = tri.Dest();
            Vertex tapex = tri.Apex();

            Vertex sorg = seg.Org();
            Vertex sdest = seg.Dest();

            c = this.points[tri.triangle.id];

            if (SegmentsIntersect(sorg, sdest, c, torg, out pt, true))
            {
                return true;
            }

            if (SegmentsIntersect(sorg, sdest, c, tdest, out pt, true))
            {
                return true;
            }

            if (SegmentsIntersect(sorg, sdest, c, tapex, out pt, true))
            {
                return true;
            }

            return false;
        }

        private void ConstructBvdCell(Vertex vertex)
        {
            VoronoiRegion region = new VoronoiRegion(vertex);
            regions.Add(region);

            Otri f = default(Otri);
            Otri f_init = default(Otri);
            Otri f_next = default(Otri);
            Osub sf = default(Osub);
            Osub sfn = default(Osub);

            Point cc_f, cc_f_next, p;

            int n = mesh.triangles.Count;

            // Call P the polygon (cell) in construction
            List<Point> vpoints = new List<Point>();

            // Call f_init a triangle incident to x
            vertex.tri.Copy(ref f_init);

            if (f_init.Org() != vertex)
            {
                throw new Exception("ConstructBvdCell: inconsistent topology.");
            }

            // Let f be initialized to f_init
            f_init.Copy(ref f);
            // Call f_next the next triangle counterclockwise around x
            f_init.Onext(ref f_next);

            // repeat ... until f = f_init
            do
            {
                // Call Lffnext the line going through the circumcenters of f and f_next
                cc_f = this.points[f.triangle.id];
                cc_f_next = this.points[f_next.triangle.id];

                // if f is tagged non-blind then
                if (!f.triangle.infected)
                {
                    // Insert the circumcenter of f into P
                    vpoints.Add(cc_f);

                    if (f_next.triangle.infected)
                    {
                        // Call S_fnext the constrained edge blinding f_next
                        sfn.seg = subsegMap[f_next.triangle.hash];

                        // Insert point Lf,f_next /\ Sf_next into P
                        if (SegmentsIntersect(sfn.SegOrg(), sfn.SegDest(), cc_f, cc_f_next, out p, true))
                        {
                            p.id = n + segIndex;
                            points[n + segIndex] = p;
                            segIndex++;

                            vpoints.Add(p);
                        }
                    }
                }
                else
                {
                    // Call Sf the constrained edge blinding f
                    sf.seg = subsegMap[f.triangle.hash];

                    // if f_next is tagged non-blind then
                    if (!f_next.triangle.infected)
                    {
                        // Insert point Lf,f_next /\ Sf into P
                        if (SegmentsIntersect(sf.SegOrg(), sf.SegDest(), cc_f, cc_f_next, out p, true))
                        {
                            p.id = n + segIndex;
                            points[n + segIndex] = p;
                            segIndex++;

                            vpoints.Add(p);
                        }
                    }
                    else
                    {
                        // Call Sf_next the constrained edge blinding f_next
                        sfn.seg = subsegMap[f_next.triangle.hash];

                        // if Sf != Sf_next then
                        if (!sf.Equal(sfn))
                        {
                            // Insert Lf,fnext /\ Sf and Lf,fnext /\ Sfnext into P
                            if (SegmentsIntersect(sf.SegOrg(), sf.SegDest(), cc_f, cc_f_next, out p, true))
                            {
                                p.id = n + segIndex;
                                points[n + segIndex] = p;
                                segIndex++;

                                vpoints.Add(p);
                            }

                            if (SegmentsIntersect(sfn.SegOrg(), sfn.SegDest(), cc_f, cc_f_next, out p, true))
                            {
                                p.id = n + segIndex;
                                points[n + segIndex] = p;
                                segIndex++;

                                vpoints.Add(p);
                            }
                        }
                    }
                }

                // f <- f_next
                f_next.Copy(ref f);

                // Call f_next the next triangle counterclockwise around x
                f_next.OnextSelf();
            }
            while (!f.Equal(f_init));

            // Output: Bounded Voronoi cell of x in counterclockwise order.
            region.Add(vpoints);
        }

        private void ConstructBoundaryBvdCell(Vertex vertex)
        {
            VoronoiRegion region = new VoronoiRegion(vertex);
            regions.Add(region);

            Otri f = default(Otri);
            Otri f_init = default(Otri);
            Otri f_next = default(Otri);
            Otri f_prev = default(Otri);
            Osub sf = default(Osub);
            Osub sfn = default(Osub);

            Vertex torg, tdest, tapex, sorg, sdest;
            Point cc_f, cc_f_next, p;

            int n = mesh.triangles.Count;

            // Call P the polygon (cell) in construction
            List<Point> vpoints = new List<Point>();

            // Call f_init a triangle incident to x
            vertex.tri.Copy(ref f_init);

            if (f_init.Org() != vertex)
            {
                throw new Exception("ConstructBoundaryBvdCell: inconsistent topology.");
            }
            // Let f be initialized to f_init
            f_init.Copy(ref f);
            // Call f_next the next triangle counterclockwise around x
            f_init.Onext(ref f_next);

            f_init.Oprev(ref f_prev);

            // Is the border to the left?
            if (f_prev.triangle != Mesh.dummytri)
            {
                // Go clockwise until we reach the border (or the initial triangle)
                while (f_prev.triangle != Mesh.dummytri && !f_prev.Equal(f_init))
                {
                    f_prev.Copy(ref f);
                    f_prev.OprevSelf();
                }

                f.Copy(ref f_init);
                f.Onext(ref f_next);
            }

            if (f_prev.triangle == Mesh.dummytri)
            {
                // For vertices on the domain boundaray, add the vertex. For 
                // internal boundaries don't add it.
                p = new Point(vertex.x, vertex.y);
                p.id = n + segIndex;
                points[n + segIndex] = p;
                segIndex++;

                vpoints.Add(p);
            }

            // Add midpoint of start triangles' edge.
            torg = f.Org();
            tdest = f.Dest();
            p = new Point((torg.X + tdest.X) / 2, (torg.Y + tdest.Y) / 2);
            p.id = n + segIndex;
            points[n + segIndex] = p;
            segIndex++;

            vpoints.Add(p);

            // repeat ... until f = f_init
            do
            {
                // Call Lffnext the line going through the circumcenters of f and f_next
                cc_f = this.points[f.triangle.id];

                if (f_next.triangle == Mesh.dummytri)
                {
                    if (!f.triangle.infected)
                    {
                        // Add last circumcenter
                        vpoints.Add(cc_f);
                    }

                    // Add midpoint of last triangles' edge (chances are it has already
                    // been added, so post process cell to remove duplicates???)
                    torg = f.Org();
                    tapex = f.Apex();
                    p = new Point((torg.X + tapex.X) / 2, (torg.Y + tapex.Y) / 2);
                    p.id = n + segIndex;
                    points[n + segIndex] = p;
                    segIndex++;

                    vpoints.Add(p);

                    break;
                }

                cc_f_next = this.points[f_next.triangle.id];

                // if f is tagged non-blind then
                if (!f.triangle.infected)
                {
                    // Insert the circumcenter of f into P
                    vpoints.Add(cc_f);

                    if (f_next.triangle.infected)
                    {
                        // Call S_fnext the constrained edge blinding f_next
                        sfn.seg = subsegMap[f_next.triangle.hash];

                        // Insert point Lf,f_next /\ Sf_next into P
                        if (SegmentsIntersect(sfn.SegOrg(), sfn.SegDest(), cc_f, cc_f_next, out p, true))
                        {
                            p.id = n + segIndex;
                            points[n + segIndex] = p;
                            segIndex++;

                            vpoints.Add(p);
                        }
                    }
                }
                else
                {
                    // Call Sf the constrained edge blinding f
                    sf.seg = subsegMap[f.triangle.hash];

                    sorg = sf.SegOrg();
                    sdest = sf.SegDest();

                    // if f_next is tagged non-blind then
                    if (!f_next.triangle.infected)
                    {
                        tdest = f.Dest();
                        tapex = f.Apex();

                        // Both circumcenters lie on the blinded side, but we
                        // have to add the intersection with the segment.

                        // Center of f edge dest->apex
                        Point bisec = new Point((tdest.X + tapex.X) / 2, (tdest.Y + tapex.Y) / 2);

                        // Find intersection of seg with line through f's bisector and circumcenter
                        if (SegmentsIntersect(sorg, sdest, bisec, cc_f, out p, false))
                        {
                            p.id = n + segIndex;
                            points[n + segIndex] = p;
                            segIndex++;

                            vpoints.Add(p);
                        }

                        // Insert point Lf,f_next /\ Sf into P
                        if (SegmentsIntersect(sorg, sdest, cc_f, cc_f_next, out p, true))
                        {
                            p.id = n + segIndex;
                            points[n + segIndex] = p;
                            segIndex++;

                            vpoints.Add(p);
                        }
                    }
                    else
                    {
                        // Call Sf_next the constrained edge blinding f_next
                        sfn.seg = subsegMap[f_next.triangle.hash];

                        // if Sf != Sf_next then
                        if (!sf.Equal(sfn))
                        {
                            // Insert Lf,fnext /\ Sf and Lf,fnext /\ Sfnext into P
                            if (SegmentsIntersect(sorg, sdest, cc_f, cc_f_next, out p, true))
                            {
                                p.id = n + segIndex;
                                points[n + segIndex] = p;
                                segIndex++;

                                vpoints.Add(p);
                            }

                            if (SegmentsIntersect(sfn.SegOrg(), sfn.SegDest(), cc_f, cc_f_next, out p, true))
                            {
                                p.id = n + segIndex;
                                points[n + segIndex] = p;
                                segIndex++;

                                vpoints.Add(p);
                            }
                        }
                        else
                        {
                            // Both circumcenters lie on the blinded side, but we
                            // have to add the intersection with the segment.

                            // Center of f_next edge org->dest
                            Point bisec = new Point((torg.X + tdest.X) / 2, (torg.Y + tdest.Y) / 2);

                            // Find intersection of seg with line through f_next's bisector and circumcenter
                            if (SegmentsIntersect(sorg, sdest, bisec, cc_f_next, out p, false))
                            {
                                p.id = n + segIndex;
                                points[n + segIndex] = p;
                                segIndex++;

                                vpoints.Add(p);
                            }
                        }
                    }
                }

                // f <- f_next
                f_next.Copy(ref f);

                // Call f_next the next triangle counterclockwise around x
                f_next.OnextSelf();
            }
            while (!f.Equal(f_init));

            // Output: Bounded Voronoi cell of x in counterclockwise order.
            region.Add(vpoints);
        }

        /// <summary>
        /// Determines the intersection point of the line segment defined by points A and B with the 
        /// line segment defined by points C and D.
        /// </summary>
        /// <param name="seg">The first segment AB.</param>
        /// <param name="pc">Endpoint C of second segment.</param>
        /// <param name="pd">Endpoint D of second segment.</param>
        /// <param name="p">Reference to the intersection point.</param>
        /// <param name="strictIntersect">If false, pa and pb represent a line.</param>
        /// <returns>Returns true if the intersection point was found, and stores that point in X,Y.
        /// Returns false if there is no determinable intersection point, in which case X,Y will
        /// be unmodified.
        /// </returns>
        private bool SegmentsIntersect(Point p1, Point p2, Point p3, Point p4, out Point p, bool strictIntersect)
        {
            p = null;

            float Ax = p1.X, Ay = p1.Y;
            float Bx = p2.X, By = p2.Y;
            float Cx = p3.X, Cy = p3.Y;
            float Dx = p4.X, Dy = p4.Y;

            float distAB, theCos, theSin, newX, ABpos;

            //  Fail if either line segment is zero-length.
            if (Ax == Bx && Ay == By || Cx == Dx && Cy == Dy) return false;

            //  Fail if the segments share an end-point.
            if (Ax == Cx && Ay == Cy || Bx == Cx && By == Cy
            || Ax == Dx && Ay == Dy || Bx == Dx && By == Dy)
            {
                return false;
            }

            //  (1) Translate the system so that point A is on the origin.
            Bx -= Ax; By -= Ay;
            Cx -= Ax; Cy -= Ay;
            Dx -= Ax; Dy -= Ay;

            //  Discover the length of segment A-B.
            distAB = UnityEngine.Mathf.Sqrt(Bx * Bx + By * By);

            //  (2) Rotate the system so that point B is on the positive X axis.
            theCos = Bx / distAB;
            theSin = By / distAB;
            newX = Cx * theCos + Cy * theSin;
            Cy = Cy * theCos - Cx * theSin; Cx = newX;
            newX = Dx * theCos + Dy * theSin;
            Dy = Dy * theCos - Dx * theSin; Dx = newX;

            //  Fail if segment C-D doesn't cross line A-B.
            if (Cy < 0 && Dy < 0 || Cy >= 0 && Dy >= 0 && strictIntersect) return false;

            //  (3) Discover the position of the intersection point along line A-B.
            ABpos = Dx + (Cx - Dx) * Dy / (Dy - Cy);

            //  Fail if segment C-D crosses line A-B outside of segment A-B.
            if (ABpos < 0 || ABpos > distAB && strictIntersect) return false;

            //  (4) Apply the discovered position to line A-B in the original coordinate system.
            p = new Point(Ax + ABpos * theCos, Ay + ABpos * theSin);

            //  Success.
            return true;
        }
    }
}
