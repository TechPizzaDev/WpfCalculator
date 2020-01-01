// Copyright (C) Josh Smith - January 2007
using System.Windows.Documents;
using System.Windows;
using System.Windows.Media;
using System.Windows.Shapes;

namespace FörstaLektion
{
    /// <summary>
    /// Renders a visual which can follow the mouse cursor, 
    /// such as during a drag-and-drop operation.
    /// </summary>
    public class DragAdorner : Adorner
    {
        private Rectangle _child;
        private double _offsetLeft;
        private double _offsetTop;

        #region Properties

        /// <summary>
        /// Gets/sets the horizontal offset of the adorner.
        /// </summary>
        public double OffsetLeft
        {
            get => _offsetLeft;
            set
            {
                _offsetLeft = value;
                UpdateLocation();
            }
        }

        /// <summary>
        /// Gets/sets the vertical offset of the adorner.
        /// </summary>
        public double OffsetTop
        {
            get => _offsetTop;
            set
            {
                _offsetTop = value;
                UpdateLocation();
            }
        }

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new instance of DragVisualAdorner.
        /// </summary>
        /// <param name="adornedElement">The element being adorned.</param>
        /// <param name="size">The size of the adorner.</param>
        /// <param name="brush">A brush to with which to paint the adorner.</param>
        public DragAdorner(UIElement adornedElement, Size size, Brush brush)
            : base(adornedElement)
        {
            _child = new Rectangle();
            _child.Fill = brush;
            _child.Width = size.Width;
            _child.Height = size.Height;
            _child.IsHitTestVisible = false;
        }

        #endregion

        public override GeneralTransform GetDesiredTransform(GeneralTransform transform)
        {
            var result = new GeneralTransformGroup();
            result.Children.Add(base.GetDesiredTransform(transform));
            result.Children.Add(new TranslateTransform(_offsetLeft, _offsetTop));
            return result;
        }

        /// <summary>
        /// Updates the location of the adorner.
        /// </summary>
        public void SetOffsets(double left, double top)
        {
            _offsetLeft = left;
            _offsetTop = top;
            UpdateLocation();
        }

        #region Protected Overrides

        protected override Size MeasureOverride(Size constraint)
        {
            _child.Measure(constraint);
            return _child.DesiredSize;
        }

        protected override Size ArrangeOverride(Size finalSize)
        {
            _child.Arrange(new Rect(finalSize));
            return finalSize;
        }

        protected override Visual GetVisualChild(int index) => _child;

        protected override int VisualChildrenCount => 1;

        #endregion

        private void UpdateLocation()
        {
            if (Parent is AdornerLayer adornerLayer)
                adornerLayer.Update(AdornedElement);
        }
    }
}