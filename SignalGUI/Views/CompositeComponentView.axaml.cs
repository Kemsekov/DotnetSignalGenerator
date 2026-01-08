using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.VisualTree;
using SignalGUI.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SignalGUI.Views;

public partial class CompositeComponentView : UserControl
{
    private int _dragStartIndex = -1;
    private Point _startDragPoint;

    public CompositeComponentView()
    {
        InitializeComponent();
        // Setup drag and drop after the control is loaded
        this.Loaded += OnLoaded;
    }

    private void OnLoaded(object? sender, RoutedEventArgs e)
    {
    }

    // Event handler for when a drag handle is pressed
    public void OnFilterDragHandlePointerPressed(object? sender, PointerPressedEventArgs e)
    {
        var dragHandle = sender as Border;
        if (dragHandle != null && this.FindControl<ListBox>("FilterList") is ListBox filterList)
        {
            // Get the data context of the item containing this drag handle
            var itemDataContext = dragHandle.DataContext as FilterItemViewModel;
            if (itemDataContext != null)
            {
                // Find the index of this item in the collection
                var filters = (DataContext as CompositeComponentViewModel)?.Filters;
                if (filters != null)
                {
                    _dragStartIndex = filters.IndexOf(itemDataContext);
                    if (_dragStartIndex >= 0)
                    {
                        _startDragPoint = e.GetPosition(this);

                        // Add event handlers for dragging
                        this.PointerMoved += OnFilterPointerMoved;
                        this.PointerReleased += OnFilterPointerReleased;
                    }
                }
            }
        }
    }

    private void OnFilterPointerMoved(object? sender, PointerEventArgs e)
    {
        if (_dragStartIndex >= 0)
        {
            var currentPoint = e.GetPosition(this);
            var deltaY = currentPoint.Y - _startDragPoint.Y;

            // Only start moving after moving a threshold distance
            if (Math.Abs(deltaY) > 5 && this.FindControl<ListBox>("FilterList") is ListBox filterList)
            {
                // Find the target index based on the current position
                var targetIndex = GetFilterDropIndex(filterList, currentPoint);

                if (targetIndex != -1 && targetIndex != _dragStartIndex)
                {
                    // Move the item in the ViewModel
                    if (DataContext is CompositeComponentViewModel viewModel)
                    {
                        viewModel.MoveFilterAt(_dragStartIndex, targetIndex);

                        // Update the start index to the new position
                        _dragStartIndex = targetIndex;

                        // Update the start drag point to the new position
                        _startDragPoint = currentPoint;
                    }
                }
            }
        }
    }

    private void OnFilterPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        // Remove event handlers
        this.PointerMoved -= OnFilterPointerMoved;
        this.PointerReleased -= OnFilterPointerReleased;

        // Reset drag state
        _dragStartIndex = -1;
    }

    private int GetFilterDropIndex(ListBox listBox, Point point)
    {
        // Get all ListBoxItem containers
        var listBoxItems = new List<Control>();
        for (int i = 0; i < listBox.ItemCount; i++)
        {
            var container = listBox.ContainerFromIndex(i) as Control;
            if (container != null)
            {
                listBoxItems.Add(container);
            }
        }

        // Find which item the point is over
        for (int i = 0; i < listBoxItems.Count; i++)
        {
            var container = listBoxItems[i];
            var containerPoint = container.TranslatePoint(new Point(0, 0), this);
            if (containerPoint != null)
            {
                var rect = new Rect(containerPoint.Value, container.Bounds.Size);
                if (rect.Contains(point))
                {
                    // Determine if we're in the upper or lower half of the item
                    var relativeY = point.Y - containerPoint.Value.Y;
                    return relativeY < rect.Height / 2 ? i : i + 1;
                }
            }
        }

        // Check if we're past the last item
        if (listBoxItems.Count > 0)
        {
            var lastContainer = listBoxItems[listBoxItems.Count - 1];
            var lastPoint = lastContainer.TranslatePoint(new Point(0, 0), this);
            if (lastPoint != null && point.Y > lastPoint.Value.Y + lastContainer.Bounds.Height)
            {
                return listBoxItems.Count;
            }
        }

        return -1;
    }

    public void OnSourceItemPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        // Find the source item that was clicked
        var stackPanel = sender as StackPanel;
        if (stackPanel?.DataContext is SourceItemViewModel sourceItem)
        {
            if (DataContext is CompositeComponentViewModel viewModel)
            {
                viewModel.SelectSource(sourceItem);
            }
        }
    }

    public void OnFilterItemPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        // Find the filter item that was clicked
        var stackPanel = sender as StackPanel;
        if (stackPanel?.DataContext is FilterItemViewModel filterItem)
        {
            if (DataContext is CompositeComponentViewModel viewModel)
            {
                viewModel.SelectFilter(filterItem);
            }
        }
    }

    private void OnSourceSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (sender is ComboBox comboBox &&
            DataContext is CompositeComponentViewModel viewModel)
        {
            if (comboBox.SelectedItem is GuiObjectFactory selectedSourceType)
            {
                viewModel.AddSources(selectedSourceType);
                // Clear the selection so the same item can be selected again
                comboBox.SelectedItem = null;
            }
        }
    }

    private void OnFilterSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (sender is ComboBox comboBox &&
            DataContext is CompositeComponentViewModel viewModel)
        {
            if (comboBox.SelectedItem is GuiObjectFactory selectedFilterType)
            {
                viewModel.AddFilters(selectedFilterType);
                // Clear the selection so the same item can be selected again
                comboBox.SelectedItem = null;
            }
        }
    }
}