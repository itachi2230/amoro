using System.Windows;
using System.Windows.Controls;

namespace Camara_Service
{
    public partial class StockCard : UserControl
    {
        public StockCard()
        {
            InitializeComponent();
            DataContext = this; // Nécessaire pour le binding des propriétés dans ce cas
        }
        public StockCard(string title,string value,double progress,string lastup,double max=100)
        {
            InitializeComponent();
            DataContext = this; // Nécessaire pour le binding des propriétés dans ce cas
            this.Title = title;
            this.Value = value;
            progresss.Maximum = max;
            this.Progress = progress;
            this.LastUpdate = lastup;
            
        }

        public static readonly DependencyProperty TitleProperty =
            DependencyProperty.Register("Title", typeof(string), typeof(StockCard), new PropertyMetadata(string.Empty));

        public string Title
        {
            get => (string)GetValue(TitleProperty);
            set => SetValue(TitleProperty, value);
        }

        public static readonly DependencyProperty ValueProperty =
            DependencyProperty.Register("Value", typeof(string), typeof(StockCard), new PropertyMetadata(string.Empty));

        public string Value
        {
            get => (string)GetValue(ValueProperty);
            set => SetValue(ValueProperty, value);
        }

        public static readonly DependencyProperty ProgressProperty =
            DependencyProperty.Register("Progress", typeof(double), typeof(StockCard), new PropertyMetadata(0.0));

        public double Progress
        {
            get => (double)GetValue(ProgressProperty);
            set => SetValue(ProgressProperty, value);
        }

        public static readonly DependencyProperty LastUpdateProperty =
            DependencyProperty.Register("LastUpdate", typeof(string), typeof(StockCard), new PropertyMetadata(string.Empty));

        public string LastUpdate
        {
            get => (string)GetValue(LastUpdateProperty);
            set => SetValue(LastUpdateProperty, value);
        }
    }
}
