// -----------------------------------------------------------------------
// <copyright file="Statistic.cs">
// Original Triangle code by Jonathan Richard Shewchuk, http://www.cs.cmu.edu/~quake/triangle.html
// Triangle.NET code by Christian Woltering, http://triangle.codeplex.com/
// </copyright>
// -----------------------------------------------------------------------

namespace TriangleNet.Tools
{
    using System;
    using System.Text;
    using TriangleNet.Data;
    using TriangleNet.Geometry;

    /// <summary>
    /// Gather mesh statistics.
    /// </summary>
    public class Statistic
    {
        #region Static members

        /// <summary>
        /// Number of incircle tests performed.
        /// </summary>
        public static long InCircleCount = 0;
        public static long InCircleCountDecimal = 0;

        /// <summary>
        /// Number of counterclockwise tests performed.
        /// </summary>
        public static long CounterClockwiseCount = 0;
        public static long CounterClockwiseCountDecimal = 0;

        /// <summary>
        /// Number of 3D orientation tests performed.
        /// </summary>
        public static long Orient3dCount = 0;

        /// <summary>
        /// Number of right-of-hyperbola tests performed.
        /// </summary>
        public static long HyperbolaCount = 0;

        /// <summary>
        /// // Number of circumcenter calculations performed.
        /// </summary>
        public static long CircumcenterCount = 0;

        /// <summary>
        /// Number of circle top calculations performed.
        /// </summary>
        public static long CircleTopCount = 0;

        /// <summary>
        /// Number of vertex relocations.
        /// </summary>
        public static long RelocationCount = 0;

        #endregion

        #region Properties

        float minEdge = 0;
        /// <summary>
        /// Gets the shortest edge.
        /// </summary>
        public float ShortestEdge { get { return minEdge; } }

        float maxEdge = 0;
        /// <summary>
        /// Gets the longest edge.
        /// </summary>
        public float LongestEdge { get { return maxEdge; } }

        //
        float minAspect = 0;
        /// <summary>
        /// Gets the shortest altitude.
        /// </summary>
        public float ShortestAltitude { get { return minAspect; } }

        float maxAspect = 0;
        /// <summary>
        /// Gets the largest aspect ratio.
        /// </summary>
        public float LargestAspectRatio { get { return maxAspect; } }

        float minArea = 0;
        /// <summary>
        /// Gets the smallest area.
        /// </summary>
        public float SmallestArea { get { return minArea; } }

        float maxArea = 0;
        /// <summary>
        /// Gets the largest area.
        /// </summary>
        public float LargestArea { get { return maxArea; } }

        float minAngle = 0;
        /// <summary>
        /// Gets the smallest angle.
        /// </summary>
        public float SmallestAngle { get { return minAngle; } }

        float maxAngle = 0;
        /// <summary>
        /// Gets the largest angle.
        /// </summary>
        public float LargestAngle { get { return maxAngle; } }

        int inVetrices = 0;
        /// <summary>
        /// Gets the number of input vertices.
        /// </summary>
        public int InputVertices { get { return inVetrices; } }

        int inTriangles = 0;
        /// <summary>
        /// Gets the number of input triangles.
        /// </summary>
        public int InputTriangles { get { return inTriangles; } }

        int inSegments = 0;
        /// <summary>
        /// Gets the number of input segments.
        /// </summary>
        public int InputSegments { get { return inSegments; } }

        int inHoles = 0;
        /// <summary>
        /// Gets the number of input holes.
        /// </summary>
        public int InputHoles { get { return inHoles; } }

        int outVertices = 0;
        /// <summary>
        /// Gets the number of mesh vertices.
        /// </summary>
        public int Vertices { get { return outVertices; } }

        int outTriangles = 0;
        /// <summary>
        /// Gets the number of mesh triangles.
        /// </summary>
        public int Triangles { get { return outTriangles; } }

        int outEdges = 0;
        /// <summary>
        /// Gets the number of mesh edges.
        /// </summary>
        public int Edges { get { return outEdges; } }

        int boundaryEdges = 0;
        /// <summary>
        /// Gets the number of exterior boundary edges.
        /// </summary>
        public int BoundaryEdges { get { return boundaryEdges; } }

        int intBoundaryEdges = 0;
        /// <summary>
        /// Gets the number of interior boundary edges.
        /// </summary>
        public int InteriorBoundaryEdges { get { return intBoundaryEdges; } }

        int constrainedEdges = 0;
        /// <summary>
        /// Gets the number of constrained edges.
        /// </summary>
        public int ConstrainedEdges { get { return constrainedEdges; } }

        int[] angleTable;
        /// <summary>
        /// Gets the angle histogram.
        /// </summary>
        public int[] AngleHistogram { get { return angleTable; } }

        int[] minAngles;
        /// <summary>
        /// Gets the min angles histogram.
        /// </summary>
        public int[] MinAngleHistogram { get { return minAngles; } }

        int[] maxAngles;
        /// <summary>
        /// Gets the max angles histogram.
        /// </summary>
        public int[] MaxAngleHistogram { get { return maxAngles; } }

        #endregion

        #region Private methods

        private void GetAspectHistogram(Mesh mesh)
        {
            int[] aspecttable;
            float[] ratiotable;

            aspecttable = new int[16];
            ratiotable = new float[] { 
                1.5f, 2.0f, 2.5f, 3.0f, 4.0f, 6.0f, 10.0f, 15.0f, 25.0f, 50.0f, 
                100.0f, 300.0f, 1000.0f, 10000.0f, 100000.0f, 0.0f };


            Otri tri = default(Otri);
            Vertex[] p = new Vertex[3];
            float[] dx = new float[3], dy = new float[3];
            float[] edgelength = new float[3];
            float triarea;
            float trilongest2;
            float triminaltitude2;
            float triaspect2;

            int aspectindex;
            int i, j, k;

            tri.orient = 0;
            foreach (var t in mesh.triangles.Values)
            {
                tri.triangle = t;
                p[0] = tri.Org();
                p[1] = tri.Dest();
                p[2] = tri.Apex();
                trilongest2 = 0.0f;

                for (i = 0; i < 3; i++)
                {
                    j = plus1Mod3[i];
                    k = minus1Mod3[i];
                    dx[i] = p[j].x - p[k].x;
                    dy[i] = p[j].y - p[k].y;
                    edgelength[i] = dx[i] * dx[i] + dy[i] * dy[i];
                    if (edgelength[i] > trilongest2)
                    {
                        trilongest2 = edgelength[i];
                    }
                }

                //triarea = Primitives.CounterClockwise(p[0], p[1], p[2]);
                triarea = UnityEngine.Mathf.Abs((p[2].x - p[0].x) * (p[1].y - p[0].y) -
                    (p[1].x - p[0].x) * (p[2].y - p[0].y)) / 2.0f;

                triminaltitude2 = triarea * triarea / trilongest2;

                triaspect2 = trilongest2 / triminaltitude2;

                aspectindex = 0;
                while ((triaspect2 > ratiotable[aspectindex] * ratiotable[aspectindex]) && (aspectindex < 15))
                {
                    aspectindex++;
                }
                aspecttable[aspectindex]++;
            }
        }

        #endregion

        static readonly int[] plus1Mod3 = { 1, 2, 0 };
        static readonly int[] minus1Mod3 = { 2, 0, 1 };

        /// <summary>
        /// Update statistics about the quality of the mesh.
        /// </summary>
        /// <param name="mesh"></param>
        public void Update(Mesh mesh, int sampleDegrees)
        {
            inVetrices = mesh.invertices;
            inTriangles = mesh.inelements;
            inSegments = mesh.insegments;
            inHoles = mesh.holes.Count;
            outVertices = mesh.vertices.Count - mesh.undeads;
            outTriangles = mesh.triangles.Count;
            outEdges = (int)mesh.edges;
            boundaryEdges = (int)mesh.hullsize;
            intBoundaryEdges = mesh.subsegs.Count - (int)mesh.hullsize;
            constrainedEdges = mesh.subsegs.Count;

            Point[] p = new Point[3];

            int k1, k2;
            int degreeStep;

            //sampleDegrees = 36; // sample every 5 degrees
            //sampleDegrees = 45; // sample every 4 degrees
            sampleDegrees = 60; // sample every 3 degrees

            float[] cosSquareTable = new float[sampleDegrees / 2 - 1];
            float[] dx = new float[3];
            float[] dy = new float[3];
            float[] edgeLength = new float[3];
            float dotProduct;
            float cosSquare;
            float triArea;
            float triLongest2;
            float triMinAltitude2;
            float triAspect2;

            float radconst = UnityEngine.Mathf.PI / sampleDegrees;
            float degconst = 180.0f / UnityEngine.Mathf.PI;

            // New angle table
            angleTable = new int[sampleDegrees];
            minAngles = new int[sampleDegrees];
            maxAngles = new int[sampleDegrees];

            for (int i = 0; i < sampleDegrees / 2 - 1; i++)
            {
                cosSquareTable[i] = UnityEngine.Mathf.Cos(radconst * (i + 1));
                cosSquareTable[i] = cosSquareTable[i] * cosSquareTable[i];
            }
            for (int i = 0; i < sampleDegrees; i++)
            {
                angleTable[i] = 0;
            }

            minAspect = mesh.bounds.Width + mesh.bounds.Height;
            minAspect = minAspect * minAspect;
            maxAspect = 0.0f;
            minEdge = minAspect;
            maxEdge = 0.0f;
            minArea = minAspect;
            maxArea = 0.0f;
            minAngle = 0.0f;
            maxAngle = 2.0f;

            bool acuteBiggest = true;
            bool acuteBiggestTri = true;

            float triMinAngle, triMaxAngle = 1;

            foreach (var tri in mesh.triangles.Values)
            {
                triMinAngle = 0; // Min angle:  0 < a <  60 degress
                triMaxAngle = 1; // Max angle: 60 < a < 180 degress

                p[0] = tri.vertices[0];
                p[1] = tri.vertices[1];
                p[2] = tri.vertices[2];

                triLongest2 = 0.0f;

                for (int i = 0; i < 3; i++)
                {
                    k1 = plus1Mod3[i];
                    k2 = minus1Mod3[i];

                    dx[i] = p[k1].X - p[k2].X;
                    dy[i] = p[k1].Y - p[k2].Y;

                    edgeLength[i] = dx[i] * dx[i] + dy[i] * dy[i];

                    if (edgeLength[i] > triLongest2)
                    {
                        triLongest2 = edgeLength[i];
                    }

                    if (edgeLength[i] > maxEdge)
                    {
                        maxEdge = edgeLength[i];
                    }

                    if (edgeLength[i] < minEdge)
                    {
                        minEdge = edgeLength[i];
                    }
                }

                //triarea = Primitives.CounterClockwise(p[0], p[1], p[2]);
                triArea = UnityEngine.Mathf.Abs((p[2].X - p[0].X) * (p[1].Y - p[0].Y) -
                    (p[1].X - p[0].X) * (p[2].Y - p[0].Y));

                if (triArea < minArea)
                {
                    minArea = triArea;
                }

                if (triArea > maxArea)
                {
                    maxArea = triArea;
                }

                triMinAltitude2 = triArea * triArea / triLongest2;
                if (triMinAltitude2 < minAspect)
                {
                    minAspect = triMinAltitude2;
                }

                triAspect2 = triLongest2 / triMinAltitude2;
                if (triAspect2 > maxAspect)
                {
                    maxAspect = triAspect2;
                }

                for (int i = 0; i < 3; i++)
                {
                    k1 = plus1Mod3[i];
                    k2 = minus1Mod3[i];

                    dotProduct = dx[k1] * dx[k2] + dy[k1] * dy[k2];
                    cosSquare = dotProduct * dotProduct / (edgeLength[k1] * edgeLength[k2]);
                    degreeStep = sampleDegrees / 2 - 1;

                    for (int j = degreeStep - 1; j >= 0; j--)
                    {
                        if (cosSquare > cosSquareTable[j])
                        {
                            degreeStep = j;
                        }
                    }

                    if (dotProduct <= 0.0)
                    {
                        angleTable[degreeStep]++;
                        if (cosSquare > minAngle)
                        {
                            minAngle = cosSquare;
                        }
                        if (acuteBiggest && (cosSquare < maxAngle))
                        {
                            maxAngle = cosSquare;
                        }

                        // Update min/max angle per triangle
                        if (cosSquare > triMinAngle)
                        {
                            triMinAngle = cosSquare;
                        }
                        if (acuteBiggestTri && (cosSquare < triMaxAngle))
                        {
                            triMaxAngle = cosSquare;
                        }
                    }
                    else
                    {
                        angleTable[sampleDegrees - degreeStep - 1]++;
                        if (acuteBiggest || (cosSquare > maxAngle))
                        {
                            maxAngle = cosSquare;
                            acuteBiggest = false;
                        }

                        // Update max angle for (possibly non-acute) triangle
                        if (acuteBiggestTri || (cosSquare > triMaxAngle))
                        {
                            triMaxAngle = cosSquare;
                            acuteBiggestTri = false;
                        }
                    }
                }

                // Update min angle histogram
                degreeStep = sampleDegrees / 2 - 1;

                for (int j = degreeStep - 1; j >= 0; j--)
                {
                    if (triMinAngle > cosSquareTable[j])
                    {
                        degreeStep = j;
                    }
                }
                minAngles[degreeStep]++;

                // Update max angle histogram
                degreeStep = sampleDegrees / 2 - 1;

                for (int j = degreeStep - 1; j >= 0; j--)
                {
                    if (triMaxAngle > cosSquareTable[j])
                    {
                        degreeStep = j;
                    }
                }

                if (acuteBiggestTri)
                {
                    maxAngles[degreeStep]++;
                }
                else
                {
                    maxAngles[sampleDegrees - degreeStep - 1]++;
                }

                acuteBiggestTri = true;
            }

            minEdge = UnityEngine.Mathf.Sqrt(minEdge);
            maxEdge = UnityEngine.Mathf.Sqrt(maxEdge);
            minAspect = UnityEngine.Mathf.Sqrt(minAspect);
            maxAspect = UnityEngine.Mathf.Sqrt(maxAspect);
            minArea *= 0.5f;
            maxArea *= 0.5f;
            if (minAngle >= 1.0f)
            {
                minAngle = 0.0f;
            }
            else
            {
                minAngle = degconst * UnityEngine.Mathf.Acos(UnityEngine.Mathf.Sqrt(minAngle));
            }

            if (maxAngle >= 1.0f)
            {
                maxAngle = 180.0f;
            }
            else
            {
                if (acuteBiggest)
                {
                    maxAngle = degconst * UnityEngine.Mathf.Acos(UnityEngine.Mathf.Sqrt(maxAngle));
                }
                else
                {
                    maxAngle = 180.0f - degconst * UnityEngine.Mathf.Acos(UnityEngine.Mathf.Sqrt(maxAngle));
                }
            }
        }
    }
}
