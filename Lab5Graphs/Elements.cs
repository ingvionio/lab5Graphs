using System.Windows;
using System.Windows.Controls;
using System.Windows.Shapes;
using System.IO;
using System.Collections.Generic;
using System.Linq;

namespace Lab5Graphs
{
    public class Vertex
    {
        public int Id { get; set; }
        public Ellipse Shape { get; set; }
        public TextBlock Text { get; set; }
        public Canvas Container { get; set; }
    }

    public class Edge
    {
        public int StartVertexId { get; set; }
        public int EndVertexId { get; set; }
        public Line Shape { get; set; }
        public TextBlock WeightText { get; set; }
        public int Weight { get; set; } = 1;
    }

    public class Graph
    {
        public List<Vertex> Vertices { get; set; } = new List<Vertex>();
        public List<Edge> Edges { get; set; } = new List<Edge>();

        // Преобразование графа в матрицу смежности
        public int[,] ToAdjacencyMatrix()
        {
            int n = Vertices.Count;
            int[,] adjacencyMatrix = new int[n, n];

            foreach (var edge in Edges)
            {
                int startIndex = edge.StartVertexId - 1;
                int endIndex = edge.EndVertexId - 1;

                adjacencyMatrix[startIndex, endIndex] = edge.Weight; // Используем вес ребра
                adjacencyMatrix[endIndex, startIndex] = edge.Weight; // Для неориентированного графа
            }

            return adjacencyMatrix;
        }

        // Запись матрицы смежности в файл
        public void WriteAdjacencyMatrixToFile(string filePath)
        {
            int n = Vertices.Count;
            int[,] adjacencyMatrix = new int[n, n];

            foreach (var edge in Edges)
            {
                int startIndex = edge.StartVertexId - 1;
                int endIndex = edge.EndVertexId - 1;
                int weight = edge.Weight;

                adjacencyMatrix[startIndex, endIndex] = weight;
                adjacencyMatrix[endIndex, startIndex] = weight; // Так как граф неориентированный
            }

            using (StreamWriter writer = new StreamWriter(filePath))
            {
                for (int i = 0; i < n; i++)
                {
                    for (int j = 0; j < n; j++)
                    {
                        writer.Write(adjacencyMatrix[i, j] + (j == n - 1 ? "" : " "));
                    }
                    writer.WriteLine();
                }
            }
        }


        // Чтение матрицы смежности из файла и восстановление графа
        public void ReadAdjacencyMatrixFromFile(string filePath)
        {
            var lines = File.ReadAllLines(filePath);
            int n = lines.Length;

            Vertices.Clear();
            Edges.Clear();

            // Создаем вершины
            for (int i = 0; i < n; i++)
            {
                Vertices.Add(new Vertex
                {
                    Id = i + 1,
                    Shape = null,
                    Container = null
                });
            }

            // Чтение и добавление рёбер
            for (int i = 0; i < n; i++)
            {
                var values = lines[i].Split(' ');
                for (int j = i; j < n; j++)
                {
                    int weight = int.Parse(values[j]);
                    if (weight > 0)
                    {
                        // Добавляем ребро с учетом веса
                        if (i != j)
                        {
                            Edges.Add(new Edge
                            {
                                StartVertexId = i + 1,
                                EndVertexId = j + 1,
                                Weight = weight
                            });
                        }
                    }
                }
            }
        }


        public void RemoveVertex(Vertex vertex)
        {
            Vertices.Remove(vertex);

            Edges.RemoveAll(edge => edge.StartVertexId == vertex.Id || edge.EndVertexId == vertex.Id);


            for (int i = 0; i < Vertices.Count; i++)
            {
                Vertices[i].Id = i + 1;
                if (Vertices[i].Text != null) Vertices[i].Text.Text = "V" + (i + 1); // Обновляем текст вершины
            }

            // Обновляем ID в ребрах
            foreach (var edge in Edges)
            {

                int startVertexIndex = Vertices.FindIndex(v => v.Id == edge.StartVertexId);
                edge.StartVertexId = startVertexIndex + 1;


                int endVertexIndex = Vertices.FindIndex(v => v.Id == edge.EndVertexId);
                edge.EndVertexId = endVertexIndex + 1;
            }

        }

        public List<Edge> FindMinimumSpanningTree()
        {
            // Prim's Algorithm implementation
            List<Edge> mst = new List<Edge>();
            HashSet<int> visitedVertices = new HashSet<int>();
            List<Edge> eligibleEdges = new List<Edge>();

            if (Vertices.Count == 0) return mst;


            visitedVertices.Add(Vertices[0].Id);


            eligibleEdges.AddRange(Edges.Where(e => e.StartVertexId == Vertices[0].Id || e.EndVertexId == Vertices[0].Id));

            while (visitedVertices.Count < Vertices.Count && eligibleEdges.Count > 0)
            {

                eligibleEdges.Sort((e1, e2) => e1.Weight.CompareTo(e2.Weight));
                Edge minEdge = eligibleEdges[0];
                eligibleEdges.RemoveAt(0);

                int nextVertexId = visitedVertices.Contains(minEdge.StartVertexId) ? minEdge.EndVertexId : minEdge.StartVertexId;

                if (!visitedVertices.Contains(nextVertexId))
                {
                    mst.Add(minEdge);
                    visitedVertices.Add(nextVertexId);
                    eligibleEdges.AddRange(Edges.Where(e => (e.StartVertexId == nextVertexId || e.EndVertexId == nextVertexId) && !visitedVertices.Contains(e.StartVertexId == nextVertexId ? e.EndVertexId : e.StartVertexId)));
                }
            }


            return mst;
        }

        public void SaveMSTToFile(string filePath, List<Edge> mstEdges)
        {
            int n = Vertices.Count;
            int[,] adjacencyMatrix = new int[n, n];


            foreach (var edge in mstEdges) // Use mstEdges instead of all edges
            {
                int startIndex = edge.StartVertexId - 1;
                int endIndex = edge.EndVertexId - 1;
                int weight = edge.Weight;


                adjacencyMatrix[startIndex, endIndex] = weight;
                adjacencyMatrix[endIndex, startIndex] = weight;
            }

            using (StreamWriter writer = new StreamWriter(filePath))
            {
                for (int i = 0; i < n; i++)
                {
                    for (int j = 0; j < n; j++)
                    {
                        writer.Write(adjacencyMatrix[i, j] + (j == n - 1 ? "" : " "));
                    }
                    writer.WriteLine();
                }
            }
        }

        private Canvas FindVertexContainerById(int vertexId)
        {
            return Vertices.FirstOrDefault(v => v.Id == vertexId)?.Container;
        }

        public int[,] ToCapacityMatrix()
        {
            int n = Vertices.Count;
            int[,] capacityMatrix = new int[n, n];

            foreach (var edge in Edges)
            {
                int startIndex = edge.StartVertexId - 1;
                int endIndex = edge.EndVertexId - 1;
                capacityMatrix[startIndex, endIndex] = edge.Weight;
            }

            return capacityMatrix;
        }
    }
}