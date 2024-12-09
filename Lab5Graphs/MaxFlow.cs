using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lab5Graphs
{
    public class MaxFlow
    {
        public int[,] capacity;
        public int[,] residual;
        public int[] parent;

        public MaxFlow(int[,] capacity)
        {
            this.capacity = capacity;
            int n = capacity.GetLength(0);
            residual = new int[n, n];
            parent = new int[n];
        }

        public bool BFS(int source, int sink)
        {
            bool[] visited = new bool[capacity.GetLength(0)];
            Queue<int> queue = new Queue<int>();
            queue.Enqueue(source);
            visited[source] = true;
            parent[source] = -1;

            while (queue.Count > 0)
            {
                int u = queue.Dequeue();

                for (int v = 0; v < capacity.GetLength(0); v++)
                {
                    if (!visited[v] && residual[u, v] > 0)
                    {
                        if (v == sink)
                        {
                            parent[v] = u;
                            return true;
                        }
                        queue.Enqueue(v);
                        parent[v] = u;
                        visited[v] = true;
                    }
                }
            }

            return false;
        }
    }
}
