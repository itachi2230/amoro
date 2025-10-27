using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace Camara_Service
{
    public partial class VentesCard : UserControl
    {
        public VentesCard()
        {
            InitializeComponent();
            DataContext = this;
        }

        public VentesCard(string title, string value, double progress, string lastup)
        {
            InitializeComponent();
            DataContext = this;
            this.Title = title;
            this.Value = value;
            this.Progress = progress;
            this.LastUpdate = lastup;
        }

        public static readonly DependencyProperty TitleProperty =
            DependencyProperty.Register("Title", typeof(string), typeof(VentesCard), new PropertyMetadata(string.Empty));

        public string Title
        {
            get => (string)GetValue(TitleProperty);
            set => SetValue(TitleProperty, value);
        }

        public static readonly DependencyProperty ValueProperty =
            DependencyProperty.Register("Value", typeof(string), typeof(VentesCard), new PropertyMetadata(string.Empty));

        public string Value
        {
            get => (string)GetValue(ValueProperty);
            set => SetValue(ValueProperty, value);
        }

        public static readonly DependencyProperty ProgressProperty =
            DependencyProperty.Register("Progress", typeof(double), typeof(VentesCard), new PropertyMetadata(0.0, OnProgressChanged));

        public double Progress
        {
            get => (double)GetValue(ProgressProperty);
            set => SetValue(ProgressProperty, value);
        }

        private static void OnProgressChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var control = (VentesCard)d;
        }

        public static readonly DependencyProperty LastUpdateProperty =
            DependencyProperty.Register("LastUpdate", typeof(string), typeof(VentesCard), new PropertyMetadata(string.Empty));

        public string LastUpdate
        {
            get => (string)GetValue(LastUpdateProperty);
            set => SetValue(LastUpdateProperty, value);
        }
    }
}
