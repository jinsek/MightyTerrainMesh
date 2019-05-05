// -----------------------------------------------------------------------
// <copyright file="QuadTree.cs" company="">
// Original code by Frank Dockhorn, http://sourceforge.net/projects/quadtreesim/
// Triangle.NET code by Christian Woltering, http://triangle.codeplex.com/
// </copyright>
// -----------------------------------------------------------------------

namespace TriangleNet.Tools
{
    using System.Collections.Generic;
    using System.Linq;
    using TriangleNet.Geometry;

    /// <summary>
    /// A Quadtree implementation optimised for triangles.
    /// </summary>
    public class QuadTree
    {
        QuadNode root;

        internal ITriangle[] triangles;

        internal int sizeBound;
        internal int maxDepth;

        /// <summary>
        /// Initializes a new instance of the <see cref="QuadTree" /> class.
        /// </summary>
        /// <param name="mesh">Mesh containing triangles.</param>
        /// <param name="maxDepth">The maximum depth of the tree.</param>
        /// <param name="sizeBound">The maximum number of triangles contained in a leaf.</param>
        /// <remarks>
        /// The quadtree does not track changes of the mesh. If a mesh is refined or
        /// changed in any other way, a new quadtree has to be built to make the point
        /// location work.
        /// 
        /// A node of the tree will be split, if its level if less than the max depth parameter
        /// AND the number of triangles in the node is greater than the size bound.
        /// </remarks>
        public QuadTree(Mesh mesh, int maxDepth = 10, int sizeBound = 10)
        {
            this.maxDepth = maxDepth;
            this.sizeBound = sizeBound;

            triangles = mesh.Triangles.ToArray();

            int currentDepth = 0;

            root = new QuadNode(mesh.Bounds, this, true);
            root.CreateSubRegion(++currentDepth);
        }

        public ITriangle Query(float x, float y)
        {
            var point = new Point(x, y);
            var indices = root.FindTriangles(point);

            var result = new List<ITriangle>();

            foreach (var i in indices)
            {
                var tri = this.triangles[i];

                if (IsPointInTriangle(point, tri.GetVertex(0), tri.GetVertex(1), tri.GetVertex(2)))
                {
                    result.Add(tri);
                }
            }

            return result.FirstOrDefault();
        }

        /// <summary>
        /// Test, if a given point lies inside a triangle.
        /// </summary>
        /// <param name="p">Point to locate.</param>
        /// <param name="t0">Corner point of triangle.</param>
        /// <param name="t1">Corner point of triangle.</param>
        /// <param name="t2">Corner point of triangle.</param>
        /// <returns>True, if point is inside or on the edge of this triangle.</returns>
        internal static bool IsPointInTriangle(Point p, Point t0, Point t1, Point t2)
        {
            // TODO: no need to create new Point instances here
            Point d0 = new Point(t1.X - t0.X, t1.Y - t0.Y);
            Point d1 = new Point(t2.X - t0.X, t2.Y - t0.Y);
            Point d2 = new Point(p.X - t0.X, p.Y - t0.Y);

            // crossproduct of (0, 0, 1) and d0
            Point c0 = new Point(-d0.Y, d0.X);

            // crossproduct of (0, 0, 1) and d1
            Point c1 = new Point(-d1.Y, d1.X);

            // Linear combination d2 = s * d0 + v * d1.
            //
            // Multiply both sides of the equation with c0 and c1
            // and solve for s and v respectively
            //
            // s = d2 * c1 / d0 * c1
            // v = d2 * c0 / d1 * c0

            float s = DotProduct(d2, c1) / DotProduct(d0, c1);
            float v = DotProduct(d2, c0) / DotProduct(d1, c0);

            if (s >= 0 && v >= 0 && ((s + v) <= 1))
            {
                // Point is inside or on the edge of this triangle.
                return true;
            }
            return false;
        }

        internal static float DotProduct(Point p, Point q)
        {
            return p.X * q.X + p.Y * q.Y;
        }
    }

    #region QuadNode class

    /// <summary>
    /// A node of the quadtree.
    /// </summary>
    class QuadNode
    {
        const int SW = 0;
        const int SE = 1;
        const int NW = 2;
        const int NE = 3;

        static readonly byte[] BITVECTOR = { 0x1, 0x2, 0x4, 0x8 };

        BoundingBox bounds;
        Point pivot;
        QuadTree tree;
        QuadNode[] regions;
        List<int> triangles;

        byte bitRegions;

        public QuadNode(BoundingBox box, QuadTree tree)
            : this(box, tree, false)
        {
        }

        public QuadNode(BoundingBox box, QuadTree tree, bool init)
        {
            this.tree = tree;

            this.bounds = new BoundingBox(box.Xmin, box.Ymin, box.Xmax, box.Ymax);
            this.pivot = new Point((box.Xmin + box.Xmax) / 2, (box.Ymin + box.Ymax) / 2);

            this.bitRegions = 0;

            this.regions = new QuadNode[4];
            this.triangles = new List<int>();

            if (init)
            {
                // Allocate memory upfront
                triangles.Capacity = tree.triangles.Length;

                foreach (var tri in tree.triangles)
                {
                    triangles.Add(tri.ID);
                }
            }
        }

        public List<int> FindTriangles(Point searchPoint)
        {
            int region = FindRegion(searchPoint);
            if (regions[region] == null)
            {
                return triangles;
            }
            return regions[region].FindTriangles(searchPoint);
        }

        public void CreateSubRegion(int currentDepth)
        {
            // The four sub regions of the quad tree
            //   +--------------+
            //   |  nw  |  ne   |
            //   |------+pivot--|
            //   |  sw  |  se   |
            //   +--------------+
            BoundingBox box;

            // 1. region south west
            box = new BoundingBox(bounds.Xmin, bounds.Ymin, pivot.X, pivot.Y);
            regions[0] = new QuadNode(box, tree);

            // 2. region south east
            box = new BoundingBox(pivot.X, bounds.Ymin, bounds.Xmax, pivot.Y);
            regions[1] = new QuadNode(box, tree);

            // 3. region north west
            box = new BoundingBox(bounds.Xmin, pivot.Y, pivot.X, bounds.Ymax);
            regions[2] = new QuadNode(box, tree);

            // 4. region north east
            box = new BoundingBox(pivot.X, pivot.Y, bounds.Xmax, bounds.Ymax);
            regions[3] = new QuadNode(box, tree);

            Point[] triangle = new Point[3];

            // Find region for every triangle vertex
            foreach (var index in triangles)
            {
                ITriangle tri = tree.triangles[index];

                triangle[0] = tri.GetVertex(0);
                triangle[1] = tri.GetVertex(1);
                triangle[2] = tri.GetVertex(2);

                AddTriangleToRegion(triangle, tri.ID);
            }

            for (int i = 0; i < 4; i++)
            {
                if (regions[i].triangles.Count > tree.sizeBound && currentDepth < tree.maxDepth)
                {
                    regions[i].CreateSubRegion(currentDepth + 1);
                }
            }
        }

        void AddTriangleToRegion(Point[] triangle, int index)
        {
            bitRegions = 0;
            if (QuadTree.IsPointInTriangle(pivot, triangle[0], triangle[1], triangle[2]))
            {
                AddToRegion(index, SW);
                AddToRegion(index, SE);
                AddToRegion(index, NW);
                AddToRegion(index, NE);
                return;
            }

            FindTriangleIntersections(triangle, index);

            if (bitRegions == 0)
            {
                // we didn't find any intersection so we add this triangle to a point's region		
                int region = FindRegion(triangle[0]);
                regions[region].triangles.Add(index);
            }
        }

        void FindTriangleIntersections(Point[] triangle, int index)
        {
            // PLEASE NOTE:   Handling of component comparison is tightly associated with the implementation 
            //                of the findRegion() function. That means when the point to be compared equals 
            //                the pivot point the triangle must be put at least into region 2.
            // Linear equations are in parametric form.
            //                m_pivot.dx = triangle[0].dx + t * (triangle[1].dx - triangle[0].dx)
            //                m_pivot.dy = triangle[0].dy + t * (triangle[1].dy - triangle[0].dy)

            int k = 2;

            float dx, dy;
            // Iterate through all triangle laterals and find bounding box intersections
            for (int i = 0; i < 3; k = i++)
            {
                dx = triangle[i].X - triangle[k].X;
                dy = triangle[i].Y - triangle[k].Y;

                if (dx != 0.0)
                {
                    FindIntersectionsWithX(dx, dy, triangle, index, k);
                }
                if (dy != 0.0)
                {
                    FindIntersectionsWithY(dx, dy, triangle, index, k);
                }
            }
        }

        void FindIntersectionsWithX(float dx, float dy, Point[] triangle, int index, int k)
        {
            // find intersection with plane x = m_pivot.dX
            float t = (pivot.X - triangle[k].X) / dx;

            if (t < (1 + UnityEngine.Mathf.Epsilon) && t > -UnityEngine.Mathf.Epsilon)
            {
                // we have an intersection
                float yComponent = triangle[k].Y + t * dy;

                if (yComponent < pivot.Y)
                {
                    if (yComponent >= bounds.Ymin)
                    {
                        AddToRegion(index, SW);
                        AddToRegion(index, SE);
                    }
                }
                else if (yComponent <= bounds.Ymax)
                {
                    AddToRegion(index, NW);
                    AddToRegion(index, NE);
                }
            }
            // find intersection with plane x = m_boundingBox[0].dX
            t = (bounds.Xmin - triangle[k].X) / dx;
            if (t < (1 + UnityEngine.Mathf.Epsilon) && t > -UnityEngine.Mathf.Epsilon)
            {
                // we have an intersection
                float yComponent = triangle[k].Y + t * dy;

                if (yComponent <= pivot.Y && yComponent >= bounds.Ymin)
                {
                    AddToRegion(index, SW);
                }
                else if (yComponent >= pivot.Y && yComponent <= bounds.Ymax)
                {
                    AddToRegion(index, NW);
                }
            }
            // find intersection with plane x = m_boundingBox[1].dX
            t = (bounds.Xmax - triangle[k].X) / dx;
            if (t < (1 + UnityEngine.Mathf.Epsilon) && t > -UnityEngine.Mathf.Epsilon)
            {
                // we have an intersection
                float yComponent = triangle[k].Y + t * dy;

                if (yComponent <= pivot.Y && yComponent >= bounds.Ymin)
                {
                    AddToRegion(index, SE);
                }
                else if (yComponent >= pivot.Y && yComponent <= bounds.Ymax)
                {
                    AddToRegion(index, NE);
                }
            }
        }

        void FindIntersectionsWithY(float dx, float dy, Point[] triangle, int index, int k)
        {
            // find intersection with plane y = m_pivot.dY
            float t = (pivot.Y - triangle[k].Y) / (dy);
            if (t < (1 + UnityEngine.Mathf.Epsilon) && t > -UnityEngine.Mathf.Epsilon)
            {
                // we have an intersection
                float xComponent = triangle[k].X + t * (dy);

                if (xComponent > pivot.X)
                {
                    if (xComponent <= bounds.Xmax)
                    {
                        AddToRegion(index, SE);
                        AddToRegion(index, NE);
                    }
                }
                else if (xComponent >= bounds.Xmin)
                {
                    AddToRegion(index, SW);
                    AddToRegion(index, NW);
                }
            }
            // find intersection with plane y = m_boundingBox[0].dY
            t = (bounds.Ymin - triangle[k].Y) / dy;
            if (t < (1 + UnityEngine.Mathf.Epsilon) && t > -UnityEngine.Mathf.Epsilon)
            {
                // we have an intersection
                float xComponent = triangle[k].X + t * dx;

                if (xComponent <= pivot.X && xComponent >= bounds.Xmin)
                {
                    AddToRegion(index, SW);
                }
                else if (xComponent >= pivot.X && xComponent <= bounds.Xmax)
                {
                    AddToRegion(index, SE);
                }
            }
            // find intersection with plane y = m_boundingBox[1].dY
            t = (bounds.Ymax - triangle[k].Y) / dy;
            if (t < (1 + UnityEngine.Mathf.Epsilon) && t > -UnityEngine.Mathf.Epsilon)
            {
                // we have an intersection
                float xComponent = triangle[k].X + t * dx;

                if (xComponent <= pivot.X && xComponent >= bounds.Xmin)
                {
                    AddToRegion(index, NW);
                }
                else if (xComponent >= pivot.X && xComponent <= bounds.Xmax)
                {
                    AddToRegion(index, NE);
                }
            }
        }

        int FindRegion(Point point)
        {
            int b = 2;
            if (point.Y < pivot.Y)
            {
                b = 0;
            }
            if (point.X > pivot.X)
            {
                b++;
            }
            return b;
        }

        void AddToRegion(int index, int region)
        {
            //if (!(m_bitRegions & BITVECTOR[region]))
            if ((bitRegions & BITVECTOR[region]) == 0)
            {
                regions[region].triangles.Add(index);
                bitRegions |= BITVECTOR[region];
            }
        }
    }

    #endregion
}
