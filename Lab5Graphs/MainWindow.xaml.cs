using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using System.IO;


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
                clickedVertex.Shape.Stroke = Brushes.Red; // Выделяем вершину
            }
            else
            {
                if (clickedVertex != _selectedVertex)
                {
                    // Вычисляем точки на краях окружностей
                    Point startCenter = new Point(Canvas.GetLeft(_selectedVertex.Container) + 25, Canvas.GetTop(_selectedVertex.Container) + 25);
                    Point endCenter = new Point(Canvas.GetLeft(clickedVertex.Container) + 25, Canvas.GetTop(clickedVertex.Container) + 25);
                    Point startEdge = GetEdgePoint(startCenter, endCenter, 25);
                    Point endEdge = GetEdgePoint(endCenter, startCenter, 25);

                    // Создаем линию (ребро)
                    Line edgeShape = new Line
                    {
                        Stroke = Brushes.Black,
                        StrokeThickness = 2,
                        X1 = startEdge.X,
                        Y1 = startEdge.Y,
                        X2 = endEdge.X,
                        Y2 = endEdge.Y
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

                    // Перерисовываем граф для отображения веса ребра
                    DrawGraph();
                }

                // Снимаем выделение с вершины в любом случае
                _selectedVertex.Shape.Stroke = Brushes.Black;
                _selectedVertex = null;
            }
        }




        private void Vertex_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl))
            {
                _draggedVertex = _graph.Vertices.Find(v => v.Container == sender as Canvas);

                if (_draggedVertex != null)
                {
                    _graph.RemoveVertex(_draggedVertex);
                    GraphCanvas.Children.Remove(_draggedVertex.Container);
                    foreach (Edge edge in _graph.Edges.ToList())
                    {

                        if (edge.Shape != null && (edge.StartVertexId == _draggedVertex.Id || edge.EndVertexId == _draggedVertex.Id))
                        {
                            GraphCanvas.Children.Remove(edge.Shape);

                        }
                        if (edge.WeightText != null && (edge.StartVertexId == _draggedVertex.Id || edge.EndVertexId == _draggedVertex.Id))
                        {
                            GraphCanvas.Children.Remove(edge.WeightText);

                        }
                    }


                    DrawGraph();
                    _draggedVertex = null;
                }


            }
            else
            {
                _draggedVertex = _graph.Vertices.Find(v => v.Container == sender as Canvas);
                if (_draggedVertex != null)
                {
                    _isDragging = true;
                    _dragStart = e.GetPosition(GraphCanvas);
                }
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

                    if (edge.WeightText != null)
                    {
                        Canvas.SetLeft(edge.WeightText, (edge.Shape.X1 + edge.Shape.X2) / 2 - 10);
                        Canvas.SetTop(edge.WeightText, (edge.Shape.Y1 + edge.Shape.Y2) / 2 - 10);
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

        private void GraphCanvas_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            // Проверяем, был ли клик на ребре
            var clickedElement = e.OriginalSource as FrameworkElement;
            var edge = _graph.Edges.FirstOrDefault(ed => ed.Shape == clickedElement || (ed.WeightText != null && ed.WeightText == clickedElement));
            if (edge != null)
            {
                // Создаем контекстное меню
                ContextMenu contextMenu = new ContextMenu();
                MenuItem changeWeightItem = new MenuItem { Header = "Change Weight" };
                contextMenu.Items.Add(changeWeightItem);

                // Обработчик события выбора пункта меню
                changeWeightItem.Click += (s, ea) =>
                {
                    string input = Microsoft.VisualBasic.Interaction.InputBox("Enter edge weight:", "Edge Weight", edge.Weight.ToString());
                    if (int.TryParse(input, out int newWeight))
                    {
                        edge.Weight = newWeight;
                        edge.WeightText.Text = newWeight.ToString();
                    }
                };

                // Открываем контекстное меню
                contextMenu.IsOpen = true;
                e.Handled = true; // Prevents other MouseRightButtonDown events
            }
        }

        private void DrawGraph()
        {
            GraphCanvas.Children.Clear();

            double canvasWidth = GraphCanvas.ActualWidth;
            double canvasHeight = GraphCanvas.ActualHeight;
            double radius = Math.Min(canvasWidth, canvasHeight) / 3; // Радиус для размещения вершин
            Point center = new Point(canvasWidth / 2, canvasHeight / 2); // Центр холста

            for (int i = 0; i < _graph.Vertices.Count; i++)
            {
                var vertex = _graph.Vertices[i];

                if (vertex.Container == null)
                {
                    // Вычисляем координаты для вершины
                    double angle = 2 * Math.PI * i / _graph.Vertices.Count;
                    double x = center.X + radius * Math.Cos(angle) - 25;
                    double y = center.Y + radius * Math.Sin(angle) - 25;

                    // Создаем визуальные элементы только для новых вершин
                    Canvas vertexContainer = new Canvas
                    {
                        Width = 50,
                        Height = 50
                    };

                    Ellipse vertexShape = new Ellipse
                    {
                        Width = 50,
                        Height = 50,
                        Fill = Brushes.Transparent,
                        Stroke = Brushes.Black,
                        StrokeThickness = 2
                    };
                    vertexContainer.Children.Add(vertexShape);

                    if (vertex.Text == null)
                    {
                        TextBlock vertexText = new TextBlock
                        {
                            Text = "V" + vertex.Id,
                            FontSize = 16,
                            Foreground = Brushes.Black,
                            Width = 50,
                            TextAlignment = TextAlignment.Center,
                            VerticalAlignment = VerticalAlignment.Center
                        };
                        vertexContainer.Children.Add(vertexText);
                        Canvas.SetLeft(vertexText, 0);
                        Canvas.SetTop(vertexText, 12.5);
                        vertex.Text = vertexText;
                    }
                    else
                    {
                        vertexContainer.Children.Add(vertex.Text);
                        Canvas.SetLeft(vertex.Text, 0);
                        Canvas.SetTop(vertex.Text, 12.5);
                    }

                    // Устанавливаем позицию контейнера на холсте
                    Canvas.SetLeft(vertexContainer, x);
                    Canvas.SetTop(vertexContainer, y);

                    GraphCanvas.Children.Add(vertexContainer);
                    vertex.Shape = vertexShape;
                    vertex.Container = vertexContainer;

                    vertexContainer.MouseLeftButtonDown += Vertex_MouseLeftButtonDown;
                    vertexContainer.MouseRightButtonDown += Vertex_MouseRightButtonDown;
                    vertexContainer.MouseRightButtonUp += Vertex_MouseRightButtonUp;
                    vertexContainer.MouseMove += Vertex_MouseMove;
                }
                else
                {
                    // Existing vertex - restore visual elements
                    GraphCanvas.Children.Add(vertex.Container);
                }
            }

            foreach (var edge in _graph.Edges)
            {
                var startVertex = _graph.Vertices.FirstOrDefault(v => v.Id == edge.StartVertexId);
                var endVertex = _graph.Vertices.FirstOrDefault(v => v.Id == edge.EndVertexId);
                if (startVertex == null || endVertex == null) continue;

                Point startCenter = new Point(Canvas.GetLeft(startVertex.Container) + 25, Canvas.GetTop(startVertex.Container) + 25);
                Point endCenter = new Point(Canvas.GetLeft(endVertex.Container) + 25, Canvas.GetTop(endVertex.Container) + 25);
                Point startEdge = GetEdgePoint(startCenter, endCenter, 25);
                Point endEdge = GetEdgePoint(endCenter, startCenter, 25);

                if (edge.Shape == null)
                {
                    Line edgeShape = new Line
                    {
                        Stroke = Brushes.Black,
                        StrokeThickness = 2,
                        X1 = startEdge.X,
                        Y1 = startEdge.Y,
                        X2 = endEdge.X,
                        Y2 = endEdge.Y
                    };
                    edge.Shape = edgeShape;
                }

                // Обновляем координаты рёбер
                edge.Shape.X1 = startEdge.X;
                edge.Shape.Y1 = startEdge.Y;
                edge.Shape.X2 = endEdge.X;
                edge.Shape.Y2 = endEdge.Y;

                // Создаём или обновляем текст веса
                if (edge.WeightText == null)
                {
                    TextBlock weightText = new TextBlock
                    {
                        Text = edge.Weight.ToString(),
                        FontSize = 12,
                        Foreground = Brushes.Black,
                        Background = Brushes.White,
                        Padding = new Thickness(2)
                    };
                    edge.WeightText = weightText;
                }
                Canvas.SetLeft(edge.WeightText, (edge.Shape.X1 + edge.Shape.X2) / 2 - 10);
                Canvas.SetTop(edge.WeightText, (edge.Shape.Y1 + edge.Shape.Y2) / 2 - 10);

                if (!GraphCanvas.Children.Contains(edge.Shape))
                    GraphCanvas.Children.Add(edge.Shape);
                if (!GraphCanvas.Children.Contains(edge.WeightText))
                    GraphCanvas.Children.Add(edge.WeightText);
            }

        }

        private Point GetEdgePoint(Point center, Point target, double radius)
        {
            double dx = target.X - center.X;
            double dy = target.Y - center.Y;
            double distance = Math.Sqrt(dx * dx + dy * dy);

            // Координаты точки пересечения
            double x = center.X + dx * radius / distance;
            double y = center.Y + dy * radius / distance;

            return new Point(x, y);
        }


        private async Task DepthFirstSearchVisual(int startVertexId, TextBlock descriptionText, ListBox stackListBox)
        {
            // Сброс подсветки всех вершин
            foreach (var vertex in _graph.Vertices)
            {
                vertex.Shape.Fill = Brushes.Transparent; // Исходный цвет вершин
            }

            // Сброс текста описания
            descriptionText.Text = "Начинаем обход графа в глубину.\n";

            // Инициализация стека
            var stack = new Stack<Vertex>();
            var visited = new HashSet<int>();

            var startVertex = _graph.Vertices.FirstOrDefault(v => v.Id == startVertexId);
            if (startVertex == null)
            {
                descriptionText.Text = "Стартовая вершина не найдена.";
                return;
            }

            stack.Push(startVertex);
            descriptionText.Text += $"Добавляем начальную вершину {startVertex.Text.Text} в стек.\n";

            while (stack.Count > 0)
            {
                // Получаем текущую вершину
                var currentVertex = stack.Pop();

                // Обновляем состояние стека
                stackListBox.Items.Clear();
                foreach (var v in stack)
                {
                    stackListBox.Items.Add(v.Text.Text);
                }

                // Если вершина уже посещена, пропускаем её
                if (visited.Contains(currentVertex.Id))
                {
                    descriptionText.Text += $"{currentVertex.Text.Text} уже была посещена, пропускаем.\n";
                    continue;
                }

                // Помечаем вершину как посещенную
                visited.Add(currentVertex.Id);
                currentVertex.Shape.Fill = Brushes.Green; // Цвет для посещенной вершины
                descriptionText.Text += $"Посещаем вершину {currentVertex.Text.Text}. Закрашиваем её в зелёный цвет.\n";

                // Задержка для визуализации
                await Task.Delay(500);

                // Добавляем смежные вершины в стек
                foreach (var edge in _graph.Edges.Where(e => e.StartVertexId == currentVertex.Id || e.EndVertexId == currentVertex.Id))
                {
                    var nextVertexId = edge.StartVertexId == currentVertex.Id ? edge.EndVertexId : edge.StartVertexId;

                    if (!visited.Contains(nextVertexId))
                    {
                        var nextVertex = _graph.Vertices.FirstOrDefault(v => v.Id == nextVertexId);
                        if (nextVertex != null)
                        {
                            stack.Push(nextVertex);
                            nextVertex.Shape.Fill = Brushes.Yellow; // Цвет для текущей вершины в обработке
                            descriptionText.Text += $"Добавляем вершину {nextVertex.Text.Text} в стек. Закрашиваем её в жёлтый цвет, так как она готова к обработке.\n";

                            // Обновляем состояние стека
                            stackListBox.Items.Insert(0, nextVertex.Text.Text);
                            await Task.Delay(500);
                        }
                    }
                }
            }

            descriptionText.Text += "Обход завершён. Все доступные вершины посещены.";
        }


        private async Task BreadthFirstSearchVisual(int startVertexId, TextBlock descriptionText, ListBox queueListBox)
        {
            // Сброс подсветки всех вершин
            foreach (var vertex in _graph.Vertices)
            {
                vertex.Shape.Fill = Brushes.Transparent; // Исходный цвет вершин
            }

            // Сброс текста описания
            descriptionText.Text = "Начинаем обход графа в ширину.\n";

            // Инициализация очереди
            var queue = new Queue<Vertex>();
            var visited = new HashSet<int>();

            var startVertex = _graph.Vertices.FirstOrDefault(v => v.Id == startVertexId);
            if (startVertex == null)
            {
                descriptionText.Text = "Стартовая вершина не найдена.";
                return;
            }

            queue.Enqueue(startVertex);
            descriptionText.Text += $"Добавляем начальную вершину {startVertex.Text.Text} в очередь.\n";

            while (queue.Count > 0)
            {
                // Получаем текущую вершину
                var currentVertex = queue.Dequeue();

                // Обновляем состояние очереди
                queueListBox.Items.Clear();
                foreach (var v in queue)
                {
                    queueListBox.Items.Add(v.Text.Text);
                }

                // Если вершина уже посещена, пропускаем её
                if (visited.Contains(currentVertex.Id))
                {
                    descriptionText.Text += $"{currentVertex.Text.Text} уже была посещена, пропускаем.\n";
                    continue;
                }

                // Помечаем вершину как посещённую
                visited.Add(currentVertex.Id);
                currentVertex.Shape.Fill = Brushes.Green; // Цвет для посещённой вершины
                descriptionText.Text += $"Посещаем вершину {currentVertex.Text.Text}. Закрашиваем её в зелёный цвет.\n";

                // Задержка для визуализации
                await Task.Delay(500);

                // Добавляем смежные вершины в очередь
                foreach (var edge in _graph.Edges.Where(e => e.StartVertexId == currentVertex.Id || e.EndVertexId == currentVertex.Id))
                {
                    var nextVertexId = edge.StartVertexId == currentVertex.Id ? edge.EndVertexId : edge.StartVertexId;

                    if (!visited.Contains(nextVertexId))
                    {
                        var nextVertex = _graph.Vertices.FirstOrDefault(v => v.Id == nextVertexId);
                        if (nextVertex != null)
                        {
                            queue.Enqueue(nextVertex);
                            nextVertex.Shape.Fill = Brushes.Yellow; // Цвет для текущей вершины в обработке
                            descriptionText.Text += $"Добавляем вершину {nextVertex.Text.Text} в очередь. Закрашиваем её в жёлтый цвет, так как она готова к обработке.\n";

                            // Обновляем состояние очереди
                            queueListBox.Items.Add(nextVertex.Text.Text);
                            await Task.Delay(500);
                        }
                    }
                }
            }

            descriptionText.Text += "Обход завершён. Все доступные вершины посещены.";
        }

        private async void StartDFSButton_Click(object sender, RoutedEventArgs e)
        {
            int startVertexId = 1; // ID стартовой вершины
            await DepthFirstSearchVisual(startVertexId, DescriptionTextBlock, ListBox);
        }

        private async void StartBFSButton_Click(object sender, RoutedEventArgs e)
        {
            int startVertexId = 1; // ID стартовой вершины
            await BreadthFirstSearchVisual(startVertexId, DescriptionTextBlock, ListBox);
        }

    }
}