// -----------------------------------------------------------------------
// <copyright file="BadTriQueue.cs">
// Original Triangle code by Jonathan Richard Shewchuk, http://www.cs.cmu.edu/~quake/triangle.html
// Triangle.NET code by Christian Woltering, http://triangle.codeplex.com/
// </copyright>
// -----------------------------------------------------------------------

namespace TriangleNet
{
    using System.Collections.Generic;
    using TriangleNet.Data;

    /// <summary>
    /// A (priority) queue for bad triangles.
    /// </summary>
    /// <remarks>
    //  The queue is actually a set of 4096 queues. I use multiple queues to
    //  give priority to smaller angles. I originally implemented a heap, but
    //  the queues are faster by a larger margin than I'd suspected.
    /// </remarks>
    class BadTriQueue
    {
        static readonly float SQRT2 = 1.4142135623730950488016887242096980785696718753769480732f;

        public int Count { get { return this.count; } }

        // Variables that maintain the bad triangle queues.  The queues are
        // ordered from 4095 (highest priority) to 0 (lowest priority).
        BadTriangle[] queuefront;
        BadTriangle[] queuetail;
        int[] nextnonemptyq;
        int firstnonemptyq;

        int count;

        public BadTriQueue()
        {
            //badtriangles = new List<BadTriangle>();

            queuefront = new BadTriangle[4096];
            queuetail = new BadTriangle[4096];
            nextnonemptyq = new int[4096];

            firstnonemptyq = -1;

            count = 0;
        }

        /// <summary>
        /// Add a bad triangle data structure to the end of a queue.
        /// </summary>
        /// <param name="badtri">The bad triangle to enqueue.</param>
        public void Enqueue(BadTriangle badtri)
        {
            float length, multiplier;
            int exponent, expincrement;
            int queuenumber;
            int posexponent;
            int i;

            this.count++;

            // Determine the appropriate queue to put the bad triangle into.
            // Recall that the key is the square of its shortest edge length.
            if (badtri.key >= 1.0)
            {
                length = badtri.key;
                posexponent = 1;
            }
            else
            {
                // 'badtri.key' is 2.0 to a negative exponent, so we'll record that
                // fact and use the reciprocal of 'badtri.key', which is > 1.0.
                length = 1.0f / badtri.key;
                posexponent = 0;
            }
            // 'length' is approximately 2.0 to what exponent?  The following code
            // determines the answer in time logarithmic in the exponent.
            exponent = 0;
            while (length > 2.0f)
            {
                // Find an approximation by repeated squaring of two.
                expincrement = 1;
                multiplier = 0.5f;
                while (length * multiplier * multiplier > 1.0)
                {
                    expincrement *= 2;
                    multiplier *= multiplier;
                }
                // Reduce the value of 'length', then iterate if necessary.
                exponent += expincrement;
                length *= multiplier;
            }
            // 'length' is approximately squareroot(2.0) to what exponent?
            exponent = 2 * exponent + (length > SQRT2 ? 1 : 0);
            // 'exponent' is now in the range 0...2047 for IEEE float precision.
            // Choose a queue in the range 0...4095.  The shortest edges have the
            // highest priority (queue 4095).
            if (posexponent > 0)
            {
                queuenumber = 2047 - exponent;
            }
            else
            {
                queuenumber = 2048 + exponent;
            }

            // Are we inserting into an empty queue?
            if (queuefront[queuenumber] == null)
            {
                // Yes, we are inserting into an empty queue.
                // Will this become the highest-priority queue?
                if (queuenumber > firstnonemptyq)
                {
                    // Yes, this is the highest-priority queue.
                    nextnonemptyq[queuenumber] = firstnonemptyq;
                    firstnonemptyq = queuenumber;
                }
                else
                {
                    // No, this is not the highest-priority queue.
                    // Find the queue with next higher priority.
                    i = queuenumber + 1;
                    while (queuefront[i] == null)
                    {
                        i++;
                    }
                    // Mark the newly nonempty queue as following a higher-priority queue.
                    nextnonemptyq[queuenumber] = nextnonemptyq[i];
                    nextnonemptyq[i] = queuenumber;
                }
                // Put the bad triangle at the beginning of the (empty) queue.
                queuefront[queuenumber] = badtri;
            }
            else
            {
                // Add the bad triangle to the end of an already nonempty queue.
                queuetail[queuenumber].nexttriang = badtri;
            }
            // Maintain a pointer to the last triangle of the queue.
            queuetail[queuenumber] = badtri;
            // Newly enqueued bad triangle has no successor in the queue.
            badtri.nexttriang = null;
        }

        /// <summary>
        /// Add a bad triangle to the end of a queue.
        /// </summary>
        /// <param name="enqtri"></param>
        /// <param name="minedge"></param>
        /// <param name="enqapex"></param>
        /// <param name="enqorg"></param>
        /// <param name="enqdest"></param>
        public void Enqueue(ref Otri enqtri, float minedge, Vertex enqapex, Vertex enqorg, Vertex enqdest)
        {
            // Allocate space for the bad triangle.
            BadTriangle newbad = new BadTriangle();

            newbad.poortri = enqtri;
            newbad.key = minedge;
            newbad.triangapex = enqapex;
            newbad.triangorg = enqorg;
            newbad.triangdest = enqdest;

            Enqueue(newbad);
        }

        /// <summary>
        /// Remove a triangle from the front of the queue.
        /// </summary>
        /// <returns></returns>
        public BadTriangle Dequeue()
        {
            // If no queues are nonempty, return NULL.
            if (firstnonemptyq < 0)
            {
                return null;
            }

            this.count--;

            // Find the first triangle of the highest-priority queue.
            BadTriangle result = queuefront[firstnonemptyq];
            // Remove the triangle from the queue.
            queuefront[firstnonemptyq] = result.nexttriang;
            // If this queue is now empty, note the new highest-priority
            // nonempty queue.
            if (result == queuetail[firstnonemptyq])
            {
                firstnonemptyq = nextnonemptyq[firstnonemptyq];
            }

            return result;
        }
    }
}
