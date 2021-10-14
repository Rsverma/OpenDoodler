using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;

namespace TimeLineTool
{
    internal class TimeLineDragAdorner : Adorner
    {
        private ContentPresenter _adorningContentPresenter;
        internal ITimeLineDataItem Data { get; set; }
        internal DataTemplate Template { get; set; }
        private Point _mousePosition;

        public Point MousePosition
        {
            get
            {
                return _mousePosition;
            }
            set
            {
                if (_mousePosition != value)
                {
                    _mousePosition = value;
                    _layer.Update(AdornedElement);
                }
            }
        }

        private AdornerLayer _layer;

        public TimeLineDragAdorner(TimeLineItemControl uiElement, DataTemplate template)
            : base(uiElement)
        {
            _adorningContentPresenter = new ContentPresenter();
            _adorningContentPresenter.Content = uiElement.DataContext;
            _adorningContentPresenter.ContentTemplate = template;
            _adorningContentPresenter.Opacity = 0.5;
            _layer = AdornerLayer.GetAdornerLayer(uiElement);

            _layer.Add(this);
            IsHitTestVisible = false;
        }

        public void Detach()
        {
            _layer.Remove(this);
        }

        protected override Visual GetVisualChild(int index)
        {
            return _adorningContentPresenter;
        }

        protected override Size MeasureOverride(Size constraint)
        {
            //_adorningContentPresenter.Measure(constraint);
            return new Size((AdornedElement as TimeLineItemControl).Width, (AdornedElement as TimeLineItemControl).DesiredSize.Height);//(_adorningContentPresenter.Width,_adorningContentPresenter.Height);
        }

        protected override int VisualChildrenCount
        {
            get
            {
                return 1;
            }
        }

        protected override Size ArrangeOverride(Size finalSize)
        {
            _adorningContentPresenter.Arrange(new Rect(finalSize));
            return finalSize;
        }

        public override GeneralTransform GetDesiredTransform(GeneralTransform transform)
        {
            GeneralTransformGroup result = new GeneralTransformGroup();
            result.Children.Add(base.GetDesiredTransform(transform));
            result.Children.Add(new TranslateTransform(MousePosition.X - 4, MousePosition.Y - 4));
            return result;
        }
    }
}