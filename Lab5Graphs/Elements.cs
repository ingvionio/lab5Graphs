using System.Windows;
using System;
using System.Windows.Controls;
using System.Windows.Shapes;
using System.IO;
using System.Collections.Generic;

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
                int startIndex = edge.StartVertexId - 1; // Идентификаторы начинаются с 1
                int endIndex = edge.EndVertexId - 1;

                adjacencyMatrix[startIndex, endIndex] = 1;
                adjacencyMatrix[endIndex, startIndex] = 1; // Поскольку граф неориентированный
            }

            return adjacencyMatrix;
        }

        // Запись матрицы смежности в файл
        public void WriteAdjacencyMatrixToFile(string filePath)
        {
            int[,] adjacencyMatrix = ToAdjacencyMatrix();
            int n = Vertices.Count;

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
                    Shape = null, // Установим позже
                    Container = null // Установим позже
                });
            }

            // Чтение и добавление рёбер
            for (int i = 0; i < n; i++)
            {
                var values = lines[i].Split(' ');
                for (int j = i; j < n; j++)
                {
                    if (int.Parse(values[j]) == 1)
                    {
                        // Добавляем ребро (если граф неориентированный, не добавляем его дважды)
                        if (i != j)
                        {
                            Edges.Add(new Edge
                            {
                                StartVertexId = i + 1,
                                EndVertexId = j + 1
                            });
                        }
                    }
                }
            }
        }
    }
}
