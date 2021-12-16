// -----------------------------------------------------------------------
// <copyright file="ITriangle.cs" company="">
// Triangle.NET code by Christian Woltering, http://triangle.codeplex.com/
// </copyright>
// -----------------------------------------------------------------------

namespace TriangleNet.Geometry
{
    using TriangleNet.Data;

    /// <summary>
    /// Triangle interface.
    /// </summary>
    public interface ITriangle
    {
        /// <summary>
        /// The triangle id.
        /// </summary>
        int ID { get; }

        /// <summary>
        /// First vertex id of the triangle.
        /// </summary>
        int P0 { get; }
        /// <summary>
        /// Second vertex id of the triangle.
        /// </summary>
        int P1 { get; }
        /// <summary>
        /// Third vertex id of the triangle.
        /// </summary>
        int P2 { get; }

        /// <summary>
        /// Gets a triangles vertex.
        /// </summary>
        /// <param name="index">The vertex index (0, 1 or 2).</param>
        /// <returns>The vertex of the specified corner index.</returns>
        Vertex GetVertex(int index);

        /// <summary>
        /// True if the triangle implementation contains neighbor information.
        /// </summary>
        bool SupportsNeighbors { get; }

        /// <summary>
        /// First neighbor.
        /// </summary>
        int N0 { get; }
        /// <summary>
        /// Second neighbor.
        /// </summary>
        int N1 { get; }
        /// <summary>
        /// Third neighbor.
        /// </summary>
        int N2 { get; }

        /// <summary>
        /// Gets a triangles neighbor.
        /// </summary>
        /// <param name="index">The vertex index (0, 1 or 2).</param>
        /// <returns>The neigbbor opposite of vertex with given index.</returns>
        ITriangle GetNeighbor(int index);

        /// <summary>
        /// Gets a triangles segment.
        /// </summary>
        /// <param name="index">The vertex index (0, 1 or 2).</param>
        /// <returns>The segment opposite of vertex with given index.</returns>
        ISegment GetSegment(int index);

        /// <summary>
        /// Triangle area constraint.
        /// </summary>
        float Area { get; set; }

        /// <summary>
        /// Region ID the triangle belongs to.
        /// </summary>
        int Region { get; }
    }
}
