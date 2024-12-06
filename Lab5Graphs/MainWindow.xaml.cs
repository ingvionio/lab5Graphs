using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using System;
using System.IO;
using System.Collections.Generic;

namespace Lab5Graphs
{
    public partial class MainWindow : Window
    {
        private readonly Graph _graph = new Graph();
        private bool _isAddingEdge = false;
        private Vertex _selectedVertex = null;

        private Point _dragStart;
        private bool _isDragging = false;
        private Vertex _draggedVertex = null;

        public MainWindow()
        {
            InitializeComponent();
        }

        // Добавление вершины на холст
        private void Canvas_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (_isAddingEdge) return;

            // Получаем позицию щелчка
            Point position = e.GetPosition(GraphCanvas);

            // Создаем вершину
            Ellipse vertexShape = new Ellipse
            {
                Width = 50,
                Height = 50,
                Fill = Brushes.Transparent,
                Stroke = Brushes.Black,
                StrokeThickness = 2
            };

            // Создаем контейнер для вершины
            Canvas vertexContainer = new Canvas
            {
                Width = 50,
                Height = 50
            };

            // Добавляем вершину в контейнер
            vertexContainer.Children.Add(vertexShape);
            Canvas.SetLeft(vertexContainer, position.X - 25);
            Canvas.SetTop(vertexContainer, position.Y - 25);

            // Создаем TextBox для ввода названия вершины
            TextBox vertexTextBox = new TextBox
            {
                Width = 50,
                Height = 25,
                TextAlignment = TextAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                Background = Brushes.White,
                BorderBrush = Brushes.Gray,
                FontSize = 14,
                Text = "V" + (_graph.Vertices.Count + 1) // Предустановленное значение для новой вершины
            };

            // Создаем объект вершины и добавляем его в граф
            var vertex = new Vertex
            {
                Id = _graph.Vertices.Count + 1,
                Shape = vertexShape,
                Container = vertexContainer
            };
            _graph.Vertices.Add(vertex);

            // Обработчик события нажатия Enter для завершения редактирования
            vertexTextBox.KeyDown += (s, ke) =>
            {
                if (ke.Key == Key.Enter)
                {
                    string vertexLabel = vertexTextBox.Text;

                    // Создаем статический TextBlock, чтобы заменить TextBox
                    TextBlock vertexText = new TextBlock
                    {
                        Text = vertexLabel,
                        FontSize = 16,
                        Foreground = Brushes.Black,
                        Width = 50,
                        TextAlignment = TextAlignment.Center,
                        VerticalAlignment = VerticalAlignment.Center
                    };

                    // Заменяем TextBox на TextBlock
                    vertexContainer.Children.Remove(vertexTextBox);
                    vertexContainer.Children.Add(vertexText);
                    Canvas.SetLeft(vertexText, 0);
                    Canvas.SetTop(vertexText, 12.5); // Центрирование текста в круге

                    // Обновляем информацию о вершине в объекте
                    vertex.Text = vertexText;
                }
            };

            // Добавляем TextBox в контейнер
            vertexContainer.Children.Add(vertexTextBox);
            Canvas.SetLeft(vertexTextBox, 0);
            Canvas.SetTop(vertexTextBox, 12.5); // Центрирование поля ввода в круге

            // Добавляем контейнер вершины на холст
            GraphCanvas.Children.Add(vertexContainer);

            // Обработчики для взаимодействия
            vertexContainer.MouseLeftButtonDown += Vertex_MouseLeftButtonDown;
            vertexContainer.MouseRightButtonDown += Vertex_MouseRightButtonDown;
            vertexContainer.MouseRightButtonUp += Vertex_MouseRightButtonUp;
            vertexContainer.MouseMove += Vertex_MouseMove;

            // Фокус на поле ввода для начала редактирования
            vertexTextBox.Focus();
        }

        // Выбор вершины для добавления рёбер
        private void Vertex_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (!_isAddingEdge) return;

            var clickedVertex = _graph.Vertices.Find(v => v.Container == sender as Canvas);

            if (_selectedVertex == null)
            {
                _selectedVertex = clickedVertex;
                clickedVertex.Shape.Stroke = Brushes.Red; // Подсвечиваем выбранную вершину
            }
            else
            {
                // Создаем линию (ребро)
                Line edgeShape = new Line
                {
                    Stroke = Brushes.Black,
                    StrokeThickness = 2,
                    X1 = Canvas.GetLeft(_selectedVertex.Container) + 25,
                    Y1 = Canvas.GetTop(_selectedVertex.Container) + 25,
                    X2 = Canvas.GetLeft(clickedVertex.Container) + 25,
                    Y2 = Canvas.GetTop(clickedVertex.Container) + 25
                };

                // Добавляем ребро на холст
                GraphCanvas.Children.Add(edgeShape);

                // Создаем объект ребра и добавляем в граф
                var edge = new Edge
                {
                    StartVertexId = _selectedVertex.Id,
                    EndVertexId = clickedVertex.Id,
                    Shape = edgeShape
                };
                _graph.Edges.Add(edge);

                // Снимаем выделение
                _selectedVertex.Shape.Stroke = Brushes.Black;
                _selectedVertex = null;
            }
        }

        // Начало перетаскивания вершины правой кнопкой
        private void Vertex_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            _draggedVertex = _graph.Vertices.Find(v => v.Container == sender as Canvas);
            if (_draggedVertex != null)
            {
                _isDragging = true;
                _dragStart = e.GetPosition(GraphCanvas);
            }
        }

        // Завершение перетаскивания вершины правой кнопкой
        private void Vertex_MouseRightButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (_isDragging)
            {
                _isDragging = false;
                _draggedVertex = null;
            }
        }

        // Перетаскивание вершины правой кнопкой
        private void Vertex_MouseMove(object sender, MouseEventArgs e)
        {
            if (_isDragging && _draggedVertex != null)
            {
                Point currentPosition = e.GetPosition(GraphCanvas);
                double offsetX = currentPosition.X - _dragStart.X;
                double offsetY = currentPosition.Y - _dragStart.Y;

                // Обновляем положение вершины
                double newLeft = Canvas.GetLeft(_draggedVertex.Container) + offsetX;
                double newTop = Canvas.GetTop(_draggedVertex.Container) + offsetY;
                Canvas.SetLeft(_draggedVertex.Container, newLeft);
                Canvas.SetTop(_draggedVertex.Container, newTop);

                _dragStart = currentPosition;

                // Обновляем рёбра, связанные с этой вершиной
                foreach (var edge in _graph.Edges)
                {
                    if (edge.StartVertexId == _draggedVertex.Id)
                    {
                        edge.Shape.X1 = newLeft + 25;
                        edge.Shape.Y1 = newTop + 25;
                    }
                    else if (edge.EndVertexId == _draggedVertex.Id)
                    {
                        edge.Shape.X2 = newLeft + 25;
                        edge.Shape.Y2 = newTop + 25;
                    }
                }
            }
        }

        // Переключение на режим добавления рёбер
        private void AddEdgeButton_Click(object sender, RoutedEventArgs e)
        {
            _isAddingEdge = !_isAddingEdge;
            AddEdgeButton.Content = _isAddingEdge ? "Finish Adding Edge" : "Add Edge";
        }

        // Очистка холста
        private void ClearButton_Click(object sender, RoutedEventArgs e)
        {
            GraphCanvas.Children.Clear();
            _graph.Vertices.Clear();
            _graph.Edges.Clear();
            _isAddingEdge = false;
            _selectedVertex = null;
        }

        private void LoadButton_Click(object sender, RoutedEventArgs e)
        {
            string filePath = Path.Text;
            if (File.Exists(filePath))
            {
                _graph.ReadAdjacencyMatrixFromFile(filePath);
                DrawGraph();
                MessageBox.Show("Graph loaded from " + filePath);
            }
            else
            {
                MessageBox.Show("File not found: " + filePath);
            }
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            string filePath = Path.Text;
            _graph.WriteAdjacencyMatrixToFile(filePath);
            MessageBox.Show("Graph saved to " + filePath);
        }

        private void DrawGraph()
        {
            GraphCanvas.Children.Clear();

            // Создаем фиксированную сетку координат для вершин, чтобы избежать наложений
            double canvasWidth = GraphCanvas.ActualWidth;
            double canvasHeight = GraphCanvas.ActualHeight;
            int vertexCount = _graph.Vertices.Count;
            double radius = Math.Min(canvasWidth, canvasHeight) / 3; // Радиус, на котором будут располагаться вершины
            Point center = new Point(canvasWidth / 2, canvasHeight / 2); // Центр холста

            // Создаем вершины и размещаем их равномерно по окружности
            for (int i = 0; i < vertexCount; i++)
            {
                double angle = 2 * Math.PI * i / vertexCount; // Угол для текущей вершины
                double x = center.X + radius * Math.Cos(angle) - 25; // Координата X вершины
                double y = center.Y + radius * Math.Sin(angle) - 25; // Координата Y вершины

                // Создаем контейнер для вершины
                Canvas vertexContainer = new Canvas
                {
                    Width = 50,
                    Height = 50
                };

                // Создаем вершину (круг)
                Ellipse vertexShape = new Ellipse
                {
                    Width = 50,
                    Height = 50,
                    Fill = Brushes.Transparent,
                    Stroke = Brushes.Black,
                    StrokeThickness = 2
                };

                // Создаем текстовое поле для названия вершины
                TextBlock vertexText = new TextBlock
                {
                    Text = "V" + (i + 1),
                    FontSize = 16,
                    Foreground = Brushes.Black,
                    Width = 50,
                    TextAlignment = TextAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
                };

                // Добавляем элементы в контейнер вершины
                vertexContainer.Children.Add(vertexShape);
                vertexContainer.Children.Add(vertexText);
                Canvas.SetLeft(vertexText, 0);
                Canvas.SetTop(vertexText, 12.5); // Центрирование текста в круге

                // Устанавливаем позицию контейнера на Canvas
                Canvas.SetLeft(vertexContainer, x);
                Canvas.SetTop(vertexContainer, y);

                // Добавляем контейнер вершины на холст
                GraphCanvas.Children.Add(vertexContainer);

                // Обновляем объект вершины в графе
                var vertex = _graph.Vertices[i];
                vertex.Container = vertexContainer;
                vertex.Shape = vertexShape;
                vertex.Text = vertexText;

                // Обработчики для взаимодействия
                vertexContainer.MouseLeftButtonDown += Vertex_MouseLeftButtonDown;
                vertexContainer.MouseRightButtonDown += Vertex_MouseRightButtonDown;
                vertexContainer.MouseRightButtonUp += Vertex_MouseRightButtonUp;
                vertexContainer.MouseMove += Vertex_MouseMove;
            }

            // Создаем рёбра
            foreach (var edge in _graph.Edges)
            {
                var startVertex = _graph.Vertices[edge.StartVertexId - 1];
                var endVertex = _graph.Vertices[edge.EndVertexId - 1];

                double x1 = Canvas.GetLeft(startVertex.Container) + 25;
                double y1 = Canvas.GetTop(startVertex.Container) + 25;
                double x2 = Canvas.GetLeft(endVertex.Container) + 25;
                double y2 = Canvas.GetTop(endVertex.Container) + 25;

                // Создаем линию (ребро)
                Line edgeShape = new Line
                {
                    Stroke = Brushes.Black,
                    StrokeThickness = 2,
                    X1 = x1,
                    Y1 = y1,
                    X2 = x2,
                    Y2 = y2
                };

                // Добавляем линию на холст
                GraphCanvas.Children.Add(edgeShape);
                edge.Shape = edgeShape;
            }
        }

    }
}
