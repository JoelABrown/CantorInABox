using System.Collections;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;

namespace Mooseware.CantorInABox;

/// <summary>
/// Utility class that encapsulates Drag Drop behaviour for WPF ItemRepeater objects
/// </summary>
public class DragDropHelper
{
    // source and target
    private readonly DataFormat _format = DataFormats.GetDataFormat("DragDropItemsControl");
    private Point _initialMousePosition;
    private Vector _initialMouseOffset;
    private object? _draggedData;
    private DraggedAdorner? _draggedAdorner;
    private InsertionAdorner? _insertionAdorner;
    private Window? _topWindow;
    // source
    private ItemsControl? _sourceItemsControl;
    private FrameworkElement? _sourceItemContainer;
    // target
    private ItemsControl? _targetItemsControl;
    private FrameworkElement? _targetItemContainer;
    private bool _hasVerticalOrientation;
    private int _insertionIndex;
    private bool _isInFirstHalf;
    // singleton
    private static readonly DragDropHelper _instance = new();
    private static DragDropHelper Instance
    {
        get
        {
            return _instance;
        }
    }

    public static bool GetIsDragSource(DependencyObject obj)
    {
        return (bool)obj.GetValue(IsDragSourceProperty);
    }

    public static void SetIsDragSource(DependencyObject obj, bool value)
    {
        obj.SetValue(IsDragSourceProperty, value);
    }

    public static readonly DependencyProperty IsDragSourceProperty =
        DependencyProperty.RegisterAttached("IsDragSource", typeof(bool), typeof(DragDropHelper), new UIPropertyMetadata(false, IsDragSourceChanged));

    public static bool GetIsDropTarget(DependencyObject obj)
    {
        return (bool)obj.GetValue(IsDropTargetProperty);
    }

    public static void SetIsDropTarget(DependencyObject obj, bool value)
    {
        obj.SetValue(IsDropTargetProperty, value);
    }

    public static readonly DependencyProperty IsDropTargetProperty =
        DependencyProperty.RegisterAttached("IsDropTarget", typeof(bool), typeof(DragDropHelper), new UIPropertyMetadata(false, IsDropTargetChanged));

    public static DataTemplate GetDragDropTemplate(DependencyObject obj)
    {
        return (DataTemplate)obj.GetValue(DragDropTemplateProperty);
    }

    public static void SetDragDropTemplate(DependencyObject obj, DataTemplate value)
    {
        obj.SetValue(DragDropTemplateProperty, value);
    }

    public static readonly DependencyProperty DragDropTemplateProperty =
        DependencyProperty.RegisterAttached("DragDropTemplate", typeof(DataTemplate), typeof(DragDropHelper), new UIPropertyMetadata(null));

    private static void IsDragSourceChanged(DependencyObject obj, DependencyPropertyChangedEventArgs e)
    {
        if (obj is ItemsControl dragSource)
        {
            if (Object.Equals(e.NewValue, true))
            {
                dragSource.PreviewMouseLeftButtonDown += Instance.DragSource_PreviewMouseLeftButtonDown;
                dragSource.PreviewMouseLeftButtonUp += Instance.DragSource_PreviewMouseLeftButtonUp;
                dragSource.PreviewMouseMove += Instance.DragSource_PreviewMouseMove;
            }
            else
            {
                dragSource.PreviewMouseLeftButtonDown -= Instance.DragSource_PreviewMouseLeftButtonDown;
                dragSource.PreviewMouseLeftButtonUp -= Instance.DragSource_PreviewMouseLeftButtonUp;
                dragSource.PreviewMouseMove -= Instance.DragSource_PreviewMouseMove;
            }
        }
    }

    private static void IsDropTargetChanged(DependencyObject obj, DependencyPropertyChangedEventArgs e)
    {
        if (obj is ItemsControl dropTarget)
        {
            if (Object.Equals(e.NewValue, true))
            {
                dropTarget.AllowDrop = true;
                dropTarget.PreviewDrop += Instance.DropTarget_PreviewDrop;
                dropTarget.PreviewDragEnter += Instance.DropTarget_PreviewDragEnter;
                dropTarget.PreviewDragOver += Instance.DropTarget_PreviewDragOver;
                dropTarget.PreviewDragLeave += Instance.DropTarget_PreviewDragLeave;
            }
            else
            {
                dropTarget.AllowDrop = false;
                dropTarget.PreviewDrop -= Instance.DropTarget_PreviewDrop;
                dropTarget.PreviewDragEnter -= Instance.DropTarget_PreviewDragEnter;
                dropTarget.PreviewDragOver -= Instance.DropTarget_PreviewDragOver;
                dropTarget.PreviewDragLeave -= Instance.DropTarget_PreviewDragLeave;
            }
        }
    }

    // DragSource
    private void DragSource_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        try
        {
            this._sourceItemsControl = (ItemsControl)sender;

            this._topWindow = Window.GetWindow(this._sourceItemsControl);
            this._initialMousePosition = e.GetPosition(this._topWindow);

            if (e.OriginalSource is Visual visual)
            {
                FrameworkElement? visualElement = _sourceItemsControl.ContainerFromElement(visual) as FrameworkElement;
                if (visualElement is not null)
                {
                    this._sourceItemContainer = visualElement;
                    this._draggedData = this._sourceItemContainer.DataContext;
                }
            }
        }
        catch (Exception ex)
        {   // For debugging only...
            System.Diagnostics.Debug.WriteLine(ex.ToString());
        }
    }

    // Drag = mouse down + move by a certain amount
    private void DragSource_PreviewMouseMove(object sender, MouseEventArgs e)
    {
        try
        {
            if (this._draggedData is not null)
            {
                // Only drag when user moved the mouse by a reasonable amount.
                if (Utilities.IsMovementBigEnough(this._initialMousePosition, e.GetPosition(this._topWindow))
                    && _sourceItemContainer is not null
                    && this._topWindow is not null)
                {
                    this._initialMouseOffset = this._initialMousePosition - this._sourceItemContainer.TranslatePoint(new Point(0, 0), this._topWindow);

                    DataObject data = new(this._format.Name, this._draggedData);

                    // Adding events to the window to make sure dragged adorner comes up when mouse is not over a drop target.
                    bool previousAllowDrop = this._topWindow.AllowDrop;
                    this._topWindow.AllowDrop = true;
                    this._topWindow.DragEnter += TopWindow_DragEnter;
                    this._topWindow.DragOver += TopWindow_DragOver;
                    this._topWindow.DragLeave += TopWindow_DragLeave;

                    DragDropEffects effects = DragDrop.DoDragDrop((DependencyObject)sender, data, DragDropEffects.Move);

                    // Without this call, there would be a bug in the following scenario: Click on a data item, and drag
                    // the mouse very fast outside of the window. When doing this really fast, for some reason I don't get 
                    // the Window leave event, and the dragged adorner is _left behind.
                    // With this call, the dragged adorner will disappear when we release the mouse outside of the window,
                    // which is when the DoDragDrop synchronous method returns.
                    RemoveDraggedAdorner();

                    this._topWindow.AllowDrop = previousAllowDrop;
                    this._topWindow.DragEnter -= TopWindow_DragEnter;
                    this._topWindow.DragOver -= TopWindow_DragOver;
                    this._topWindow.DragLeave -= TopWindow_DragLeave;

                    this._draggedData = null;
                }
            }
        }
        catch (Exception ex)
        {   // For debugging only...
            System.Diagnostics.Debug.WriteLine(ex.ToString());
        }
    }

    private void DragSource_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        this._draggedData = null;
    }

    // DropTarget

    private void DropTarget_PreviewDragEnter(object sender, DragEventArgs e)
    {
        try
        {
            this._targetItemsControl = (ItemsControl)sender;
            object draggedItem = e.Data.GetData(this._format.Name);

            DecideDropTarget(e);
            if (draggedItem is not null)
            {
                // Dragged Adorner is created on the first enter only.
                ShowDraggedAdorner(e.GetPosition(this._topWindow));
                CreateInsertionAdorner();
            }
            e.Handled = true;
        }
        catch (Exception ex)
        {   // For debugging only...
            System.Diagnostics.Debug.WriteLine(ex.ToString());
        }
    }

    private void DropTarget_PreviewDragOver(object sender, DragEventArgs e)
    {
        try
        {
            object draggedItem = e.Data.GetData(this._format.Name);

            DecideDropTarget(e);
            if (draggedItem is not null)
            {
                // Dragged Adorner is only updated here - it has already been created in DragEnter.
                ShowDraggedAdorner(e.GetPosition(this._topWindow));
                UpdateInsertionAdornerPosition();
            }
            e.Handled = true;
        }
        catch (Exception ex)
        {   // For debugging only...
            System.Diagnostics.Debug.WriteLine(ex.ToString());
        }
    }

    private void DropTarget_PreviewDrop(object sender, DragEventArgs e)
    {
        try
        {
            object draggedItem = e.Data.GetData(this._format.Name);
            int indexRemoved = -1;

            if (draggedItem is not null
                    && this._targetItemsControl is not null)
            {
                if ((e.Effects & DragDropEffects.Move) != 0 
                    && this._sourceItemsControl is not null)
                {
                    indexRemoved = Utilities.RemoveItemFromItemsControl(this._sourceItemsControl, draggedItem);
                }
                // This happens when we drag an item to a later position within the same ItemsControl.
                if (indexRemoved != -1 && this._sourceItemsControl == this._targetItemsControl && indexRemoved < this._insertionIndex)
                {
                    this._insertionIndex--;
                }
                Utilities.InsertItemInItemsControl(this._targetItemsControl, draggedItem, this._insertionIndex);

                RemoveDraggedAdorner();
                RemoveInsertionAdorner();
            }
            e.Handled = true;
        }
        catch (Exception ex)
        {   // For debugging only...
            System.Diagnostics.Debug.WriteLine(ex.ToString());
        }
    }

    private void DropTarget_PreviewDragLeave(object sender, DragEventArgs e)
    {
        try
        {
            // Dragged Adorner is only created once on DragEnter + every time we enter the window. 
            // It's only removed once on the DragDrop, and every time we leave the window. (so no need to remove it here)
            object draggedItem = e.Data.GetData(this._format.Name);

            if (draggedItem is not null)
            {
                RemoveInsertionAdorner();
            }
            e.Handled = true;
        }
        catch (Exception ex)
        {   // For debugging only...
            System.Diagnostics.Debug.WriteLine(ex.ToString());
        }
    }

    // If the types of the dragged data and ItemsControl's source are compatible, 
    // there are 3 situations to have into account when deciding the drop target:
    // 1. mouse is over an items container
    // 2. mouse is over the empty part of an ItemsControl, but ItemsControl is not empty
    // 3. mouse is over an empty ItemsControl.
    // The goal of this method is to decide on the values of the following properties: 
    // _targetItemContainer, _insertionIndex and _isInFirstHalf.
    private void DecideDropTarget(DragEventArgs e)
    {
        int targetItemsControlCount = 0;
        if (this._targetItemsControl is not null)
        {
            targetItemsControlCount = this._targetItemsControl.Items.Count;
        }
        object draggedItem = e.Data.GetData(this._format.Name);

        if (IsDropDataTypeAllowed(draggedItem))
        {
            if (targetItemsControlCount > 0)
            {
                if (this._targetItemsControl is not null
                    && this._targetItemsControl.ItemContainerGenerator is not null
                    && this._targetItemsControl.ItemContainerGenerator.ContainerFromIndex(0) is not null)
                {
                    var frameworkElement = this._targetItemsControl.ItemContainerGenerator.ContainerFromIndex(0) as FrameworkElement;
                    if (frameworkElement is not null)
                    {
                        this._hasVerticalOrientation = Utilities.HasVerticalOrientation(frameworkElement);
                    }
                    this._targetItemContainer = _targetItemsControl.ContainerFromElement((DependencyObject)e.OriginalSource) as FrameworkElement;

                    if (this._targetItemContainer is not null)
                    {
                        Point positionRelativeToItemContainer = e.GetPosition(this._targetItemContainer);
                        this._isInFirstHalf = Utilities.IsInFirstHalf(this._targetItemContainer, positionRelativeToItemContainer, this._hasVerticalOrientation);
                        this._insertionIndex = this._targetItemsControl.ItemContainerGenerator.IndexFromContainer(this._targetItemContainer);

                        if (!this._isInFirstHalf)
                        {
                            this._insertionIndex++;
                        }
                    }
                    else
                    {
                        this._targetItemContainer = this._targetItemsControl.ItemContainerGenerator.ContainerFromIndex(targetItemsControlCount - 1) as FrameworkElement;
                        this._isInFirstHalf = false;
                        this._insertionIndex = targetItemsControlCount;
                    }
                }
                else
                {
                    this._targetItemContainer = null;
                    this._insertionIndex = 0;
                }
            }
            else
            {
                this._targetItemContainer = null;
                this._insertionIndex = 0;
            }
        }
        else
        {
            this._targetItemContainer = null;
            this._insertionIndex = -1;
            e.Effects = DragDropEffects.None;
        }
    }

    // Can the dragged data be added to the destination collection?
    // It can if destination is bound to IList<allowed type>, IList or not data bound.
    private bool IsDropDataTypeAllowed(object draggedItem)
    {
        bool isDropDataTypeAllowed;
        IEnumerable? collectionSource = this._targetItemsControl!.ItemsSource;
        if (draggedItem is not null)
        {
            if (collectionSource is not null)
            {
                Type draggedType = draggedItem.GetType();
                Type collectionType = collectionSource.GetType();

                Type genericIListType = collectionType.GetInterface("IList`1")!;
                if (genericIListType is not null)
                {
                    Type[] genericArguments = genericIListType.GetGenericArguments();
                    isDropDataTypeAllowed = genericArguments[0].IsAssignableFrom(draggedType);
                }
                else if (typeof(IList).IsAssignableFrom(collectionType))
                {
                    isDropDataTypeAllowed = true;
                }
                else
                {
                    isDropDataTypeAllowed = false;
                }
            }
            else // the ItemsControl's ItemsSource is not data bound.
            {
                isDropDataTypeAllowed = true;
            }
        }
        else
        {
            isDropDataTypeAllowed = false;
        }
        return isDropDataTypeAllowed;
    }

    // Window

    private void TopWindow_DragEnter(object sender, DragEventArgs e)
    {
        try
        {
            ShowDraggedAdorner(e.GetPosition(this._topWindow));
            e.Effects = DragDropEffects.None;
            e.Handled = true;
        }
        catch (Exception ex)
        {   // For debugging only...
            System.Diagnostics.Debug.WriteLine(ex.ToString());
        }
    }

    private void TopWindow_DragOver(object sender, DragEventArgs e)
    {
        try
        {
            ShowDraggedAdorner(e.GetPosition(this._topWindow));
            e.Effects = DragDropEffects.None;
            e.Handled = true;
        }
        catch (Exception ex)
        {   // For debugging only...
            System.Diagnostics.Debug.WriteLine(ex.ToString());
        }
    }

    private void TopWindow_DragLeave(object sender, DragEventArgs e)
    {
        try
        {
            RemoveDraggedAdorner();
            e.Handled = true;
        }
        catch (Exception ex)
        {   // For debugging only...
            System.Diagnostics.Debug.WriteLine(ex.ToString());
        }
    }

    // Adorners

    // Creates or updates the dragged Adorner. 
    private void ShowDraggedAdorner(Point currentPosition)
    {
        if (this._draggedAdorner == null 
            && this._draggedData is not null
            && this._sourceItemsControl is not null
            && this._sourceItemContainer is not null)
        {
            var adornerLayer = AdornerLayer.GetAdornerLayer(this._sourceItemsControl);
            this._draggedAdorner = new DraggedAdorner(this._draggedData, GetDragDropTemplate(this._sourceItemsControl), this._sourceItemContainer, adornerLayer);
        }
        this._draggedAdorner?.SetPosition(currentPosition.X - this._initialMousePosition.X + this._initialMouseOffset.X, currentPosition.Y - this._initialMousePosition.Y + this._initialMouseOffset.Y);
    }

    private void RemoveDraggedAdorner()
    {
        if (this._draggedAdorner is not null)
        {
            this._draggedAdorner.Detach();
            this._draggedAdorner = null;
        }
    }

    private void CreateInsertionAdorner()
    {
        if (this._targetItemContainer is not null)
        {
            // Here, I need to get adorner layer from _targetItemContainer and not _targetItemsControl. 
            // This way I get the AdornerLayer within ScrollContentPresenter, and not the one under AdornerDecorator (Snoop is awesome).
            // If I used _targetItemsControl, the adorner would hang out of ItemsControl when there's a horizontal scroll bar.
            var adornerLayer = AdornerLayer.GetAdornerLayer(this._targetItemContainer);
            this._insertionAdorner = new InsertionAdorner(this._hasVerticalOrientation, this._isInFirstHalf, this._targetItemContainer, adornerLayer);
        }
    }

    private void UpdateInsertionAdornerPosition()
    {
        if (this._insertionAdorner is not null)
        {
            this._insertionAdorner.IsInFirstHalf = this._isInFirstHalf;
            this._insertionAdorner.InvalidateVisual();
        }
    }

    private void RemoveInsertionAdorner()
    {
        if (this._insertionAdorner is not null)
        {
            this._insertionAdorner.Detach();
            this._insertionAdorner = null;
        }
    }
}
