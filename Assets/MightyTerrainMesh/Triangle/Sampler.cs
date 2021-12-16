// -----------------------------------------------------------------------
// <copyright file="Sampler.cs">
// Triangle.NET code by Christian Woltering, http://triangle.codeplex.com/
// </copyright>
// -----------------------------------------------------------------------

namespace TriangleNet
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;

    /// <summary>
    /// Used for triangle sampling in the Mesh.Locate method.
    /// </summary>
    class Sampler
    {
        static Random rand = new Random(DateTime.Now.Millisecond);

        // Number of random samples for point location (at least 1).
        int samples = 1;

        // Number of triangles in mesh.
        int triangleCount = 0;

        // Empirically chosen factor.
        static int samplefactor = 11;

        // Keys of the triangle dictionary.
        int[] keys;

        /// <summary>
        /// Reset the sampler.
        /// </summary>
        public void Reset()
        {
            this.samples = 1;
            this.triangleCount = 0;
        }

        /// <summary>
        /// Update sampling parameters if mesh changed.
        /// </summary>
        /// <param name="mesh">Current mesh.</param>
        public void Update(Mesh mesh)
        {
            this.Update(mesh, false);
        }

        /// <summary>
        /// Update sampling parameters if mesh changed.
        /// </summary>
        /// <param name="mesh">Current mesh.</param>
        public void Update(Mesh mesh, bool forceUpdate)
        {
            int count = mesh.triangles.Count;

            // TODO: Is checking the triangle count a good way to monitor mesh changes?
            if (triangleCount != count || forceUpdate)
            {
                triangleCount = count;

                // The number of random samples taken is proportional to the cube root of
                // the number of triangles in the mesh.  The next bit of code assumes
                // that the number of triangles increases monotonically (or at least
                // doesn't decrease enough to matter).
                while (samplefactor * samples * samples * samples < count)
                {
                    samples++;
                }

                // TODO: Is there a way not calling ToArray()?
                keys = mesh.triangles.Keys.ToArray();
            }
        }

        /// <summary>
        /// Get a random sample set of triangle keys.
        /// </summary>
        /// <returns>Array of triangle keys.</returns>
        public int[] GetSamples(Mesh mesh)
        {
            // TODO: Using currKeys to check key availability?
            List<int> randSamples = new List<int>(samples);

            int range = triangleCount / samples;
            int key;

            for (int i = 0; i < samples; i++)
            {
                // Yeah, rand should be equally distributed, but just to make
                // sure, use a range variable...
                key = rand.Next(i * range, (i + 1) * range - 1);

                if (!mesh.triangles.Keys.Contains(keys[key]))
                {
                    // Keys collection isn't up to date anymore!
                    this.Update(mesh, true);
                    i--;
                }
                else
                {
                    randSamples.Add(keys[key]);
                }
            }

            return randSamples.ToArray();
        }
    }
}
