// -----------------------------------------------------------------------
// <copyright file="TriangleFormat.cs" company="">
// Triangle.NET code by Christian Woltering, http://triangle.codeplex.com/
// </copyright>
// -----------------------------------------------------------------------

namespace TriangleNet.IO
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using TriangleNet.Geometry;
    using System.IO;

    /// <summary>
    /// Implements geometry and mesh file formats of the the original Triangle code.
    /// </summary>
    public class TriangleFormat : IGeometryFormat, IMeshFormat
    {
        public Mesh Import(string filename)
        {
            string ext = Path.GetExtension(filename);

            if (ext == ".node" || ext == ".poly" || ext == ".ele")
            {
                List<ITriangle> triangles;
                InputGeometry geometry;

                FileReader.Read(filename, out geometry, out triangles);

                if (geometry != null && triangles != null)
                {
                    Mesh mesh = new Mesh();
                    mesh.Load(geometry, triangles);

                    return mesh;
                }
            }

            throw new NotSupportedException("Could not load '" + filename + "' file.");
        }

        public void Write(Mesh mesh, string filename)
        {
            FileWriter.WritePoly(mesh, Path.ChangeExtension(filename, ".poly"));
            FileWriter.WriteElements(mesh, Path.ChangeExtension(filename, ".ele"));
        }

        public InputGeometry Read(string filename)
        {
            string ext = Path.GetExtension(filename);

            if (ext == ".node")
            {
                return FileReader.ReadNodeFile(filename);
            }
            
            if (ext == ".poly")
            {
                return FileReader.ReadPolyFile(filename);
            }

            throw new NotSupportedException("File format '" + ext + "' not supported.");
        }
    }
}
