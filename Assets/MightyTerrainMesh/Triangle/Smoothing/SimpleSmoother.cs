// -----------------------------------------------------------------------
// <copyright file="SimpleSmoother.cs" company="">
// Triangle.NET code by Christian Woltering, http://triangle.codeplex.com/
// </copyright>
// -----------------------------------------------------------------------

namespace TriangleNet.Smoothing
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using TriangleNet.Geometry;
    using TriangleNet.Tools;

    /// <summary>
    /// Simple mesh smoother implementation.
    /// </summary>
    /// <remarks>
    /// Vertices wich should not move (e.g. segment vertices) MUST have a
    /// boundary mark greater than 0.
    /// </remarks>
    public class SimpleSmoother : ISmoother
    {
        Mesh mesh;

        public SimpleSmoother(Mesh mesh)
        {
            this.mesh = mesh;
        }

        public void Smooth()
        {
            mesh.behavior.Quality = false;

            // Take a few smoothing rounds.
            for (int i = 0; i < 5; i++)
            {
                Step();

                // Actually, we only want to rebuild, if mesh is no longer
                // Delaunay. Flipping edges could be the right choice instead 
                // of re-triangulating...
                mesh.Triangulate(Rebuild());
            }
        }

        /// <summary>
        /// Smooth all free nodes.
        /// </summary>
        private void Step()
        {
            BoundedVoronoi voronoi = new BoundedVoronoi(this.mesh, false);

            var cells = voronoi.Regions;

            float x, y;
            int n;

            foreach (var cell in cells)
            {
                n = 0;
                x = y = 0.0f;
                foreach (var p in cell.Vertices)
                {
                    n++;
                    x += p.x;
                    y += p.y;
                }

                cell.Generator.x = x / n;
                cell.Generator.y = y / n;
            }
        }

        /// <summary>
        /// Rebuild the input geometry.
        /// </summary>
        private InputGeometry Rebuild()
        {
            InputGeometry geometry = new InputGeometry(mesh.vertices.Count);

            foreach (var vertex in mesh.vertices.Values)
            {
                geometry.AddPoint(vertex.x, vertex.y, vertex.mark);
            }

            foreach (var segment in mesh.subsegs.Values)
            {
                geometry.AddSegment(segment.P0, segment.P1, segment.Boundary);
            }

            foreach (var hole in mesh.holes)
            {
                geometry.AddHole(hole.x, hole.y);
            }

            foreach (var region in mesh.regions)
            {
                geometry.AddRegion(region.point.x, region.point.y, region.id);
            }

            return geometry;
        }
    }
}
