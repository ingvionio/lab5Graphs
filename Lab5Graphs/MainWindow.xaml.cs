using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using System.IO;
using Microsoft.Win32;


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
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "Text files (*.txt)|*.txt|All files (*.*)|*.*"; // Set file filter

            if (openFileDialog.ShowDialog() == true) // Show dialog and check if OK was clicked
            {
                string filePath = openFileDialog.FileName;
                _graph.ReadAdjacencyMatrixFromFile(filePath);
                DrawGraph();
                MessageBox.Show("Graph loaded from " + filePath);
            }
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            SaveFileDialog saveFileDialog = new SaveFileDialog();
            saveFileDialog.Filter = "Text files (*.txt)|*.txt|All files (*.*)|*.*"; // Set file filter

            if (saveFileDialog.ShowDialog() == true) // Show dialog and check if OK was clicked
            {
                string filePath = saveFileDialog.FileName;
                _graph.WriteAdjacencyMatrixToFile(filePath);
                MessageBox.Show("Graph saved to " + filePath);
            }
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

        private async Task MaxFlowVisual(int source, int sink, TextBlock descriptionText, ListBox listBox)
        {
            int[,] capacityMatrix = _graph.ToCapacityMatrix();
            MaxFlow maxFlowObj = new MaxFlow(capacityMatrix);
            int n = capacityMatrix.GetLength(0);

            for (int i = 0; i < n; i++)
            {
                for (int j = 0; j < n; j++)
                {
                    maxFlowObj.residual[i, j] = maxFlowObj.capacity[i, j];
                }
            }

            int maxFlow = 0;

            while (maxFlowObj.BFS(source, sink))
            {
                int pathFlow = int.MaxValue;
                Stack<int> path = new Stack<int>();
                int v = sink;

                // Идем от стока к источнику, чтобы найти минимальную пропускную способность
                while (v != source)
                {
                    path.Push(v);
                    int u = maxFlowObj.parent[v];
                    pathFlow = Math.Min(pathFlow, maxFlowObj.residual[u, v]);
                    v = u;
                }
                path.Push(source);

                descriptionText.Text += $"Найден увеличивающий путь с пропускной способностью {pathFlow}\n";

                // Идем от источника к стоку, чтобы обновить остаточные пропускные способности
                while (path.Count > 1)
                {
                    int u = path.Pop();
                    int w = path.Peek();
                    maxFlowObj.residual[u, w] -= pathFlow;
                    maxFlowObj.residual[w, u] += pathFlow;

                    // Визуализация изменения потока
                    var edge = _graph.Edges.FirstOrDefault(e => (e.StartVertexId == u + 1 && e.EndVertexId == w + 1) || (e.StartVertexId == w + 1 && e.EndVertexId == u + 1));
                    if (edge != null)
                    {
                        edge.Shape.Stroke = Brushes.Blue;
                        edge.WeightText.Text = (edge.Weight - maxFlowObj.residual[u, w]).ToString();
                        descriptionText.Text += $"Обновляем ребро ({u + 1}, {w + 1}) с новым остаточным потоком {edge.Weight - maxFlowObj.residual[u, w]}\n";
                        await Task.Delay(500);
                    }
                }

                maxFlow += pathFlow;
                descriptionText.Text += $"Текущий максимальный поток: {maxFlow}\n";
            }

            descriptionText.Text += $"Максимальный поток: {maxFlow}\n";
        }

        /*
         private async Task MaxFlowVisual(int source, int sink, TextBlock descriptionText, ListBox listBox)
        {
            int[,] capacityMatrix = _graph.ToCapacityMatrix();
            MaxFlow maxFlowObj = new MaxFlow(capacityMatrix);
            int n = capacityMatrix.GetLength(0);

            for (int i = 0; i < n; i++)
            {
                for (int j = 0; j < n; j++)
                {
                    maxFlowObj.residual[i, j] = maxFlowObj.capacity[i, j];
                }
            }

            int maxFlow = 0;

            while (maxFlowObj.BFS(source, sink))
            {
                int pathFlow = int.MaxValue;
                for (int v = sink; v != source; v = maxFlowObj.parent[v])
                {
                    int u = maxFlowObj.parent[v];
                    pathFlow = Math.Min(pathFlow, maxFlowObj.residual[u, v]);
                }

                descriptionText.Text += $"Найден увеличивающий путь с пропускной способностью {pathFlow}\n";

                for (int v = sink; v != source; v = maxFlowObj.parent[v])
                {
                    int u = maxFlowObj.parent[v];
                    maxFlowObj.residual[u, v] -= pathFlow;
                    maxFlowObj.residual[v, u] += pathFlow;

                    // Визуализация изменения потока
                    var edge = _graph.Edges.FirstOrDefault(e => (e.StartVertexId == u + 1 && e.EndVertexId == v + 1) || (e.StartVertexId == v + 1 && e.EndVertexId == u + 1));
                    if (edge != null)
                    {
                        edge.Shape.Stroke = Brushes.Blue;
                        edge.WeightText.Text = (edge.Weight - maxFlowObj.residual[u, v]).ToString();
                        descriptionText.Text += $"Обновляем ребро ({u + 1}, {v + 1}) с новым остаточным потоком {edge.Weight - maxFlowObj.residual[u, v]}\n";
                        await Task.Delay(500);
                    }
                }

                maxFlow += pathFlow;
                descriptionText.Text += $"Текущий максимальный поток: {maxFlow}\n";
            }

            descriptionText.Text += $"Максимальный поток: {maxFlow}\n";
        }*/

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

        private async void StartAlgorithmButton_Click(object sender, RoutedEventArgs e)
        {
            ResetGraphVisualization();

            int startVertexId = 1;

            string selectedAlgorithm = (string)(AlgorithmComboBox.SelectedItem as ComboBoxItem)?.Content;

            if (selectedAlgorithm == "Depth-First Search (DFS)")
            {
                await DepthFirstSearchVisual(startVertexId, DescriptionTextBlock, ListBox);
            }
            else if (selectedAlgorithm == "Breadth-First Search (BFS)")
            {
                await BreadthFirstSearchVisual(startVertexId, DescriptionTextBlock, ListBox);
            }
            else if (selectedAlgorithm == "Minimum Spanning Tree (MST)")
            {
                await MinimumSpanningTreeVisual(DescriptionTextBlock); // Call the MST visualization
            }
            else if (selectedAlgorithm == "Maximum Flow")
            {
                await MaxFlowVisual(0, _graph.Vertices.Count - 1, DescriptionTextBlock, ListBox);
            }
        }

        private async Task MinimumSpanningTreeVisual(TextBlock descriptionText)
        {
            descriptionText.Text = "Построение минимального остовного дерева (алгоритм Прима):\n\n";
            descriptionText.Text += "Алгоритм Прима находит минимальное остовное дерево для связного взвешенного графа. \n";
            descriptionText.Text += "Остовное дерево — это подграф, включающий все вершины исходного графа и являющийся деревом (т.е. не содержит циклов). \n";
            descriptionText.Text += "Минимальное остовное дерево имеет наименьший суммарный вес ребер среди всех возможных остовных деревьев.\n\n";


            List<Edge> mstEdges = _graph.FindMinimumSpanningTree();

            if (mstEdges.Count < _graph.Vertices.Count - 1 && _graph.Vertices.Count > 0)
            {
                descriptionText.Text += "Граф несвязный! Невозможно построить остовное дерево.\n";
                return;
            }

            descriptionText.Text += "Шаги алгоритма:\n";

            HashSet<int> visitedVertices = new HashSet<int>();
            visitedVertices.Add(_graph.Vertices[0].Id); // Начинаем с первой вершины
            descriptionText.Text += $"1. Начинаем с вершины {_graph.Vertices[0].Text.Text}.\n";
            await Task.Delay(500);

            int stepCounter = 2;
            while (visitedVertices.Count < _graph.Vertices.Count)
            {
                Edge minEdge = null;
                foreach (var vertex in _graph.Vertices.Where(v => visitedVertices.Contains(v.Id)))
                {
                    foreach (var edge in _graph.Edges.Where(e => (e.StartVertexId == vertex.Id || e.EndVertexId == vertex.Id)))
                    {
                        int otherVertex = edge.StartVertexId == vertex.Id ? edge.EndVertexId : edge.StartVertexId;
                        if (!visitedVertices.Contains(otherVertex))
                        {
                            if (minEdge == null || edge.Weight < minEdge.Weight)
                            {
                                minEdge = edge;
                            }
                        }
                    }
                }

                if (minEdge != null)
                {
                    minEdge.Shape.Stroke = Brushes.Red; // Подсвечиваем ребро MST
                    string startVertexName = _graph.Vertices.First(v => v.Id == minEdge.StartVertexId).Text.Text;
                    string endVertexName = _graph.Vertices.First(v => v.Id == minEdge.EndVertexId).Text.Text;
                    visitedVertices.Add(visitedVertices.Contains(minEdge.StartVertexId) ? minEdge.EndVertexId : minEdge.StartVertexId);

                    descriptionText.Text += $"{stepCounter}. Из всех рёбер, соединяющих уже посещенные вершины с непосещенными, выбираем ребро с минимальным весом: ({startVertexName}, {endVertexName}).\n";
                    await Task.Delay(500); // Задержка для визуализации
                    stepCounter++;
                }
                else
                {

                    break; // Exit if no minEdge is found (for disconnected graphs)
                }
            }

            descriptionText.Text += $"Минимальное остовное дерево построено (всего {mstEdges.Count} ребер).\n";
        }

        private void ResetGraphVisualization()
        {
            foreach (var vertex in _graph.Vertices)
            {
                vertex.Shape.Fill = Brushes.Transparent;
            }

            foreach (var edge in _graph.Edges)
            {
                edge.Shape.Stroke = Brushes.Black;
            }

            DescriptionTextBlock.Text = ""; // Clear log
            ListBox.Items.Clear(); // Clear Stack/Queue display

        }

        private void AlgorithmComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            //Clear log when algorithm changes
            DescriptionTextBlock.Text = "";

        }

    }
}