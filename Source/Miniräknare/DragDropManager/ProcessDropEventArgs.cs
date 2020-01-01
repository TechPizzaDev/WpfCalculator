// Copyright (C) Josh Smith - January 2007
using System;
using System.Collections.ObjectModel;
using System.Windows;

namespace FörstaLektion
{
    /// <summary>
    /// Event arguments used by the <see cref="ListViewDragDropManager.ProcessDrop"/> event.
    /// </summary>
    /// <typeparam name="TItem">The type of data object being dropped.</typeparam>
    public class ProcessDropEventArgs<TItem> : EventArgs where TItem : class
    {
        #region Public Properties

        /// <summary>
        /// The items source of the ListView where the drop occurred.
        /// </summary>
        public ObservableCollection<TItem> ItemsSource { get; }
        
        /// <summary>
        /// The data object which was dropped.
        /// </summary>
        public TItem DataItem { get; }

        /// <summary>
        /// The current index of the data item being dropped, in the ItemsSource collection.
        /// </summary>
        public int OldIndex { get; }

        /// <summary>
        /// The target index of the data item being dropped, in the ItemsSource collection.
        /// </summary>
        public int NewIndex { get; }

        /// <summary>
        /// The drag drop effects allowed to be performed.
        /// </summary>
        public DragDropEffects AllowedEffects { get; } = DragDropEffects.None;

        /// <summary>
        /// The drag drop effects performed on the dropped item.
        /// </summary>
        public DragDropEffects Effects { get; set; } = DragDropEffects.None;

        #endregion

        #region Constructor

        internal ProcessDropEventArgs(
            ObservableCollection<TItem> itemsSource,
            TItem dataItem,
            int oldIndex,
            int newIndex,
            DragDropEffects allowedEffects)
        {
            ItemsSource = itemsSource;
            DataItem = dataItem;
            OldIndex = oldIndex;
            NewIndex = newIndex;
            AllowedEffects = allowedEffects;
        }

        #endregion
    }
}
