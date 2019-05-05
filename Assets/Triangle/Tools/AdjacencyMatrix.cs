// -----------------------------------------------------------------------
// <copyright file="AdjacencyMatrix.cs" company="">
// Triangle.NET code by Christian Woltering, http://triangle.codeplex.com/
// </copyright>
// -----------------------------------------------------------------------

namespace TriangleNet.Tools
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;

    /// <summary>
    /// The adjacency matrix of the mesh.
    /// </summary>
    public class AdjacencyMatrix
    {
        // Number of nodes in the mesh.
        int node_num;

        // Number of adjacency entries.
        int adj_num;

        // Pointers into the actual adjacency structure adj. Information about row k is
        // stored in entries adj_row(k) through adj_row(k+1)-1 of adj. Size: node_num + 1
        int[] adj_row;

        // The adjacency structure. For each row, it contains the column indices 
        // of the nonzero entries. Size: adj_num
        int[] adj;

        public int[] AdjacencyRow
        {
            get { return adj_row; }
        }

        public int[] Adjacency
        {
            get { return adj; }
        }

        public AdjacencyMatrix(Mesh mesh)
        {
            this.node_num = mesh.vertices.Count;

            // Set up the adj_row adjacency pointer array.
            this.adj_row = AdjacencyCount(mesh);
            this.adj_num = adj_row[node_num] - 1;

            // Set up the adj adjacency array.
            this.adj = AdjacencySet(mesh, this.adj_row);
        }

        /// <summary>
        /// Computes the bandwidth of an adjacency matrix.
        /// </summary>
        /// <returns>Bandwidth of the adjacency matrix.</returns>
        public int Bandwidth()
        {
            int band_hi;
            int band_lo;
            int col;
            int i, j;

            band_lo = 0;
            band_hi = 0;

            for (i = 0; i < node_num; i++)
            {
                for (j = adj_row[i]; j <= adj_row[i + 1] - 1; j++)
                {
                    col = adj[j - 1];
                    band_lo = UnityEngine.Mathf.Max(band_lo, i - col);
                    band_hi = UnityEngine.Mathf.Max(band_hi, col - i);
                }
            }

            return band_lo + 1 + band_hi;
        }

        #region Adjacency matrix

        /// <summary>
        /// Counts adjacencies in a triangulation.
        /// </summary>
        /// <remarks>
        /// This routine is called to count the adjacencies, so that the
        /// appropriate amount of memory can be set aside for storage when
        /// the adjacency structure is created.
        ///
        /// The triangulation is assumed to involve 3-node triangles.
        ///
        /// Two nodes are "adjacent" if they are both nodes in some triangle.
        /// Also, a node is considered to be adjacent to itself.
        ///
        /// Diagram:
        ///
        ///       3
        ///    s  |\
        ///    i  | \
        ///    d  |  \
        ///    e  |   \  side 1
        ///       |    \
        ///    2  |     \
        ///       |      \
        ///       1-------2
        ///
        ///         side 3
        /// </remarks>
        int[] AdjacencyCount(Mesh mesh)
        {
            int i;
            int node;
            int n1, n2, n3;
            int tri_id;
            int neigh_id;

            int[] adj_rows = new int[node_num + 1];

            // Set every node to be adjacent to itself.
            for (node = 0; node < node_num; node++)
            {
                adj_rows[node] = 1;
            }

            // Examine each triangle.
            foreach (var tri in mesh.triangles.Values)
            {
                tri_id = tri.id;

                n1 = tri.vertices[0].id;
                n2 = tri.vertices[1].id;
                n3 = tri.vertices[2].id;

                // Add edge (1,2) if this is the first occurrence, that is, if 
                // the edge (1,2) is on a boundary (nid <= 0) or if this triangle
                // is the first of the pair in which the edge occurs (tid < nid).
                neigh_id = tri.neighbors[2].triangle.id;

                if (neigh_id < 0 || tri_id < neigh_id)
                {
                    adj_rows[n1] += 1;
                    adj_rows[n2] += 1;
                }

                // Add edge (2,3).
                neigh_id = tri.neighbors[0].triangle.id;

                if (neigh_id < 0 || tri_id < neigh_id)
                {
                    adj_rows[n2] += 1;
                    adj_rows[n3] += 1;
                }

                // Add edge (3,1).
                neigh_id = tri.neighbors[1].triangle.id;

                if (neigh_id < 0 || tri_id < neigh_id)
                {
                    adj_rows[n3] += 1;
                    adj_rows[n1] += 1;
                }
            }

            // We used ADJ_COL to count the number of entries in each column.
            // Convert it to pointers into the ADJ array.
            for (node = node_num; 1 <= node; node--)
            {
                adj_rows[node] = adj_rows[node - 1];
            }

            adj_rows[0] = 1;
            for (i = 1; i <= node_num; i++)
            {
                adj_rows[i] = adj_rows[i - 1] + adj_rows[i];
            }

            return adj_rows;
        }

        /// <summary>
        /// Sets adjacencies in a triangulation.
        /// </summary>
        /// <remarks>
        /// This routine can be used to create the compressed column storage
        /// for a linear triangle finite element discretization of Poisson's
        /// equation in two dimensions.
        /// </remarks>
        int[] AdjacencySet(Mesh mesh, int[] rows)
        {
            // Output list, stores the actual adjacency information.
            int[] list;

            // Copy of the adjacency rows input.
            int[] rowsCopy = new int[node_num];
            Array.Copy(rows, rowsCopy, node_num);

            int i, n = rows[node_num] - 1;

            list = new int[n];
            for (i = 0; i < n; i++)
            {
                list[i] = -1;
            }

            // Set every node to be adjacent to itself.
            for (i = 0; i < node_num; i++)
            {
                list[rowsCopy[i] - 1] = i;
                rowsCopy[i] += 1;
            }

            int n1, n2, n3; // Vertex numbers.
            int tid, nid; // Triangle and neighbor id.

            // Examine each triangle.
            foreach (var tri in mesh.triangles.Values)
            {
                tid = tri.id;

                n1 = tri.vertices[0].id;
                n2 = tri.vertices[1].id;
                n3 = tri.vertices[2].id;

                // Add edge (1,2) if this is the first occurrence, that is, if 
                // the edge (1,2) is on a boundary (nid <= 0) or if this triangle
                // is the first of the pair in which the edge occurs (tid < nid).
                nid = tri.neighbors[2].triangle.id;

                if (nid < 0 || tid < nid)
                {
                    list[rowsCopy[n1] - 1] = n2;
                    rowsCopy[n1] += 1;
                    list[rowsCopy[n2] - 1] = n1;
                    rowsCopy[n2] += 1;
                }

                // Add edge (2,3).
                nid = tri.neighbors[0].triangle.id;

                if (nid < 0 || tid < nid)
                {
                    list[rowsCopy[n2] - 1] = n3;
                    rowsCopy[n2] += 1;
                    list[rowsCopy[n3] - 1] = n2;
                    rowsCopy[n3] += 1;
                }

                // Add edge (3,1).
                nid = tri.neighbors[1].triangle.id;

                if (nid < 0 || tid < nid)
                {
                    list[rowsCopy[n1] - 1] = n3;
                    rowsCopy[n1] += 1;
                    list[rowsCopy[n3] - 1] = n1;
                    rowsCopy[n3] += 1;
                }
            }

            int k1, k2;

            // Ascending sort the entries for each node.
            for (i = 0; i < node_num; i++)
            {
                k1 = rows[i];
                k2 = rows[i + 1] - 1;
                HeapSort(list, k1 - 1, k2 + 1 - k1);
            }

            return list;
        }

        #endregion

        #region Heap sort

        /// <summary>
        /// Reorders an array of integers into a descending heap.
        /// </summary>
        /// <param name="size">the size of the input array.</param>
        /// <param name="a">an unsorted array.</param>
        /// <remarks>
        /// A heap is an array A with the property that, for every index J,
        /// A[J] >= A[2*J+1] and A[J] >= A[2*J+2], (as long as the indices
        /// 2*J+1 and 2*J+2 are legal).
        ///
        /// Diagram:
        ///
        ///                  A(0)
        ///                /      \
        ///            A(1)         A(2)
        ///          /     \        /  \
        ///      A(3)       A(4)  A(5) A(6)
        ///      /  \       /   \
        ///    A(7) A(8)  A(9) A(10)
        /// </remarks>
        private void CreateHeap(int[] a, int offset, int size)
        {
            int i;
            int ifree;
            int key;
            int m;

            // Only nodes (N/2)-1 down to 0 can be "parent" nodes.
            for (i = (size / 2) - 1; 0 <= i; i--)
            {
                // Copy the value out of the parent node.
                // Position IFREE is now "open".
                key = a[offset + i];
                ifree = i;

                for (; ; )
                {
                    // Positions 2*IFREE + 1 and 2*IFREE + 2 are the descendants of position
                    // IFREE.  (One or both may not exist because they equal or exceed N.)
                    m = 2 * ifree + 1;

                    // Does the first position exist?
                    if (size <= m)
                    {
                        break;
                    }
                    else
                    {
                        // Does the second position exist?
                        if (m + 1 < size)
                        {
                            // If both positions exist, take the larger of the two values,
                            // and update M if necessary.
                            if (a[offset + m] < a[offset + m + 1])
                            {
                                m = m + 1;
                            }
                        }

                        // If the large descendant is larger than KEY, move it up,
                        // and update IFREE, the location of the free position, and
                        // consider the descendants of THIS position.
                        if (key < a[offset + m])
                        {
                            a[offset + ifree] = a[offset + m];
                            ifree = m;
                        }
                        else
                        {
                            break;
                        }
                    }
                }

                // When you have stopped shifting items up, return the item you
                // pulled out back to the heap.
                a[offset + ifree] = key;
            }

            return;
        }


        /// <summary>
        /// ascending sorts an array of integers using heap sort.
        /// </summary>
        /// <param name="size">Number of entries in the array.</param>
        /// <param name="a">Array to be sorted;</param>
        private void HeapSort(int[] a, int offset, int size)
        {
            int n1;
            int temp;

            if (size <= 1)
            {
                return;
            }

            // 1: Put A into descending heap form.
            CreateHeap(a, offset, size);

            // 2: Sort A.
            // The largest object in the heap is in A[0].
            // Move it to position A[N-1].
            temp = a[offset];
            a[offset] = a[offset + size - 1];
            a[offset + size - 1] = temp;

            // Consider the diminished heap of size N1.
            for (n1 = size - 1; 2 <= n1; n1--)
            {
                // Restore the heap structure of the initial N1 entries of A.
                CreateHeap(a, offset, n1);

                // Take the largest object from A[0] and move it to A[N1-1].
                temp = a[offset];
                a[offset] = a[offset + n1 - 1];
                a[offset + n1 - 1] = temp;
            }

            return;
        }

        #endregion
    }
}
