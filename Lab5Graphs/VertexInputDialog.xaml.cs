using System.Windows;

namespace Lab5Graphs
{
    public partial class VertexInputDialog : Window
    {
        public string VertexValue { get; private set; }

        public VertexInputDialog()
        {
            InitializeComponent();
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            // Проверка на наличие ввода
            if (!string.IsNullOrWhiteSpace(VertexInputTextBox.Text))
            {
                VertexValue = VertexInputTextBox.Text;
                DialogResult = true;
                Close();
            }
            else
            {
                MessageBox.Show("Please enter a value for the vertex.", "Input Required", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
