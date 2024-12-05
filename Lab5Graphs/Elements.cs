using System.Windows;
using System.Windows.Controls;
using System.Windows.Shapes;

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
    }
}
