using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace OpenBoardAnim.TimeLineTool
{
    //public class TimeLineItemControl:ContentPresenter
    public class TimeLineItemControl : Button
    {
        internal bool ReadyToDraw { get; set; } = true;

        public bool IsExpanded
        {
            get { return (Boolean)GetValue(IsExpandedProperty); }
            set { SetValue(IsExpandedProperty, value); }
        }

        // Using a DependencyProperty as the backing store for IsExpanded.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty IsExpandedProperty =
            DependencyProperty.Register("IsExpanded", typeof(Boolean), typeof(TimeLineItemControl), new UIPropertyMetadata(false));

        #region unitsize

        public Double UnitSize
        {
            get { return (Double)GetValue(UnitSizeProperty); }
            set { SetValue(UnitSizeProperty, value); }
        }

        // Using a DependencyProperty as the backing store for UnitSize.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty UnitSizeProperty =
            DependencyProperty.Register("UnitSize", typeof(Double), typeof(TimeLineItemControl),
            new UIPropertyMetadata(5.0,
                    new PropertyChangedCallback(OnUnitSizeChanged)));

        private static void OnUnitSizeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            TimeLineItemControl ctrl = d as TimeLineItemControl;
            if (ctrl != null)
            {
                ctrl.PlaceOnCanvas();
            }
        }

        #endregion unitsize

        #region ViewLevel

        public TimeLineViewLevel ViewLevel
        {
            get { return (TimeLineViewLevel)GetValue(ViewLevelProperty); }
            set { SetValue(ViewLevelProperty, value); }
        }

        // Using a DependencyProperty as the backing store for ViewLevel.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ViewLevelProperty =
            DependencyProperty.Register("ViewLevel", typeof(TimeLineViewLevel), typeof(TimeLineItemControl),
            new UIPropertyMetadata(TimeLineViewLevel.Hours,
                new PropertyChangedCallback(OnViewLevelChanged)));

        private static void OnViewLevelChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            TimeLineItemControl ctrl = d as TimeLineItemControl;
            if (ctrl != null)
            {
                ctrl.PlaceOnCanvas();
            }
        }

        #endregion ViewLevel

        #region timeline start time

        public DateTime TimeLineStartTime
        {
            get { return (DateTime)GetValue(TimeLineStartTimeProperty); }
            set { SetValue(TimeLineStartTimeProperty, value); }
        }

        // Using a DependencyProperty as the backing store for TimeLineStartTime.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty TimeLineStartTimeProperty =
            DependencyProperty.Register("TimeLineStartTime", typeof(DateTime), typeof(TimeLineItemControl),
            new UIPropertyMetadata(DateTime.MinValue,
                new PropertyChangedCallback(OnTimeValueChanged)));

        #endregion timeline start time

        #region start time

        public DateTime StartTime
        {
            get { return (DateTime)GetValue(StartTimeProperty); }
            set { SetValue(StartTimeProperty, value); }
        }

        // Using a DependencyProperty as the backing store for StartTime.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty StartTimeProperty =
            DependencyProperty.Register("StartTime", typeof(DateTime), typeof(TimeLineItemControl),
            new UIPropertyMetadata(DateTime.MinValue,
                new PropertyChangedCallback(OnTimeValueChanged)));

        #endregion start time

        #region end time

        public DateTime EndTime
        {
            get { return (DateTime)GetValue(EndTimeProperty); }
            set { SetValue(EndTimeProperty, value); }
        }

        // Using a DependencyProperty as the backing store for EndTime.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty EndTimeProperty =
            DependencyProperty.Register("EndTime", typeof(DateTime), typeof(TimeLineItemControl),
            new UIPropertyMetadata(DateTime.MinValue.AddMinutes(5),
                                    new PropertyChangedCallback(OnTimeValueChanged)));

        #endregion end time

        public Double EditBorderThreshold
        {
            get { return (Double)GetValue(EditBorderThresholdProperty); }
            set { SetValue(EditBorderThresholdProperty, value); }
        }

        // Using a DependencyProperty as the backing store for EditBorderThreshold.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty EditBorderThresholdProperty =
            DependencyProperty.Register("EditBorderThreshold", typeof(Double), typeof(TimeLineItemControl), new UIPropertyMetadata(4.0, new PropertyChangedCallback(OnEditThresholdChanged)));

        private static void OnEditThresholdChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            TimeLineItemControl ctrl = d as TimeLineItemControl;
        }

        private static void OnTimeValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            TimeLineItemControl ctrl = d as TimeLineItemControl;
            if (ctrl != null)
                ctrl.PlaceOnCanvas();
        }

        internal void PlaceOnCanvas()
        {
            var w = CalculateWidth();
            if (w > 0)
                Width = w;
            var p = CalculateLeftPosition();
            if (p >= 0)
            {
                Canvas.SetLeft(this, p);
            }
        }

        protected override void OnInitialized(EventArgs e)
        {
            base.OnInitialized(e);
        }

        private ContentPresenter _LeftIndicator;
        private ContentPresenter _RightIndicator;

        public override void OnApplyTemplate()
        {
            _LeftIndicator = Template.FindName("PART_LeftIndicator", this) as ContentPresenter;
            _RightIndicator = Template.FindName("PART_RightIndicator", this) as ContentPresenter;
            if (_LeftIndicator != null)
                _LeftIndicator.Visibility = System.Windows.Visibility.Collapsed;
            if (_RightIndicator != null)
                _RightIndicator.Visibility = System.Windows.Visibility.Collapsed;
            base.OnApplyTemplate();
        }

        internal Double CalculateWidth()
        {
            try
            {
                DateTime start = (DateTime)GetValue(StartTimeProperty);
                DateTime end = (DateTime)GetValue(EndTimeProperty);
                TimeSpan duration = end.Subtract(start);
                return ConvertTimeToDistance(duration);
            }
            catch (Exception)
            {
                return 0;
            }
        }

        internal Double CalculateLeftPosition()
        {
            DateTime start = (DateTime)GetValue(StartTimeProperty);
            DateTime timelinestart = (DateTime)GetValue(TimeLineStartTimeProperty);

            TimeSpan Duration = start.Subtract(timelinestart);
            return ConvertTimeToDistance(Duration);
        }

        #region conversion utilities

        private Double ConvertTimeToDistance(TimeSpan span)
        {
            TimeLineViewLevel lvl = (TimeLineViewLevel)GetValue(ViewLevelProperty);
            Double unitSize = (Double)GetValue(UnitSizeProperty);
            Double value = unitSize;
            switch (lvl)
            {
                case TimeLineViewLevel.Minutes:
                    value = span.TotalMinutes * unitSize;
                    break;

                case TimeLineViewLevel.Hours:
                    value = span.TotalSeconds * unitSize;
                    break;

                case TimeLineViewLevel.Days:
                    value = span.TotalDays * unitSize;
                    break;

                case TimeLineViewLevel.Weeks:
                    value = (span.TotalDays / 7.0) * unitSize;
                    break;

                case TimeLineViewLevel.Months:
                    value = (span.TotalDays / 30.0) * unitSize;
                    break;

                case TimeLineViewLevel.Years:
                    value = (span.TotalDays / 365.0) * unitSize;
                    break;

                default:
                    break;
            }
            return value;
        }

        private TimeSpan ConvertDistanceToTime(Double distance)
        {
            TimeLineViewLevel lvl = (TimeLineViewLevel)GetValue(ViewLevelProperty);
            Double unitSize = (Double)GetValue(UnitSizeProperty);
            double minutes, hours, days, weeks, months, years, milliseconds = 0;

            switch (lvl)
            {
                case TimeLineViewLevel.Minutes:
                    //value = span.TotalMinutes * unitSize;
                    minutes = (distance / unitSize);
                    //convert to milliseconds
                    milliseconds = minutes * 60000;
                    break;

                case TimeLineViewLevel.Hours:
                    hours = (distance / unitSize);
                    //convert to milliseconds
                    milliseconds = hours * 1000;
                    break;

                case TimeLineViewLevel.Days:
                    days = (distance / unitSize);
                    //convert to milliseconds
                    milliseconds = days * 24 * 60 * 60000;
                    break;

                case TimeLineViewLevel.Weeks:
                    //value = (span.TotalDays / 7.0) * unitSize;
                    weeks = (7 * distance / unitSize);
                    //convert to milliseconds
                    milliseconds = weeks * 7 * 24 * 60 * 60000;
                    break;

                case TimeLineViewLevel.Months:
                    months = (30 * distance / unitSize); ;
                    //convert to milliseconds
                    milliseconds = months * 30 * 24 * 60 * 60000;
                    break;

                case TimeLineViewLevel.Years:
                    years = (365 * distance / unitSize);
                    //convert to milliseconds
                    milliseconds = years * 365 * 24 * 60 * 60000;
                    break;

                default:
                    break;
            }
            long ticks = (long)milliseconds * 10000;
            TimeSpan returner = new TimeSpan(ticks);
            return returner;

            //return new TimeSpan(0, 0, 0, 0, (int)milliseconds);
        }

        #endregion conversion utilities

        private void SetIndicators(System.Windows.Visibility left, System.Windows.Visibility right)
        {
            if (_LeftIndicator != null)
            {
                _LeftIndicator.Visibility = left;
            }
            if (_RightIndicator != null)
            {
                _RightIndicator.Visibility = right;
            }
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            switch (GetClickAction())
            {
                case TimeLineAction.StretchStart:
                    SetIndicators(System.Windows.Visibility.Visible, System.Windows.Visibility.Collapsed);
                    break;

                case TimeLineAction.StretchEnd:
                    SetIndicators(System.Windows.Visibility.Collapsed, System.Windows.Visibility.Visible);
                    //this.Cursor = Cursors.SizeWE;//Cursors.Hand;//Cursors.ScrollWE;
                    break;

                default:
                    SetIndicators(System.Windows.Visibility.Collapsed, System.Windows.Visibility.Collapsed);
                    break;
            }
        }

        protected override void OnMouseLeave(MouseEventArgs e)
        {
            SetIndicators(System.Windows.Visibility.Collapsed, System.Windows.Visibility.Collapsed);
            if (IsExpanded && (e.LeftButton == MouseButtonState.Pressed || e.RightButton == MouseButtonState.Pressed))
            {
                return;
            }
            IsExpanded = false;
            base.OnMouseLeave(e);
        }

        #region manipulation tools

        internal TimeLineAction GetClickAction()
        {
            var X = Mouse.GetPosition(this).X;
            Double borderThreshold = (Double)GetValue(EditBorderThresholdProperty);// 4;
            Double unitsize = (Double)GetValue(UnitSizeProperty);

            if (X < borderThreshold)
                return TimeLineAction.StretchStart;
            if (X > Width - borderThreshold)
                return TimeLineAction.StretchEnd;
            return TimeLineAction.Move;
        }

        internal bool CanDelta(int StartOrEnd, Double deltaX)
        {
            Double unitS = (Double)GetValue(UnitSizeProperty);
            Double threshold = unitS / 3.0;
            Double newW = unitS;
            if (StartOrEnd == 0)//we are moving the start
            {
                if (deltaX < 0)
                    return true;
                //otherwises get what our new width would be
                newW = Width - deltaX;//delta is + but we are actually going to shrink our width by moving start +
                return newW > threshold;
            }
            else
            {
                if (deltaX > 0)
                    return true;
                newW = Width + deltaX;
                return newW > threshold;
            }
        }

        internal TimeSpan GetDeltaTime(Double deltaX)
        {
            return ConvertDistanceToTime(deltaX);
        }

        internal void GetPlacementInfo(ref Double left, ref Double width, ref Double end)
        {
            left = Canvas.GetLeft(this);
            width = Width;
            end = left + Width;
            //Somewhere on the process of removing a timeline control from the visual tree
            //it resets our start time to min value.  In that case it then results in ridiculous placement numbers
            //that this feeds to the control and crashes the whole app in a strange way.
            if (TimeLineStartTime == DateTime.MinValue)
            {
                left = 0;
                width = 1;
                end = 1;
            }
        }

        internal void MoveMe(Double deltaX)
        {
            var left = Canvas.GetLeft(this);
            left += deltaX;
            if (left < 0)
                left = 0;
            Canvas.SetLeft(this, left);

            TimeSpan startTs = ConvertDistanceToTime(left);
            DateTime tlStart = TimeLineStartTime;
            DateTime s = StartTime;
            DateTime e = EndTime;
            TimeSpan duration = e.Subtract(s);

            StartTime = tlStart.Add(startTs);
            EndTime = StartTime.Add(duration);
        }

        #endregion manipulation tools

        internal void MoveEndTime(double delta)
        {
            Width += delta;
            //calculate our new end time
            DateTime s = (DateTime)GetValue(StartTimeProperty);
            TimeSpan ts = ConvertDistanceToTime(Width);
            EndTime = s.Add(ts);
        }

        internal void MoveStartTime(double delta)
        {
            Double curLeft = Canvas.GetLeft(this);
            if (curLeft == 0 && delta < 0)
                return;
            curLeft += delta;
            Width = Width - delta;
            if (curLeft < 0)
            {
                //we need to
                Width -= curLeft;//We are moving back to 0 and have to fix our width to not bump a bit.
                curLeft = 0;
            }
            Canvas.SetLeft(this, curLeft);
            //recalculate start time;
            TimeSpan ts = ConvertDistanceToTime(curLeft);
            StartTime = TimeLineStartTime.Add(ts);
        }

        internal void MoveToNewStartTime(DateTime start)
        {
            DateTime s = (DateTime)GetValue(StartTimeProperty);
            DateTime e = (DateTime)GetValue(EndTimeProperty);
            TimeSpan duration = e.Subtract(s);
            StartTime = start;
            EndTime = start.Add(duration);
            PlaceOnCanvas();
        }

        /// <summary>
        /// Sets up with a default of 55 of our current units in size.
        /// </summary>
        internal void InitializeDefaultLength()
        {
            TimeSpan duration = ConvertDistanceToTime(10 * (Double)GetValue(UnitSizeProperty));
            EndTime = StartTime.Add(duration);
            Width = CalculateWidth();
        }
    }
}