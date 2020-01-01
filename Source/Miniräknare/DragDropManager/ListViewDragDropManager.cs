// Copyright (C) Josh Smith - January 2007
using System;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Input;

namespace FörstaLektion
{
    /// <summary>
    /// Manages the dragging and dropping of ListViewItems in a ListView.
    /// The TItem type parameter indicates the type of the objects in
    /// the ListView's items source.  The ListView's ItemsSource must be 
    /// set to an instance of ObservableCollection of TItem, or an 
    /// Exception will be thrown.
    /// </summary>
    /// <typeparam name="TItem">The type of the ListView's items.</typeparam>
    public class ListViewDragDropManager<TItem> 
        where TItem : class
    {
        private bool isDragInProgress;
        private bool canInitiateDrag;
        private DragAdorner dragAdorner;
        private double dragAdornerOpacity;
        private int indexToSelect;
        private TItem itemUnderDragCursor;
        private ListView listView;
        private Point ptMouseDown;
        private bool showDragAdorner;

        #region Constructors

        /// <summary>
        /// Initializes a new instance of ListViewDragManager.
        /// </summary>
        public ListViewDragDropManager()
        {
            canInitiateDrag = false;
            dragAdornerOpacity = 0.7;
            indexToSelect = -1;
            showDragAdorner = true;
        }

        /// <summary>
        /// Initializes a new instance of ListViewDragManager.
        /// </summary>
        /// <param name="listView"></param>
        public ListViewDragDropManager(ListView listView) : this()
        {
            ListView = listView;
        }

        /// <summary>
        /// Initializes a new instance of ListViewDragManager.
        /// </summary>
        /// <param name="listView"></param>
        /// <param name="dragAdornerOpacity"></param>
        public ListViewDragDropManager(ListView listView, double dragAdornerOpacity) : this(listView)
        {
            DragAdornerOpacity = dragAdornerOpacity;
        }

        /// <summary>
        /// Initializes a new instance of ListViewDragManager.
        /// </summary>
        /// <param name="listView"></param>
        /// <param name="showDragAdorner"></param>
        public ListViewDragDropManager(ListView listView, bool showDragAdorner) : this(listView)
        {
            ShowDragAdorner = showDragAdorner;
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets/sets the opacity of the drag adorner.  This property has no
        /// effect if ShowDragAdorner is false. The default value is 0.7
        /// </summary>
        public double DragAdornerOpacity
        {
            get => dragAdornerOpacity;
            set
            {
                if (IsDragInProgress)
                    throw new InvalidOperationException("Cannot set the DragAdornerOpacity property during a drag operation.");

                if (value < 0.0 || value > 1.0)
                    throw new ArgumentOutOfRangeException("DragAdornerOpacity", value, "Must be between 0 and 1.");

                dragAdornerOpacity = value;
            }
        }

        /// <summary>
        /// Returns true if there is currently a drag operation being managed.
        /// </summary>
        public bool IsDragInProgress
        {
            get => isDragInProgress;
            private set
            {
                if(isDragInProgress != value)
                {
                    isDragInProgress = value;
                    (isDragInProgress ? OnDragStart : OnDragStop)?.Invoke(this, null);
                }
            }
        }

        /// <summary>
        /// Gets/sets the ListView whose dragging is managed.  This property
        /// can be set to null, to prevent drag management from occuring.  If
        /// the ListView's AllowDrop property is false, it will be set to true.
        /// </summary>
        public ListView ListView
        {
            get => listView;
            set
            {
                if (IsDragInProgress)
                    throw new InvalidOperationException("Cannot set the ListView property during a drag operation.");

                if (listView != null)
                {
                    #region Unhook Events

                    listView.PreviewMouseLeftButtonDown -= ListView_PreviewMouseLeftButtonDown;
                    listView.PreviewMouseMove -= ListView_PreviewMouseMove;
                    listView.DragOver -= ListView_DragOver;
                    listView.DragLeave -= ListView_DragLeave;
                    listView.DragEnter -= ListView_DragEnter;
                    listView.Drop -= ListView_Drop;

                    #endregion // Unhook Events
                }

                listView = value;

                if (listView != null)
                {
                    if (!listView.AllowDrop)
                        listView.AllowDrop = true;

                    #region Hook Events

                    listView.PreviewMouseLeftButtonDown += ListView_PreviewMouseLeftButtonDown;
                    listView.PreviewMouseMove += ListView_PreviewMouseMove;
                    listView.DragOver += ListView_DragOver;
                    listView.DragLeave += ListView_DragLeave;
                    listView.DragEnter += ListView_DragEnter;
                    listView.Drop += ListView_Drop;

                    #endregion // Hook Events
                }
            }
        }

        public event EventHandler<EventArgs> OnDragStart;

        public event EventHandler<EventArgs> OnDragStop;

        /// <summary>
        /// Raised when a drop occurs.  By default the dropped item will be moved
        /// to the target index.  Handle this event if relocating the dropped item
        /// requires custom behavior.  Note, if this event is handled the default
        /// item dropping logic will not occur.
        /// </summary>
        public event EventHandler<ProcessDropEventArgs<TItem>> ProcessDrop;

        /// <summary>
        /// Gets/sets whether a visual representation of the ListViewItem being dragged
        /// follows the mouse cursor during a drag operation.  The default value is true.
        /// </summary>
        public bool ShowDragAdorner
        {
            get => showDragAdorner;
            set
            {
                if (IsDragInProgress)
                    throw new InvalidOperationException("Cannot set the ShowDragAdorner property during a drag operation.");

                showDragAdorner = value;
            }
        }

        #endregion

        #region Event Handling Methods

        void ListView_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (IsMouseOverScrollbar)
            {
                // 4/13/2007 - Set the flag to false when cursor is over scrollbar.
                canInitiateDrag = false;
                return;
            }

            int index = IndexUnderDragCursor;
            canInitiateDrag = index > -1;

            if (canInitiateDrag)
            {
                // Remember the location and index of the ListViewItem the user clicked on for later.
                ptMouseDown = MouseUtilities.GetMousePosition(listView);
                indexToSelect = index;
            }
            else
            {
                ptMouseDown = new Point(-10000, -10000);
                indexToSelect = -1;
            }
        }

        void ListView_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            if (!CanStartDragOperation)
                return;

            // Select the item the user clicked on.
            if (listView.SelectedIndex != indexToSelect)
                listView.SelectedIndex = indexToSelect;

            // If the item at the selected index is null, there's nothing
            // we can do, so just return;
            if (listView.SelectedItem == null)
                return;

            ListViewItem itemToDrag = GetListViewItem(listView.SelectedIndex);
            if (itemToDrag == null)
                return;

            AdornerLayer adornerLayer = ShowDragAdornerResolved ? InitializeAdornerLayer(itemToDrag) : null;

            InitializeDragOperation(itemToDrag);
            PerformDragOperation();
            FinishDragOperation(itemToDrag, adornerLayer);
        }

        void ListView_DragOver(object sender, DragEventArgs e)
        {
            e.Effects = DragDropEffects.Move;

            if (ShowDragAdornerResolved)
                UpdateDragAdornerLocation();

            // Update the item which is known to be currently under the drag cursor.
            int index = IndexUnderDragCursor;
            ItemUnderDragCursor = index < 0 ? null : ListView.Items[index] as TItem;
        }

        void ListView_DragLeave(object sender, DragEventArgs e)
        {
            if (!IsMouseOver(listView))
            {
                if (ItemUnderDragCursor != null)
                    ItemUnderDragCursor = null;

                if (dragAdorner != null)
                    dragAdorner.Visibility = Visibility.Collapsed;
            }
        }

        void ListView_DragEnter(object sender, DragEventArgs e)
        {
            if (dragAdorner != null && dragAdorner.Visibility != Visibility.Visible)
            {
                // Update the location of the adorner and then show it.				
                UpdateDragAdornerLocation();
                dragAdorner.Visibility = Visibility.Visible;
            }
        }

        void ListView_Drop(object sender, DragEventArgs e)
        {
            if (ItemUnderDragCursor != null)
                ItemUnderDragCursor = null;

            e.Effects = DragDropEffects.None;

            if (!e.Data.GetDataPresent(typeof(TItem)))
                return;

            // Get the data object which was dropped.
            if (!(e.Data.GetData(typeof(TItem)) is TItem data))
                return;

            // Get the ObservableCollection<TItem> which contains the dropped data object.
            if (!(listView.ItemsSource is ObservableCollection<TItem> itemsSource))
                throw new Exception(
                    "A ListView managed by ListViewDragManager must have its " +
                    "ItemsSource set to an ObservableCollection<TItem>.");

            int oldIndex = itemsSource.IndexOf(data);
            int newIndex = IndexUnderDragCursor;

            if (newIndex < 0)
            {
                // The drag started somewhere else, and our ListView is empty
                // so make the new item the first in the list.
                if (itemsSource.Count == 0)
                    newIndex = 0;

                // The drag started somewhere else, but our ListView has items
                // so make the new item the last in the list.
                else if (oldIndex < 0)
                    newIndex = itemsSource.Count;

                // The user is trying to drop an item from our ListView into
                // our ListView, but the mouse is not over an item, so don't
                // let them drop it.
                else
                    return;
            }

            // Dropping an item back onto itself is not considered an actual 'drop'.
            if (oldIndex == newIndex)
                return;

            if (ProcessDrop != null)
            {
                // Let the client code process the drop.
                var args = new ProcessDropEventArgs<TItem>(
                    itemsSource, data, oldIndex, newIndex, e.AllowedEffects);
                ProcessDrop(this, args);
                e.Effects = args.Effects;
            }
            else
            {
                // Move the dragged data object from it's original index to the
                // new index (according to where the mouse cursor is).  If it was
                // not previously in the ListBox, then insert the item.
                if (oldIndex > -1)
                    itemsSource.Move(oldIndex, newIndex);
                else
                    itemsSource.Insert(newIndex, data);

                // Set the Effects property so that the call to DoDragDrop will return 'Move'.
                e.Effects = DragDropEffects.Move;
            }
        }

        #endregion

        #region Private Helpers

        bool CanStartDragOperation
        {
            get
            {
                if (Mouse.LeftButton != MouseButtonState.Pressed ||
                    !canInitiateDrag ||
                    indexToSelect == -1 ||
                    !HasCursorLeftDragThreshold)
                    return false;
                return true;
            }
        }

        void FinishDragOperation(ListViewItem draggedItem, AdornerLayer adornerLayer)
        {
            ListViewItemDragState.SetIsBeingDragged(draggedItem, false);
            IsDragInProgress = false;

            if (ItemUnderDragCursor != null)
                ItemUnderDragCursor = null;

            if (adornerLayer != null)
            {
                adornerLayer.Remove(dragAdorner);
                dragAdorner = null;
            }
        }

        ListViewItem GetListViewItem(int index)
        {
            if (listView.ItemContainerGenerator.Status != GeneratorStatus.ContainersGenerated)
                return null;
            return listView.ItemContainerGenerator.ContainerFromIndex(index) as ListViewItem;
        }

        ListViewItem GetListViewItem(TItem dataItem)
        {
            if (listView.ItemContainerGenerator.Status != GeneratorStatus.ContainersGenerated)
                return null;
            return listView.ItemContainerGenerator.ContainerFromItem(dataItem) as ListViewItem;
        }

        bool HasCursorLeftDragThreshold
        {
            get
            {
                if (indexToSelect < 0)
                    return false;

                ListViewItem item = GetListViewItem(indexToSelect);
                Rect bounds = VisualTreeHelper.GetDescendantBounds(item);
                Point ptInItem = listView.TranslatePoint(ptMouseDown, item);

                // In case the cursor is at the very top or bottom of the ListViewItem
                // we want to make the vertical threshold very small so that dragging
                // over an adjacent item does not select it.
                double topOffset = Math.Abs(ptInItem.Y);
                double btmOffset = Math.Abs(bounds.Height - ptInItem.Y);
                double vertOffset = Math.Min(topOffset, btmOffset);

                double width = SystemParameters.MinimumHorizontalDragDistance * 2;
                double height = Math.Min(SystemParameters.MinimumVerticalDragDistance, vertOffset) * 2;
                var szThreshold = new Size(width, height);

                var rect = new Rect(ptMouseDown, szThreshold);
                rect.Offset(szThreshold.Width / -2, szThreshold.Height / -2);
                Point ptInListView = MouseUtilities.GetMousePosition(listView);
                return !rect.Contains(ptInListView);
            }
        }

        /// <summary>
        /// Returns the index of the ListViewItem underneath the
        /// drag cursor, or -1 if the cursor is not over an item.
        /// </summary>
        int IndexUnderDragCursor
        {
            get
            {
                int index = -1;
                for (int i = 0; i < listView.Items.Count; ++i)
                {
                    ListViewItem item = GetListViewItem(i);
                    if (IsMouseOver(item))
                    {
                        index = i;
                        break;
                    }
                }
                return index;
            }
        }

        AdornerLayer InitializeAdornerLayer(ListViewItem itemToDrag)
        {
            // Create a brush which will paint the ListViewItem onto
            // a visual in the adorner layer.
            var brush = new VisualBrush(itemToDrag);

            // Create an element which displays the source item while it is dragged.
            dragAdorner = new DragAdorner(listView, itemToDrag.RenderSize, brush);

            // Set the drag adorner's opacity.		
            dragAdorner.Opacity = DragAdornerOpacity;

            var layer = AdornerLayer.GetAdornerLayer(listView);
            layer.Add(dragAdorner);

            // Save the location of the cursor when the left mouse button was pressed.
            ptMouseDown = MouseUtilities.GetMousePosition(listView);

            return layer;
        }

        void InitializeDragOperation(ListViewItem itemToDrag)
        {
            // Set some flags used during the drag operation.
            IsDragInProgress = true;
            canInitiateDrag = false;

            // Let the ListViewItem know that it is being dragged.
            ListViewItemDragState.SetIsBeingDragged(itemToDrag, true);
        }

        bool IsMouseOver(Visual target)
        {
            // We need to use MouseUtilities to figure out the cursor
            // coordinates because, during a drag-drop operation, the WPF
            // mechanisms for getting the coordinates behave strangely.

            Rect bounds = VisualTreeHelper.GetDescendantBounds(target);
            Point mousePos = MouseUtilities.GetMousePosition(target);
            return bounds.Contains(mousePos);
        }

        /// <summary>
        /// Returns true if the mouse cursor is over a scrollbar in the ListView.
        /// </summary>
        bool IsMouseOverScrollbar
        {
            get
            {
                Point ptMouse = MouseUtilities.GetMousePosition(listView);
                HitTestResult hitTest = VisualTreeHelper.HitTest(listView, ptMouse);
                if (hitTest == null)
                    return false;

                DependencyObject depObj = hitTest.VisualHit;
                while (depObj != null)
                {
                    if (depObj is ScrollBar)
                        return true;

                    // VisualTreeHelper works with objects of type Visual or Visual3D.
                    // If the current object is not derived from Visual or Visual3D,
                    // then use the LogicalTreeHelper to find the parent element.
                    if (depObj is Visual || depObj is System.Windows.Media.Media3D.Visual3D)
                        depObj = VisualTreeHelper.GetParent(depObj);
                    else
                        depObj = LogicalTreeHelper.GetParent(depObj);
                }

                return false;
            }
        }

        TItem ItemUnderDragCursor
        {
            get => itemUnderDragCursor;
            set
            {
                if (itemUnderDragCursor == value)
                    return;

                // The first pass handles the previous item under the cursor.
                // The second pass handles the new one.
                for (int i = 0; i < 2; ++i)
                {
                    if (i == 1)
                        itemUnderDragCursor = value;

                    if (itemUnderDragCursor != null)
                    {
                        ListViewItem listViewItem = GetListViewItem(itemUnderDragCursor);
                        if (listViewItem != null)
                            ListViewItemDragState.SetIsUnderDragCursor(listViewItem, i == 1);
                    }
                }
            }
        }

        void PerformDragOperation()
        {
            var selectedItem = listView.SelectedItem as TItem;
            var allowedEffects = DragDropEffects.Move | DragDropEffects.Move | DragDropEffects.Link;
            if (DragDrop.DoDragDrop(listView, selectedItem, allowedEffects) != DragDropEffects.None)
            {
                // The item was dropped into a new location,
                // so make it the new selected item.
                listView.SelectedItem = selectedItem;
            }
        }

        bool ShowDragAdornerResolved => ShowDragAdorner && DragAdornerOpacity > 0.0;

        void UpdateDragAdornerLocation()
        {
            if (dragAdorner == null)
                return;

            Point ptCursor = MouseUtilities.GetMousePosition(ListView);

            double left = ptCursor.X - ptMouseDown.X;

            ListViewItem itemBeingDragged = GetListViewItem(indexToSelect);
            Point itemLoc = itemBeingDragged.TranslatePoint(new Point(0, 0), ListView);
            double top = itemLoc.Y + ptCursor.Y - ptMouseDown.Y;

            dragAdorner.SetOffsets(left, top);
        }

        #endregion
    }
}