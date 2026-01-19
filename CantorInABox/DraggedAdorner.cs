using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;

namespace Mooseware.CantorInABox;

/// <summary>
/// Utility class that provides visual feedback for DragDrop operations on WPF ItemsRepeater objects
/// </summary>
public class DraggedAdorner : Adorner
{
    private readonly ContentPresenter _contentPresenter;
    private double _left;
    private double _top;
    private readonly AdornerLayer _adornerLayer;

    public DraggedAdorner(object dragDropData, DataTemplate dragDropTemplate, UIElement adornedElement, AdornerLayer adornerLayer)
        : base(adornedElement)
    {
        this._adornerLayer = adornerLayer;

        this._contentPresenter = new ContentPresenter
        {
            Content = dragDropData,
            ContentTemplate = dragDropTemplate,
            Opacity = 0.7
        };

        this._adornerLayer.Add(this);
    }

    public void SetPosition(double left, double top)
    {
        // -1 and +13 align the dragged adorner with the dashed rectangle that shows up
        // near the mouse cursor when dragging.
        this._left = left - 1;
        this._top = top + 13;
        this._adornerLayer?.Update(this.AdornedElement);
    }

    protected override Size MeasureOverride(Size constraint)
    {
        this._contentPresenter.Measure(constraint);
        return this._contentPresenter.DesiredSize;
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        this._contentPresenter.Arrange(new Rect(finalSize));
        return finalSize;
    }

    protected override Visual GetVisualChild(int index)
    {
        return this._contentPresenter;
    }

    protected override int VisualChildrenCount
    {
        get { return 1; }
    }

    public override GeneralTransform GetDesiredTransform(GeneralTransform transform)
    {
        GeneralTransformGroup result = new();
        result.Children.Add(base.GetDesiredTransform(transform));
        result.Children.Add(new TranslateTransform(this._left, this._top));

        return result;
    }

    public void Detach()
    {
        this._adornerLayer.Remove(this);
    }

}
