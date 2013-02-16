using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Mygod.Puzzle
{
    public class Reseau : Decorator
    {
        public int Rows
        {
            get { return (int)GetValue(RowsProperty); }
            set { SetValue(RowsProperty, value); }
        }

        public static readonly DependencyProperty RowsProperty = DependencyProperty.Register("Rows", typeof(int), typeof(Reseau), 
            new FrameworkPropertyMetadata(10, FrameworkPropertyMetadataOptions.AffectsRender));

        public int Columns
        {
            get { return (int)GetValue(ColumnsProperty); }
            set { SetValue(ColumnsProperty, value); }
        }

        public static readonly DependencyProperty ColumnsProperty = DependencyProperty.Register("Columns", typeof(int), typeof(Reseau),
            new FrameworkPropertyMetadata(10, FrameworkPropertyMetadataOptions.AffectsRender));

        public Brush BorderBrush
        {
            get { return (Brush)GetValue(BorderBrushProperty); }
            set { SetValue(BorderBrushProperty, value); }
        }

        public static readonly DependencyProperty BorderBrushProperty = Border.BorderBrushProperty.AddOwner(typeof(Reseau), 
            new PropertyMetadata(new SolidColorBrush(Color.FromArgb(0x66, 0xdd, 0xdd, 0xdd))));

        public double BorderThickness
        {
            get { return (double)GetValue(BorderThicknessProperty); }
            set { SetValue(BorderThicknessProperty, value); }
        }

        public static readonly DependencyProperty BorderThicknessProperty = DependencyProperty.Register("BorderThickness", 
            typeof(double), typeof(Reseau), new PropertyMetadata(1.0));
        
        protected override void OnRender(DrawingContext drawingContext)
        {
            base.OnRender(drawingContext);
            var pen = new Pen(BorderBrush, BorderThickness);
            double width = ActualWidth / Columns, height = ActualHeight / Rows;
            for (var x = 0; x <= Columns; x++)
            {
                var t = x * width;
                drawingContext.DrawLine(pen, new Point(t, 0), new Point(t, ActualHeight));
            }
            for (var y = 0; y <= Rows; y++)
            {
                var t = y * height;
                drawingContext.DrawLine(pen, new Point(0, t), new Point(ActualWidth, t));
            }
        }
    }
}
