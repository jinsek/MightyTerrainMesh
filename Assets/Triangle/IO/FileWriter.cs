// -----------------------------------------------------------------------
// <copyright file="FileWriter.cs" company="">
// Original Triangle code by Jonathan Richard Shewchuk, http://www.cs.cmu.edu/~quake/triangle.html
// Triangle.NET code by Christian Woltering, http://triangle.codeplex.com/
// </copyright>
// -----------------------------------------------------------------------

namespace TriangleNet.IO
{
    using System;
    using System.IO;
    using System.Globalization;
    using TriangleNet.Data;
    using TriangleNet.Geometry;
    using System.Collections.Generic;

    /// <summary>
    /// Helper methods for writing Triangle file formats.
    /// </summary>
    public static class FileWriter
    {
        static NumberFormatInfo nfi = CultureInfo.InvariantCulture.NumberFormat;

        /// <summary>
        /// Number the vertices and write them to a .node file.
        /// </summary>
        /// <param name="mesh"></param>
        /// <param name="filename"></param>
        public static void Write(Mesh mesh, string filename)
        {
            FileWriter.WritePoly(mesh, Path.ChangeExtension(filename, ".poly"));
            FileWriter.WriteElements(mesh, Path.ChangeExtension(filename, ".ele"));
        }

        /// <summary>
        /// Number the vertices and write them to a .node file.
        /// </summary>
        /// <param name="mesh"></param>
        /// <param name="filename"></param>
        public static void WriteNodes(Mesh mesh, string filename)
        {
            using (StreamWriter writer = new StreamWriter(filename))
            {
                FileWriter.WriteNodes(writer, mesh);
            }
        }

        /// <summary>
        /// Number the vertices and write them to a .node file.
        /// </summary>
        private static void WriteNodes(StreamWriter writer, Mesh mesh)
        {
            int outvertices = mesh.vertices.Count;

            Behavior behavior = mesh.behavior;

            if (behavior.Jettison)
            {
                outvertices = mesh.vertices.Count - mesh.undeads;
            }

            if (writer != null)
            {
                // Number of vertices, number of dimensions, number of vertex attributes,
                // and number of boundary markers (zero or one).
                writer.WriteLine("{0} {1} {2} {3}", outvertices, mesh.mesh_dim, mesh.nextras,
                    behavior.UseBoundaryMarkers ? "1" : "0");

                if (mesh.numbering == NodeNumbering.None)
                {
                    // If the mesh isn't numbered yet, use linear node numbering.
                    mesh.Renumber();
                }

                if (mesh.numbering == NodeNumbering.Linear)
                {
                    // If numbering is linear, just use the dictionary values.
                    WriteNodes(writer, mesh.vertices.Values, behavior.UseBoundaryMarkers,
                        mesh.nextras, behavior.Jettison);
                }
                else
                {
                    // If numbering is not linear, a simple 'foreach' traversal of the dictionary
                    // values doesn't reflect the actual numbering. Use an array instead.

                    // TODO: Could use a custom sorting function on dictionary values instead.
                    Vertex[] nodes = new Vertex[mesh.vertices.Count];

                    foreach (var node in mesh.vertices.Values)
                    {
                        nodes[node.id] = node;
                    }

                    WriteNodes(writer, nodes, behavior.UseBoundaryMarkers,
                        mesh.nextras, behavior.Jettison);
                }
            }
        }

        /// <summary>
        /// Write the vertices to a stream.
        /// </summary>
        /// <param name="nodes"></param>
        /// <param name="writer"></param>
        private static void WriteNodes(StreamWriter writer, IEnumerable<Vertex> nodes, bool markers,
            int attribs, bool jettison)
        {
            int index = 0;

            foreach (var vertex in nodes)
            {
                if (!jettison || vertex.type != VertexType.UndeadVertex)
                {
                    // Vertex number, x and y coordinates.
                    writer.Write("{0} {1} {2}", index, vertex.x.ToString(nfi), vertex.y.ToString(nfi));

                    // Write attributes.
                    for (int j = 0; j < attribs; j++)
                    {
                        writer.Write(" {0}", vertex.attributes[j].ToString(nfi));
                    }

                    if (markers)
                    {
                        // Write the boundary marker.
                        writer.Write(" {0}", vertex.mark);
                    }

                    writer.WriteLine();

                    index++;
                }
            }
        }

        /// <summary>
        /// Write the triangles to an .ele file.
        /// </summary>
        /// <param name="mesh"></param>
        /// <param name="filename"></param>
        public static void WriteElements(Mesh mesh, string filename)
        {
            Otri tri = default(Otri);
            Vertex p1, p2, p3;
            bool regions = mesh.behavior.useRegions;

            int j = 0;

            tri.orient = 0;

            using (StreamWriter writer = new StreamWriter(filename))
            {
                // Number of triangles, vertices per triangle, attributes per triangle.
                writer.WriteLine("{0} 3 {1}", mesh.triangles.Count, regions ? 1 : 0);

                foreach (var item in mesh.triangles.Values)
                {
                    tri.triangle = item;

                    p1 = tri.Org();
                    p2 = tri.Dest();
                    p3 = tri.Apex();

                    // Triangle number, indices for three vertices.
                    writer.Write("{0} {1} {2} {3}", j, p1.id, p2.id, p3.id);

                    if (regions)
                    {
                        writer.Write(" {0}", tri.triangle.region);
                    }

                    writer.WriteLine();

                    // Number elements
                    item.id = j++;
                }
            }
        }

        /// <summary>
        /// Write the segments and holes to a .poly file.
        /// </summary>
        /// <param name="mesh"></param>
        /// <param name="filename"></param>
        public static void WritePoly(Mesh mesh, string filename)
        {
            FileWriter.WritePoly(mesh, filename, true);
        }

        /// <summary>
        /// Write the segments and holes to a .poly file.
        /// </summary>
        /// <param name="mesh">Data source.</param>
        /// <param name="filename">File name.</param>
        /// <param name="writeNodes">Write nodes into this file.</param>
        /// <remarks>If the nodes should not be written into this file, 
        /// make sure a .node file was written before, so that the nodes 
        /// are numbered right.</remarks>
        public static void WritePoly(Mesh mesh, string filename, bool writeNodes)
        {
            Osub subseg = default(Osub);
            Vertex pt1, pt2;

            bool useBoundaryMarkers = mesh.behavior.UseBoundaryMarkers;

            using (StreamWriter writer = new StreamWriter(filename))
            {
                if (writeNodes)
                {
                    // Write nodes to this file.
                    FileWriter.WriteNodes(writer, mesh);
                }
                else
                {
                    // The zero indicates that the vertices are in a separate .node file.
                    // Followed by number of dimensions, number of vertex attributes,
                    // and number of boundary markers (zero or one).
                    writer.WriteLine("0 {0} {1} {2}", mesh.mesh_dim, mesh.nextras,
                        useBoundaryMarkers ? "1" : "0");
                }

                // Number of segments, number of boundary markers (zero or one).
                writer.WriteLine("{0} {1}", mesh.subsegs.Count,
                    useBoundaryMarkers ? "1" : "0");

                subseg.orient = 0;

                int j = 0;
                foreach (var item in mesh.subsegs.Values)
                {
                    subseg.seg = item;

                    pt1 = subseg.Org();
                    pt2 = subseg.Dest();

                    // Segment number, indices of its two endpoints, and possibly a marker.
                    if (useBoundaryMarkers)
                    {
                        writer.WriteLine("{0} {1} {2} {3}", j, pt1.id, pt2.id, subseg.seg.boundary);
                    }
                    else
                    {
                        writer.WriteLine("{0} {1} {2}", j, pt1.id, pt2.id);
                    }

                    j++;
                }

                // Holes
                j = 0;
                writer.WriteLine("{0}", mesh.holes.Count);
                foreach (var hole in mesh.holes)
                {
                    writer.WriteLine("{0} {1} {2}", j++, hole.X.ToString(nfi), hole.Y.ToString(nfi));
                }

                // Regions
                if (mesh.regions.Count > 0)
                {
                    j = 0;
                    writer.WriteLine("{0}", mesh.regions.Count);
                    foreach (var region in mesh.regions)
                    {
                        writer.WriteLine("{0} {1} {2} {3}", j, region.point.X.ToString(nfi),
                            region.point.Y.ToString(nfi), region.id);

                        j++;
                    }
                }
            }
        }

        /// <summary>
        /// Write the edges to an .edge file.
        /// </summary>
        /// <param name="mesh"></param>
        /// <param name="filename"></param>
        public static void WriteEdges(Mesh mesh, string filename)
        {
            Otri tri = default(Otri), trisym = default(Otri);
            Osub checkmark = default(Osub);
            Vertex p1, p2;

            Behavior behavior = mesh.behavior;

            using (StreamWriter writer = new StreamWriter(filename))
            {
                // Number of edges, number of boundary markers (zero or one).
                writer.WriteLine("{0} {1}", mesh.edges, behavior.UseBoundaryMarkers ? "1" : "0");

                long index = 0;
                // To loop over the set of edges, loop over all triangles, and look at
                // the three edges of each triangle.  If there isn't another triangle
                // adjacent to the edge, operate on the edge.  If there is another
                // adjacent triangle, operate on the edge only if the current triangle
                // has a smaller pointer than its neighbor.  This way, each edge is
                // considered only once.
                foreach (var item in mesh.triangles.Values)
                {
                    tri.triangle = item;

                    for (tri.orient = 0; tri.orient < 3; tri.orient++)
                    {
                        tri.Sym(ref trisym);
                        if ((tri.triangle.id < trisym.triangle.id) || (trisym.triangle == Mesh.dummytri))
                        {
                            p1 = tri.Org();
                            p2 = tri.Dest();

                            if (behavior.UseBoundaryMarkers)
                            {
                                // Edge number, indices of two endpoints, and a boundary marker.
                                // If there's no subsegment, the boundary marker is zero.
                                if (behavior.useSegments)
                                {
                                    tri.SegPivot(ref checkmark);

                                    if (checkmark.seg == Mesh.dummysub)
                                    {
                                        writer.WriteLine("{0} {1} {2} {3}", index, p1.id, p2.id, 0);
                                    }
                                    else
                                    {
                                        writer.WriteLine("{0} {1} {2} {3}", index, p1.id, p2.id,
                                                checkmark.seg.boundary);
                                    }
                                }
                                else
                                {
                                    writer.WriteLine("{0} {1} {2} {3}", index, p1.id, p2.id,
                                            trisym.triangle == Mesh.dummytri ? "1" : "0");
                                }
                            }
                            else
                            {
                                // Edge number, indices of two endpoints.
                                writer.WriteLine("{0} {1} {2}", index, p1.id, p2.id);
                            }

                            index++;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Write the triangle neighbors to a .neigh file.
        /// </summary>
        /// <param name="mesh"></param>
        /// <param name="filename"></param>
        /// <remarks>WARNING: Be sure WriteElements has been called before, 
        /// so the elements are numbered right!</remarks>
        public static void WriteNeighbors(Mesh mesh, string filename)
        {
            Otri tri = default(Otri), trisym = default(Otri);
            int n1, n2, n3;
            int i = 0;

            using (StreamWriter writer = new StreamWriter(filename))
            {
                // Number of triangles, three neighbors per triangle.
                writer.WriteLine("{0} 3", mesh.triangles.Count);

                Mesh.dummytri.id = -1;

                foreach (var item in mesh.triangles.Values)
                {
                    tri.triangle = item;

                    tri.orient = 1;
                    tri.Sym(ref trisym);
                    n1 = trisym.triangle.id;

                    tri.orient = 2;
                    tri.Sym(ref trisym);
                    n2 = trisym.triangle.id;

                    tri.orient = 0;
                    tri.Sym(ref trisym);
                    n3 = trisym.triangle.id;

                    // Triangle number, neighboring triangle numbers.
                    writer.WriteLine("{0} {1} {2} {3}", i++, n1, n2, n3);
                }
            }
        }

        /// <summary>
        /// Write the Voronoi diagram to a .voro file.
        /// </summary>
        /// <param name="mesh"></param>
        /// <param name="filename"></param>
        /// <returns></returns>
        /// <remarks>
        /// The Voronoi diagram is the geometric dual of the Delaunay triangulation.
        /// Hence, the Voronoi vertices are listed by traversing the Delaunay
        /// triangles, and the Voronoi edges are listed by traversing the Delaunay
        /// edges.
        ///
        /// WARNING:  In order to assign numbers to the Voronoi vertices, this
        /// procedure messes up the subsegments or the extra nodes of every
        /// element.  Hence, you should call this procedure last.</remarks>
        public static void WriteVoronoi(Mesh mesh, string filename)
        {
            Otri tri = default(Otri), trisym = default(Otri);
            Vertex torg, tdest, tapex;
            Point circumcenter;
            float xi = 0, eta = 0;

            int p1, p2, index = 0;
            tri.orient = 0;

            using (StreamWriter writer = new StreamWriter(filename))
            {
                // Number of triangles, two dimensions, number of vertex attributes, no markers.
                writer.WriteLine("{0} 2 {1} 0", mesh.triangles.Count, mesh.nextras);

                foreach (var item in mesh.triangles.Values)
                {
                    tri.triangle = item;
                    torg = tri.Org();
                    tdest = tri.Dest();
                    tapex = tri.Apex();
                    circumcenter = Primitives.FindCircumcenter(torg, tdest, tapex, ref xi, ref eta);

                    // X and y coordinates.
                    writer.Write("{0} {1} {2}", index, circumcenter.X.ToString(nfi),
                        circumcenter.Y.ToString(nfi));

                    for (int i = 0; i < mesh.nextras; i++)
                    {
                        writer.Write(" 0");
                        // TODO
                        // Interpolate the vertex attributes at the circumcenter.
                        //writer.Write(" {0}", torg.attribs[i] + xi * (tdes.attribst[i] - torg.attribs[i]) + 
                        //    eta * (tapex.attribs[i] - torg.attribs[i]));
                    }
                    writer.WriteLine();

                    tri.triangle.id = index++;
                }


                // Number of edges, zero boundary markers.
                writer.WriteLine("{0} 0", mesh.edges);

                index = 0;
                // To loop over the set of edges, loop over all triangles, and look at
                // the three edges of each triangle.  If there isn't another triangle
                // adjacent to the edge, operate on the edge.  If there is another
                // adjacent triangle, operate on the edge only if the current triangle
                // has a smaller pointer than its neighbor.  This way, each edge is
                // considered only once.
                foreach (var item in mesh.triangles.Values)
                {
                    tri.triangle = item;

                    for (tri.orient = 0; tri.orient < 3; tri.orient++)
                    {
                        tri.Sym(ref trisym);
                        if ((tri.triangle.id < trisym.triangle.id) || (trisym.triangle == Mesh.dummytri))
                        {
                            // Find the number of this triangle (and Voronoi vertex).
                            p1 = tri.triangle.id;

                            if (trisym.triangle == Mesh.dummytri)
                            {
                                torg = tri.Org();
                                tdest = tri.Dest();

                                // Write an infinite ray. Edge number, index of one endpoint,
                                // -1, and x and y coordinates of a vector representing the
                                // direction of the ray.
                                writer.WriteLine("{0} {1} -1 {2} {3}", index, p1,
                                        (tdest[1] - torg[1]).ToString(nfi),
                                        (torg[0] - tdest[0]).ToString(nfi));
                            }
                            else
                            {
                                // Find the number of the adjacent triangle (and Voronoi vertex).
                                p2 = trisym.triangle.id;
                                // Finite edge.  Write indices of two endpoints.
                                writer.WriteLine("{0} {1} {2}", index, p1, p2);
                            }

                            index++;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Write the triangulation to an .off file.
        /// </summary>
        /// <param name="mesh"></param>
        /// <param name="filename"></param>
        /// <remarks>
        /// OFF stands for the Object File Format, a format used by the Geometry
        /// Center's Geomview package.
        /// </remarks>
        public static void WriteOffFile(Mesh mesh, string filename)
        {
            Otri tri;
            Vertex p1, p2, p3;

            long outvertices = mesh.vertices.Count;

            if (mesh.behavior.Jettison)
            {
                outvertices = mesh.vertices.Count - mesh.undeads;
            }

            int index = 0;

            using (StreamWriter writer = new StreamWriter(filename))
            {
                writer.WriteLine("OFF");
                writer.WriteLine("{0}  {1}  {2}", outvertices, mesh.triangles.Count, mesh.edges);

                foreach (var item in mesh.vertices.Values)
                {
                    p1 = item;

                    if (!mesh.behavior.Jettison || p1.type != VertexType.UndeadVertex)
                    {
                        // The "0.0" is here because the OFF format uses 3D coordinates.
                        writer.WriteLine(" {0}  {1}  0.0", p1[0].ToString(nfi), p1[1].ToString(nfi));

                        p1.id = index++;
                    }
                }

                // Write the triangles.
                tri.orient = 0;
                foreach (var item in mesh.triangles.Values)
                {
                    tri.triangle = item;

                    p1 = tri.Org();
                    p2 = tri.Dest();
                    p3 = tri.Apex();

                    // The "3" means a three-vertex polygon.
                    writer.WriteLine(" 3   {0}  {1}  {2}", p1.id, p2.id, p3.id);
                }
            }
        }
    }
}
