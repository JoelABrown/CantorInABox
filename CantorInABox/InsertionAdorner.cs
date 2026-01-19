using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;

namespace Mooseware.CantorInABox
{
    /// <summary>
    /// Utility class that provides visual feedback for DragDrop operations on WPF ItemsRepeater objects
    /// </summary>
    public class InsertionAdorner : Adorner
    {
        private readonly bool _isSeparatorHorizontal;
        public bool IsInFirstHalf { get; set; }
        private readonly AdornerLayer _adornerLayer;
        private static readonly Pen _pen;
        private static readonly PathGeometry _triangle;

        // Create the _pen and _triangle in a static constructor and freeze them to improve performance.
        static InsertionAdorner()
        {
            _pen = new Pen { Brush = Brushes.Gray, Thickness = 2 };
            _pen.Freeze();

            LineSegment firstLine = new(new Point(0, -5), false);
            firstLine.Freeze();
            LineSegment secondLine = new(new Point(0, 5), false);
            secondLine.Freeze();

            PathFigure figure = new() { StartPoint = new Point(5, 0) };
            figure.Segments.Add(firstLine);
            figure.Segments.Add(secondLine);
            figure.Freeze();

            _triangle = new PathGeometry();
            _triangle.Figures.Add(figure);
            _triangle.Freeze();
        }

        public InsertionAdorner(bool isSeparatorHorizontal, bool isInFirstHalf, UIElement adornedElement, AdornerLayer adornerLayer)
            : base(adornedElement)
        {
            this._isSeparatorHorizontal = isSeparatorHorizontal;
            this.IsInFirstHalf = isInFirstHalf;
            this._adornerLayer = adornerLayer;
            this.IsHitTestVisible = false;

            this._adornerLayer.Add(this);
        }

        // This draws one line and two triangles at each end of the line.
        protected override void OnRender(DrawingContext drawingContext)
        {
            CalculateStartAndEndPoint(out Point startPoint, out Point endPoint);
            drawingContext.DrawLine(_pen, startPoint, endPoint);

            if (this._isSeparatorHorizontal)
            {
                DrawTriangle(drawingContext, startPoint, 0);
                DrawTriangle(drawingContext, endPoint, 180);
            }
            else
            {
                DrawTriangle(drawingContext, startPoint, 90);
                DrawTriangle(drawingContext, endPoint, -90);
            }
        }

        private static void DrawTriangle(DrawingContext drawingContext, Point origin, double angle)
        {
            drawingContext.PushTransform(new TranslateTransform(origin.X, origin.Y));
            drawingContext.PushTransform(new RotateTransform(angle));

            drawingContext.DrawGeometry(_pen.Brush, null, _triangle);

            drawingContext.Pop();
            drawingContext.Pop();
        }

        private void CalculateStartAndEndPoint(out Point startPoint, out Point endPoint)
        {
            startPoint = new Point();
            endPoint = new Point();

            double width = this.AdornedElement.RenderSize.Width;
            double height = this.AdornedElement.RenderSize.Height;

            if (this._isSeparatorHorizontal)
            {
                endPoint.X = width;
                if (!this.IsInFirstHalf)
                {
                    startPoint.Y = height;
                    endPoint.Y = height;
                }
            }
            else
            {
                endPoint.Y = height;
                if (!this.IsInFirstHalf)
                {
                    startPoint.X = width;
                    endPoint.X = width;
                }
            }
        }

        public void Detach()
        {
            this._adornerLayer.Remove(this);
        }

    }
}
