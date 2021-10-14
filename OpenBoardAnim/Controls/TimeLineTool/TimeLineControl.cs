using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;

namespace OpenBoardAnim.TimeLineTool
{
    public class TimeLineControl : Canvas
    {
        public static TimeSpan CalculateMinimumAllowedTimeSpan(double unitSize)
        {
            //minute = unitsize*pixels
            //desired minimum widh for these manipulations = 10 pixels
            int minPixels = 10;
            double hours = minPixels / unitSize;
            //convert to milliseconds
            long ticks = (long)(hours * 60 * 60000 * 10000);
            return new TimeSpan(ticks);
        }

        private Double _bumpThreshold = 1.5;
        private ScrollViewer _scrollViewer;
        private Canvas _gridCanvas;
        private static TimeLineDragAdorner _dragAdorner;

        private static TimeLineDragAdorner DragAdorner
        {
            get
            {
                return _dragAdorner;
            }
            set
            {
                if (_dragAdorner != null)
                    _dragAdorner.Detach();
                _dragAdorner = value;
            }
        }

        private Boolean _synchedWithSiblings = true;

        public Boolean SynchedWithSiblings
        {
            get
            {
                return _synchedWithSiblings;
            }
            set
            {
                _synchedWithSiblings = value;
            }
        }

        internal Boolean _isSynchInstigator = false;
        internal Double SynchWidth = 0;

        private Boolean _itemsInitialized = false;

        private Boolean _unitSizeInitialized = false;
        private Boolean _startDateInitialized = false;

        #region dependency properties

        public ITimeLineDataItem FocusOnItem
        {
            get { return (ITimeLineDataItem)GetValue(FocusOnItemProperty); }
            set { SetValue(FocusOnItemProperty, value); }
        }

        // Using a DependencyProperty as the backing store for FocusOnItem.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty FocusOnItemProperty =
            DependencyProperty.Register("FocusOnItem", typeof(ITimeLineDataItem), typeof(TimeLineControl), new UIPropertyMetadata(null, new PropertyChangedCallback(FocusItemChanged)));

        public static void FocusItemChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            TimeLineControl tc = d as TimeLineControl;
            if ((e.NewValue != null) && (tc != null))
            {
                tc.ScrollToItem(e.NewValue as ITimeLineDataItem);
            }
        }

        private void ScrollToItem(ITimeLineDataItem target)
        {
            Double tgtNewWidth = 0;
            Double maxUnitSize = 450;//28000;
            Double minUnitSize = 1;
            if (_scrollViewer != null)
            {
                for (int i = 1; i < Children.Count; i++)
                {
                    var ctrl = Children[i] as TimeLineItemControl;
                    if (ctrl != null && ctrl.DataContext == target)
                    {
                        Double curW = ctrl.Width;
                        if (curW < 5)
                        {
                            tgtNewWidth = 50;
                        }
                        else if (curW > _scrollViewer.ViewportWidth)
                        {
                            tgtNewWidth = _scrollViewer.ViewportWidth / 3;
                        }

                        if (tgtNewWidth != 0)
                        {
                            Double newUnitSize = (UnitSize * tgtNewWidth) / curW;
                            if (newUnitSize > maxUnitSize)
                                newUnitSize = maxUnitSize;
                            else if (newUnitSize < minUnitSize)
                                newUnitSize = minUnitSize;
                            UnitSize = newUnitSize;
                            SynchronizeSiblings();
                        }
                        ctrl.BringIntoView();
                        return;
                    }
                }
            }
        }

        #region manager

        public ITimeLineManager Manager
        {
            get { return (ITimeLineManager)GetValue(ManagerProperty); }
            set { SetValue(ManagerProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Manager.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ManagerProperty =
            DependencyProperty.Register("Manager", typeof(ITimeLineManager), typeof(TimeLineControl),
            new UIPropertyMetadata(null));

        #endregion manager

        #region minwidth

        public new double MinWidth
        {
            get => (double)GetValue(MinWidthProperty);
            set => SetValue(MinWidthProperty, value);
        }

        // Using a DependencyProperty as the backing store for MinWidth.  This enables animation, styling, binding, etc...
        public static new readonly DependencyProperty MinWidthProperty =
            DependencyProperty.Register("MinWidth", typeof(Double), typeof(TimeLineControl), new UIPropertyMetadata(0.0));

        #endregion minwidth

        #region minheight

        public new double MinHeight
        {
            get => (double)GetValue(MinHeightProperty);
            set => SetValue(MinHeightProperty, value);
        }

        // Using a DependencyProperty as the backing store for MinHeight.  This enables animation, styling, binding, etc...
        public static new readonly DependencyProperty MinHeightProperty =
            DependencyProperty.Register("MinHeight", typeof(double), typeof(TimeLineControl), new UIPropertyMetadata(0.0));

        #endregion minheight

        #region background and grid dependency properties

        #region minimum unit width

        public Double MinimumUnitWidth
        {
            get { return (Double)GetValue(MinimumUnitWidthProperty); }
            set { SetValue(MinimumUnitWidthProperty, value); }
        }

        // Using a DependencyProperty as the backing store for MinimumUnitWidth.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty MinimumUnitWidthProperty =
            DependencyProperty.Register("MinimumUnitWidth", typeof(Double), typeof(TimeLineControl),
                new UIPropertyMetadata(10.0,
                    new PropertyChangedCallback(OnBackgroundValueChanged)));

        #endregion minimum unit width

        #region snap to grid

        public Boolean SnapToGrid
        {
            get { return (Boolean)GetValue(SnapToGridProperty); }
            set { SetValue(SnapToGridProperty, value); }
        }

        // Using a DependencyProperty as the backing store for SnapToGrid.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty SnapToGridProperty =
            DependencyProperty.Register("SnapToGrid", typeof(Boolean), typeof(TimeLineControl),
                new UIPropertyMetadata(null));

        //new UIPropertyMetadata(false,
        //new PropertyChangedCallback(OnBackgroundValueChanged)));

        #endregion snap to grid

        #region draw time grid

        public Boolean DrawTimeGrid
        {
            get { return (Boolean)GetValue(DrawTimeGridProperty); }
            set { SetValue(DrawTimeGridProperty, value); }
        }

        // Using a DependencyProperty as the backing store for DrawTimeGrid.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty DrawTimeGridProperty =
            DependencyProperty.Register("DrawTimeGrid", typeof(Boolean), typeof(TimeLineControl),
                new UIPropertyMetadata(false,
                    new PropertyChangedCallback(OnDrawTimeGridChanged)));

        #endregion draw time grid

        #region minor unit thickness

        public int MinorUnitThickness
        {
            get { return (int)GetValue(MinorUnitThicknessProperty); }
            set { SetValue(MinorUnitThicknessProperty, value); }
        }

        // Using a DependencyProperty as the backing store for MinorUnitThickness.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty MinorUnitThicknessProperty =
            DependencyProperty.Register("MinorUnitThickness", typeof(int), typeof(TimeLineControl),
                        new UIPropertyMetadata(1,
                            new PropertyChangedCallback(OnBackgroundValueChanged)));

        #endregion minor unit thickness

        #region major unit thickness

        public int MajorUnitThickness
        {
            get { return (int)GetValue(MajorUnitThicknessProperty); }
            set { SetValue(MajorUnitThicknessProperty, value); }
        }

        // Using a DependencyProperty as the backing store for MajorUnitThickness.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty MajorUnitThicknessProperty =
            DependencyProperty.Register("MajorUnitThickness", typeof(int), typeof(TimeLineControl),
                new UIPropertyMetadata(3, new PropertyChangedCallback(OnBackgroundValueChanged)));

        #endregion major unit thickness

        private static byte _defC = 80;

        #region day line brush

        public Brush DayLineBrush
        {
            get { return (Brush)GetValue(DayLineBrushProperty); }
            set { SetValue(DayLineBrushProperty, value); }
        }

        // Using a DependencyProperty as the backing store for DayLineBrush.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty DayLineBrushProperty =
            DependencyProperty.Register("DayLineBrush", typeof(Brush), typeof(TimeLineControl),
                new UIPropertyMetadata(new SolidColorBrush(new Color() { R = _defC, G = _defC, B = _defC, A = 255 }),
                    new PropertyChangedCallback(OnBackgroundValueChanged)));

        #endregion day line brush

        #region hour line brush

        public Brush HourLineBrush
        {
            get { return (Brush)GetValue(HourLineBrushProperty); }
            set { SetValue(HourLineBrushProperty, value); }
        }

        // Using a DependencyProperty as the backing store for HourLineBrush.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty HourLineBrushProperty =
            DependencyProperty.Register("HourLineBrush", typeof(Brush), typeof(TimeLineControl),
            new UIPropertyMetadata(new SolidColorBrush(new Color() { R = _defC, G = _defC, B = _defC, A = 255 / 2 }),
                new PropertyChangedCallback(OnBackgroundValueChanged)));

        #endregion hour line brush

        #region minute line brush

        public Brush MinuteLineBrush
        {
            get { return (Brush)GetValue(MinuteLineBrushProperty); }
            set { SetValue(MinuteLineBrushProperty, value); }
        }

        // Using a DependencyProperty as the backing store for MinuteLineBrush.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty MinuteLineBrushProperty =
            DependencyProperty.Register("MinuteLineBrush", typeof(Brush), typeof(TimeLineControl),
            new UIPropertyMetadata(new SolidColorBrush(new Color() { R = _defC, G = _defC, B = _defC, A = 255 / 3 }),
                new PropertyChangedCallback(OnBackgroundValueChanged)));

        #endregion minute line brush

        private static void OnDrawTimeGridChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            TimeLineControl tc = d as TimeLineControl;
            if (tc != null)
            {
                tc.DrawBackGround(true);
            }
        }

        private static void OnBackgroundValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            TimeLineControl tc = d as TimeLineControl;
            if (tc != null)
            {
                tc.DrawBackGround();
            }
        }

        #endregion background and grid dependency properties

        #region item template

        private DataTemplate _template;

        public DataTemplate ItemTemplate
        {
            get { return (DataTemplate)GetValue(ItemTemplateProperty); }
            set { SetValue(ItemTemplateProperty, value); }
        }

        // Using a DependencyProperty as the backing store for ItemTemplate.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ItemTemplateProperty =
            DependencyProperty.Register("ItemTemplate", typeof(DataTemplate), typeof(TimeLineControl),
            new UIPropertyMetadata(null,
                new PropertyChangedCallback(OnItemTemplateChanged)));

        private static void OnItemTemplateChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            TimeLineControl tc = d as TimeLineControl;
            if (tc != null)
            {
                tc.SetTemplate(e.NewValue as DataTemplate);
            }
        }

        #endregion item template

        #region Items

        public ObservableCollection<ITimeLineDataItem> Items
        {
            get { return (ObservableCollection<ITimeLineDataItem>)GetValue(ItemsProperty); }
            set { SetValue(ItemsProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Items.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ItemsProperty =
            DependencyProperty.Register("Items", typeof(ObservableCollection<ITimeLineDataItem>), typeof(TimeLineControl),
            new UIPropertyMetadata(null,
                new PropertyChangedCallback(OnItemsChanged)));

        private static void OnItemsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            TimeLineControl tc = d as TimeLineControl;
            if (tc != null)
            {
                tc.InitializeItems(e.NewValue as ObservableCollection<ITimeLineDataItem>);
                tc.UpdateUnitSize(tc.UnitSize);
                tc._itemsInitialized = true;

                tc.DrawBackGround();
            }
        }

        #endregion Items

        #region ViewLevel

        public TimeLineViewLevel ViewLevel
        {
            get { return (TimeLineViewLevel)GetValue(ViewLevelProperty); }
            set { SetValue(ViewLevelProperty, value); }
        }

        // Using a DependencyProperty as the backing store for ViewLevel.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ViewLevelProperty =
            DependencyProperty.Register("ViewLevel", typeof(TimeLineViewLevel), typeof(TimeLineControl),
            new UIPropertyMetadata(TimeLineViewLevel.Hours,
                new PropertyChangedCallback(OnViewLevelChanged)));

        private static void OnViewLevelChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            TimeLineControl tc = d as TimeLineControl;
            if (tc != null)
            {
                tc.UpdateViewLevel((TimeLineViewLevel)e.NewValue);
            }
        }

        #endregion ViewLevel

        #region unitsize

        public Double UnitSize
        {
            get { return (Double)GetValue(UnitSizeProperty); }
            set { SetValue(UnitSizeProperty, value); }
        }

        // Using a DependencyProperty as the backing store for UnitSize.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty UnitSizeProperty =
            DependencyProperty.Register("UnitSize", typeof(Double), typeof(TimeLineControl),
            new UIPropertyMetadata(5.0,
                new PropertyChangedCallback(OnUnitSizeChanged)));

        private static void OnUnitSizeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            TimeLineControl tc = d as TimeLineControl;
            if (tc != null)
            {
                tc._unitSizeInitialized = true;
                tc.UpdateUnitSize((Double)e.NewValue);
            }
        }

        #endregion unitsize

        #region start date

        public DateTime StartDate
        {
            get { return (DateTime)GetValue(StartDateProperty); }
            set { SetValue(StartDateProperty, value); }
        }

        // Using a DependencyProperty as the backing store for StartDate.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty StartDateProperty =
            DependencyProperty.Register("StartDate", typeof(DateTime), typeof(TimeLineControl),
            new UIPropertyMetadata(DateTime.MinValue,
                new PropertyChangedCallback(OnStartDateChanged)));

        private static void OnStartDateChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            TimeLineControl tc = d as TimeLineControl;
            if (tc != null)
            {
                tc._startDateInitialized = true;
                tc.ReDrawChildren();
            }
        }

        #endregion start date

        #region manipulation mode

        public TimeLineManipulationMode ManipulationMode
        {
            get { return (TimeLineManipulationMode)GetValue(ManipulationModeProperty); }
            set { SetValue(ManipulationModeProperty, value); }
        }

        // Using a DependencyProperty as the backing store for ManipulationMode.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ManipulationModeProperty =
            DependencyProperty.Register("ManipulationMode", typeof(TimeLineManipulationMode), typeof(TimeLineControl), new UIPropertyMetadata(TimeLineManipulationMode.Free));

        #endregion manipulation mode

        #endregion dependency properties

        public TimeLineControl()
        {
            _gridCanvas = new Canvas();
            Children.Add(_gridCanvas);
            Focusable = true;
            KeyDown += OnKeyDown;
            KeyUp += OnKeyUp;
            MouseEnter += TimeLineControl_MouseEnter;
            MouseLeave += TimeLineControl_MouseLeave;
            //Items = new ObservableCollection<ITimeLineDataItem>();

            DragDrop.AddDragOverHandler(this, TimeLineControl_DragOver);
            DragDrop.AddDropHandler(this, TimeLineControl_Drop);
            DragDrop.AddDragEnterHandler(this, TimeLineControl_DragOver);
            DragDrop.AddDragLeaveHandler(this, TimeLineControL_DragLeave);

            AllowDrop = true;

            _scrollViewer = GetParentScrollViewer();
        }

        #region control life cycle events

        protected override void OnRender(DrawingContext dc)
        {
            base.OnRender(dc);
            _scrollViewer = GetParentScrollViewer();
        }

        /*
        /// <summary>
        /// I was unable to track down why this control was locking up when
        /// synchronise with siblings is checked and the parent element is closed etc.
        /// I was getting something with a contextswitchdeadblock that I was wracking my
        /// brain trying to figure out.  The problem only happened when a timeline control
        /// with a child timeline item was present.  I could have n empty timeline controls
        /// with no problem.  Adding one timeline item however caused that error when the parent element
        /// is closed etc.
        /// </summary>
        /// <param name="child"></param>
        protected override void ParentLayoutInvalidated(UIElement child)
        {
            //this event fires when something drags over this or when the control is trying to close
            if (child == _tmpDraggAdornerControl)
                return;
            if (!Children.Contains(child))
                return;
            base.ParentLayoutInvalidated(child);
            SynchedWithSiblings = false;
            //Because this layout invalidated became neccessary, I had to then put null checks on all attempts
            //to get a timeline item control.  There appears to be some UI threading going on so that just checking the children count
            //at the begining of the offending methods was not preventing me from crashing.
            Children.Clear();
        }*/

        #endregion control life cycle events

        #region miscellaneous helpers

        private ScrollViewer GetParentScrollViewer()
        {
            DependencyObject item = VisualTreeHelper.GetParent(this);
            while (item != null)
            {
                String name = "";
                var ctrl = item as Control;
                if (ctrl != null)
                    name = ctrl.Name;
                if (item is ScrollViewer)
                {
                    return item as ScrollViewer;
                }
                item = VisualTreeHelper.GetParent(item);
            }
            return null;
        }

        private void SetTemplate(DataTemplate dataTemplate)
        {
            _template = dataTemplate;
            for (int i = 0; i < Children.Count; i++)
            {
                TimeLineItemControl titem = Children[i] as TimeLineItemControl;
                if (titem != null)
                    titem.ContentTemplate = dataTemplate;
            }
        }

        private void InitializeItems(ObservableCollection<ITimeLineDataItem> observableCollection)
        {
            if (observableCollection == null)
                return;
            this.Children.Clear();
            Children.Add(_gridCanvas);

            foreach (ITimeLineDataItem data in observableCollection)
            {
                TimeLineItemControl adder = CreateTimeLineItemControl(data);

                Children.Add(adder);
            }
            Items.CollectionChanged -= Items_CollectionChanged;
            Items.CollectionChanged += Items_CollectionChanged;
        }

        private void Items_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Add)
            {
                var itm = e.NewItems[0] as ITimeLineDataItem;
                if (itm.StartTime.HasValue && itm.StartTime.Value == DateTime.MinValue)
                {//newly created item isn't a drop in so we need to instantiate and place its control.
                    TimeSpan duration = itm.EndTime.Value.Subtract(itm.StartTime.Value);
                    if (Items.Count == 1)//this is the first one added
                    {
                        itm.StartTime = StartDate;
                        itm.EndTime = StartDate.Add(duration);
                    }
                    else
                    {
                        var last = Items.OrderBy(i => i.StartTime.Value).LastOrDefault();
                        if (last != null)
                        {
                            itm.StartTime = last.EndTime;
                            itm.EndTime = itm.StartTime.Value.Add(duration);
                        }
                    }
                    var ctrl = CreateTimeLineItemControl(itm);
                    //The index if Items.Count-1 because of zero indexing.
                    //however our children is 1 indexed because 0 is our canvas grid.
                    Children.Insert(Items.Count, ctrl);
                }
            }
            if (e.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Remove)
            {
                var removeItem = e.OldItems[0];
                for (int i = 1; i < Children.Count; i++)
                {
                    TimeLineItemControl checker = Children[i] as TimeLineItemControl;
                    if (checker != null && checker.DataContext == removeItem)
                    {
                        Children.Remove(checker);
                        break;
                    }
                }
            }
        }

        private TimeLineItemControl CreateTimeLineItemControl(ITimeLineDataItem data)
        {
            Binding startBinding = new Binding("StartTime");
            startBinding.Mode = BindingMode.TwoWay;
            startBinding.UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged;
            Binding endBinding = new Binding("EndTime");
            endBinding.Mode = BindingMode.TwoWay;
            endBinding.UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged;
            DateTime timelineStart = StartDate;

            Binding expandedBinding = new Binding("TimelineViewExpanded");
            expandedBinding.Mode = BindingMode.TwoWay;
            endBinding.UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged;

            TimeLineItemControl adder = new TimeLineItemControl();
            adder.TimeLineStartTime = timelineStart;
            adder.DataContext = data;
            adder.Content = data;

            adder.SetBinding(TimeLineItemControl.StartTimeProperty, startBinding);
            adder.SetBinding(TimeLineItemControl.EndTimeProperty, endBinding);
            adder.SetBinding(TimeLineItemControl.IsExpandedProperty, expandedBinding);

            if (_template != null)
            {
                adder.ContentTemplate = _template;
            }

            /*adder.PreviewMouseLeftButtonDown += item_PreviewEditButtonDown;
            adder.MouseMove += item_MouseMove;
            adder.PreviewMouseLeftButtonUp += item_PreviewEditButtonUp;*/
            adder.PreviewMouseRightButtonDown += item_PreviewEditButtonDown;
            adder.MouseMove += item_MouseMove;
            adder.PreviewMouseRightButtonUp += item_PreviewEditButtonUp;

            adder.PreviewMouseLeftButtonUp += item_PreviewDragButtonUp;
            adder.PreviewMouseLeftButtonDown += item_PreviewDragButtonDown;
            adder.UnitSize = UnitSize;
            return adder;
        }

        #endregion miscellaneous helpers

        #region updaters fired on dp changes

        private void UpdateUnitSize(double size)
        {
            if (Items == null)
                return;
            for (int i = 0; i < Items.Count; i++)
            {
                TimeLineItemControl titem = GetTimeLineItemControlAt(i);
                if (titem != null)
                    titem.UnitSize = size;
            }
            ReDrawChildren();
        }

        private void UpdateViewLevel(TimeLineViewLevel lvl)
        {
            if (Items == null)
                return;
            for (int i = 0; i < Items.Count; i++)
            {
                var templatedControl = GetTimeLineItemControlAt(i);
                if (templatedControl != null)
                    templatedControl.ViewLevel = lvl;
            }
            ReDrawChildren();
            //Now we go back and have to detect if things have been collapsed
        }

        //TODO: set up the timeline start date dependency property and do this margin check
        //for all including the first one.
        private void ReDrawChildren()
        {
            if (Items == null)
            {
                DrawBackGround();
                return;
            }
            DateTime start = (DateTime)GetValue(StartDateProperty);
            Double w = 0;
            Double s = 0;
            Double e = 0;
            for (int i = 0; i < Items.Count; i++)
            {
                var mover = GetTimeLineItemControlAt(i);
                if (mover != null)
                {
                    mover.TimeLineStartTime = start;
                    if (!mover.ReadyToDraw)
                        mover.ReadyToDraw = true;
                    mover.PlaceOnCanvas();
                    mover.GetPlacementInfo(ref s, ref w, ref e);
                }
            }
            //find our background rectangle and set its width;
            DrawBackGround();
        }

        #endregion updaters fired on dp changes

        #region background and grid methods

        private void DrawBackGround(Boolean isDrawGridUpdate = false)
        {
            Brush b = Background;
            double setWidth = MinWidth;
            if (_gridCanvas.Children.Count <= 0)
            {
                _gridCanvas.Children.Add(new Rectangle());
            }
            Rectangle bg = _gridCanvas.Children[0] as Rectangle;
            if (!_startDateInitialized ||
                !_unitSizeInitialized ||
                !_itemsInitialized ||
                Items == null)
            {
                setWidth = Math.Max(MinWidth, GetMyWidth());
                setWidth = Math.Max(setWidth, SynchWidth);
                bg.Width = setWidth;
                bg.Height = Math.Max(DesiredSize.Height, Height);
                if (Double.IsNaN(bg.Height) || bg.Height < MinHeight)
                {
                    bg.Height = MinHeight;
                }
                bg.Fill = b;
                Width = bg.Width;
                Height = bg.Height;
            }
            else
            {
                var oldW = Width;
                var oldDrawTimeGrid = DrawTimeGrid;
                if (isDrawGridUpdate)
                    oldDrawTimeGrid = !oldDrawTimeGrid;
                //this is run every time we may need to update our siblings.
                SynchronizeSiblings();

                if (Items == null)
                    return;
                setWidth = Math.Max(MinWidth, GetMyWidth());
                setWidth = Math.Max(setWidth, SynchWidth);
                bg.Width = setWidth;
                bg.Height = Math.Max(DesiredSize.Height, Height);
                if (Double.IsNaN(bg.Height) || bg.Height < MinHeight)
                {
                    bg.Height = MinHeight;
                }
                bg.Fill = b;
                Width = bg.Width;
                Height = bg.Height;
                if (DrawTimeGrid)
                {
                    if (Width != oldW || !oldDrawTimeGrid || (Width == MinWidth))
                        DrawTimeGridExecute();
                }
                else
                {
                    ClearTimeGridExecute();
                }
                if ((oldW != Width) && (_scrollViewer != null))//if we are at min width then we need to redraw our time grid when unit sizes change
                {
                    var available = LayoutInformation.GetLayoutSlot(_scrollViewer);
                    Size s = new Size(available.Width, available.Height);
                    _scrollViewer.Measure(s);
                    _scrollViewer.Arrange(available);
                }
            }
        }

        internal Double GetMyWidth()
        {
            if (Items == null)
            {
                return MinWidth;
            }
            var lastItem = GetTimeLineItemControlAt(Items.Count - 1);

            if (lastItem == null)
                return MinWidth;
            Double l = 0;
            Double w = 0;
            Double e = 0;
            lastItem.GetPlacementInfo(ref l, ref w, ref e);
            return Math.Max(MinWidth, e);
        }

        private void SynchronizeSiblings()
        {
            if (!SynchedWithSiblings)
                return;
            var current = VisualTreeHelper.GetParent(this) as FrameworkElement;

            while (current != null && !(current is ItemsControl))
            {
                current = VisualTreeHelper.GetParent(current) as FrameworkElement;
            }

            if (current is ItemsControl)
            {
                var pnl = current as ItemsControl;
                //this is called on updates for all siblings so it could easily
                //end up infinitely looping if each time tried to synch its siblings
                Boolean isSynchInProgress = false;
                //is there a synch instigator
                Double maxWidth = GetMyWidth();

                var siblings = TimeLineControl.FindAllTimeLineControlsInsidePanel(current);

                foreach (var ctrl in siblings)
                {
                    var tcSib = ctrl as TimeLineControl;
                    if (tcSib != null)
                    {
                        if (tcSib._isSynchInstigator)
                            isSynchInProgress = true;
                        Double sibW = tcSib.GetMyWidth();
                        if (sibW > maxWidth)
                        {
                            maxWidth = sibW;
                        }
                    }
                }
                SynchWidth = maxWidth;
                if (!isSynchInProgress)
                {
                    _isSynchInstigator = true;
                    foreach (var ctrl in siblings)
                    {
                        var tcSib = ctrl as TimeLineControl;
                        if (tcSib != null && tcSib != this)
                        {
                            tcSib.SynchWidth = maxWidth;
                            //tcSib.UnitSize = UnitSize;
                            //tcSib.StartDate = StartDate;
                            tcSib.DrawBackGround();
                        }
                    }
                }
                _isSynchInstigator = false;
            }
        }

        //helper to let a panel find all children of a given type
        private static IEnumerable<TimeLineControl> FindAllTimeLineControlsInsidePanel(DependencyObject depObj)
        {
            if (depObj != null)
            {
                for (int i = 0; i < VisualTreeHelper.GetChildrenCount(depObj); i++)
                {
                    DependencyObject child = VisualTreeHelper.GetChild(depObj, i);
                    if (child != null && child is TimeLineControl)
                    {
                        yield return (TimeLineControl)child;
                    }

                    foreach (TimeLineControl childOfChild in FindAllTimeLineControlsInsidePanel(child))
                    {
                        yield return childOfChild;
                    }
                }
            }
        }

        private void ClearTimeGridExecute()
        {
            if (_gridCanvas.Children.Count == 2)
                _gridCanvas.Children.RemoveAt(1);
        }

        private void DrawTimeGridExecute()
        {
            if (Items == null)
                return;
            if (StartDate == DateTime.MinValue)
                return;
            if (_gridCanvas.Children.Count < 2)
            {
                _gridCanvas.Children.Add(new Canvas());
            }
            Canvas grid = _gridCanvas.Children[1] as Canvas;
            grid.Children.Clear();
            Double hourSize = UnitSize;

            //place our gridlines
            DrawDayLines(grid);
            DrawHourLines(grid);
            DrawMinuteLines(grid);
        }

        private void DrawMinuteLines(Canvas grid)
        {
            Double halfHourSize = UnitSize / 2;
            Double fifteenMinSize = UnitSize / 4;
            Double minuteSize = UnitSize / 60;
            int startMinute = StartDate.Minute;
            int startSecond = StartDate.Second;
            int remainingMinutes = 60 - startMinute;
            int remainingSeconds = 60 - startSecond;
            if (remainingSeconds == 60)
                remainingSeconds = 0;

            if (fifteenMinSize >= MinimumUnitWidth)
            {
                if (startMinute < 45)
                    remainingMinutes = 45 - startMinute;
                if (startMinute < 30)
                    remainingMinutes = 30 - startMinute;
                if (startMinute < 15)
                    remainingMinutes = 15 - startMinute;
                if (startSecond != 0)
                    remainingMinutes--;
                else remainingMinutes = 0;

                TimeSpan nextFifteenGap = new TimeSpan(0, remainingMinutes, remainingSeconds);
                DateTime nextFifteenDate = StartDate.Add(nextFifteenGap);
                Double nextFifteenDistance = nextFifteenGap.TotalHours * UnitSize;
                DrawIncrementLines(grid, nextFifteenDate, nextFifteenDistance, new TimeSpan(0, 15, 0), fifteenMinSize, MinuteLineBrush, 0);
            }
            else if (halfHourSize >= MinimumUnitWidth)
            {
                if (startMinute < 30)
                    remainingMinutes = 30 - startMinute;
                if (startSecond != 0)
                    remainingMinutes--;
                TimeSpan nextHalfGap = new TimeSpan(0, remainingMinutes, remainingSeconds);
                DateTime nextHalfDate = StartDate.Add(nextHalfGap);
                Double nextHalfDistance = nextHalfGap.TotalHours * UnitSize;
                DrawIncrementLines(grid, nextHalfDate, nextHalfDistance, new TimeSpan(0, 30, 0), halfHourSize, MinuteLineBrush, 0);
            }
        }

        private void DrawHourLines(Canvas grid)
        {
            Double hourSize = UnitSize;
            Double halfDaySize = hourSize * 12;
            int startMinute = StartDate.Minute;
            int remainingMinutes = 60 - startMinute;
            int startSecond = StartDate.Second;
            int remainingSeconds = 60 - startSecond;
            if (remainingSeconds == 60)
                remainingSeconds = 0;
            if (startSecond != 0)
                remainingMinutes--;
            if (startSecond != 0)
                remainingMinutes--;
            else remainingMinutes = 0;

            if (hourSize >= MinimumUnitWidth)
            {
                int remainingToMajor = 24 - StartDate.Hour;
                if (StartDate.Hour < 12)
                    remainingToMajor = 12 - StartDate.Hour;
                //time to our next hour
                TimeSpan firstHourGap = new TimeSpan(0, remainingMinutes, remainingSeconds);
                DateTime nextHour = StartDate.Add(firstHourGap);
                Double firstHourDistance = firstHourGap.TotalHours * hourSize;
                DrawIncrementLines(grid, nextHour, firstHourDistance,
                                    new TimeSpan(1, 0, 0), hourSize, HourLineBrush, 12, remainingToMajor);
            }
            else if (halfDaySize >= MinimumUnitWidth)
            {
                int startHour = StartDate.Hour;
                int remainingHours = 24 - startHour;
                if (startHour < 12)
                {
                    remainingHours = 12 - startHour;
                }
                if (startMinute != 0)
                    remainingHours--;

                TimeSpan nextHalfGap = new TimeSpan(remainingHours, remainingMinutes, remainingSeconds);
                DateTime nextHalfDay = StartDate.Add(nextHalfGap);
                Double nextHalfDistance = nextHalfGap.TotalHours * hourSize;
                DrawIncrementLines(grid, nextHalfDay, nextHalfDistance, new TimeSpan(12, 0, 0), halfDaySize, HourLineBrush, -1);
            }
        }

        private void DrawDayLines(Canvas grid)
        {
            Double daySize = UnitSize * 24;

            if (daySize >= MinimumUnitWidth)
            {
                TimeSpan increment = new TimeSpan(24, 0, 0);
                int startHour = StartDate.Hour;
                int startMinute = StartDate.Minute;
                int remainingHours = 24 - startHour;
                if (startMinute > 0)
                    remainingHours--;
                int remainingMinutes = 60 - startMinute;
                if (startMinute == 0)
                    remainingMinutes = 0;
                int startSecond = StartDate.Second;
                int remainingSeconds = 60 - startSecond;
                if (startSecond != 0)
                    remainingMinutes--;
                else
                    remainingSeconds = 0;

                TimeSpan firstDayGap = new TimeSpan(remainingHours, remainingMinutes, remainingSeconds);
                Double firstDayDistance = (firstDayGap.TotalHours * UnitSize);
                DateTime nextDay = StartDate.Add(new TimeSpan(remainingHours, remainingMinutes, 0));

                DrawIncrementLines(grid, nextDay, firstDayDistance,
                                    increment, daySize, DayLineBrush, 7);
            }
        }

        private void DrawIncrementLines(Canvas grid, DateTime firstLineDate, Double firstLineDistance,
                TimeSpan timeStep, Double unitSize, Brush brush, int majorEvery, int majorEveryOffset = 0)
        {
            Double curX = firstLineDistance;
            DateTime curDate = firstLineDate;
            int curLine = 0;
            while (curX < Width)
            {
                Line l = new Line();
                l.ToolTip = curDate;
                l.StrokeThickness = MinorUnitThickness;
                if ((majorEvery > 0) && ((curLine - majorEveryOffset) % majorEvery == 0))
                {
                    l.StrokeThickness = MajorUnitThickness;
                }
                l.Stroke = brush;
                l.X1 = 0;
                l.X2 = 0;
                l.Y1 = 0;
                l.Y2 = Math.Max(DesiredSize.Height, Height);
                grid.Children.Add(l);
                Canvas.SetLeft(l, curX);
                curX += unitSize;
                curDate += timeStep;
                curLine++;
            }
        }

        #endregion background and grid methods

        #region mouse enter and leave events

        private void TimeLineControl_MouseLeave(object sender, MouseEventArgs e)
        {
            //Keyboard.Focus(this);
        }

        private void TimeLineControl_MouseEnter(object sender, MouseEventArgs e)
        {
            //Keyboard.Focus(this);
        }

        #endregion mouse enter and leave events

        #region drag events and fields

        private Boolean _dragging = false;
        private Point _dragStartPosition = new Point(double.MinValue, double.MinValue);

        /// <summary>
        /// When we drag something from an external control over this I need a temp control
        /// that lets me adorn those accordingly as well
        /// </summary>
        private TimeLineItemControl _tmpDraggAdornerControl;

        private TimeLineItemControl _dragObject = null;

        private void item_PreviewDragButtonDown(object sender, MouseButtonEventArgs e)
        {
            _dragStartPosition = Mouse.GetPosition(null);
            _dragObject = sender as TimeLineItemControl;
        }

        private void item_PreviewDragButtonUp(object sender, MouseButtonEventArgs e)
        {
            _dragStartPosition.X = double.MinValue;
            _dragStartPosition.Y = double.MinValue;
            _dragObject = null;
        }

        private void TimeLineControl_DragOver(object sender, DragEventArgs e)
        {
            //throw new NotImplementedException();
            TimeLineItemControl d = e.Data.GetData(typeof(TimeLineItemControl)) as TimeLineItemControl;
            if (d != null)
            {
                if (Manager != null)
                {
                    if (!Manager.CanAddToTimeLine(d.DataContext as ITimeLineDataItem))
                    {
                        e.Effects = DragDropEffects.None;
                        return;
                    }
                }
                e.Effects = DragDropEffects.Move;
                //this is an internal drag or a drag from another time line control
                if (DragAdorner == null)
                {
                    _dragAdorner = new TimeLineDragAdorner(d, ItemTemplate);
                }
                DragAdorner.MousePosition = e.GetPosition(d);
                DragAdorner.InvalidateVisual();
            }
            else
            {//GongSolutions.Wpf.DragDrop
                var d2 = e.Data.GetData("GongSolutions.Wpf.DragDrop");
                if (d2 != null)
                {
                    if (Manager != null)
                    {
                        if (!Manager.CanAddToTimeLine(d2 as ITimeLineDataItem))
                        {
                            e.Effects = DragDropEffects.None;
                            return;
                        }
                    }

                    e.Effects = DragDropEffects.Move;
                    if (DragAdorner == null)
                    {
                        //we are dragging from an external source and we don't have a timeline item control of any sort
                        Children.Remove(_tmpDraggAdornerControl);
                        //in order to get an adornment layer the control has to be somewhere
                        _tmpDraggAdornerControl = new TimeLineItemControl();
                        _tmpDraggAdornerControl.UnitSize = UnitSize;
                        Children.Add(_tmpDraggAdornerControl);
                        Canvas.SetLeft(_tmpDraggAdornerControl, -1000000);
                        _tmpDraggAdornerControl.DataContext = d2;
                        _tmpDraggAdornerControl.StartTime = StartDate;
                        _tmpDraggAdornerControl.InitializeDefaultLength();
                        _tmpDraggAdornerControl.ContentTemplate = ItemTemplate;

                        _dragAdorner = new TimeLineDragAdorner(_tmpDraggAdornerControl, ItemTemplate);
                    }
                    DragAdorner.MousePosition = e.GetPosition(_tmpDraggAdornerControl);
                    DragAdorner.InvalidateVisual();
                }
            }
            DragScroll(e);
        }

        private void TimeLineControL_DragLeave(object sender, DragEventArgs e)
        {
            DragAdorner = null;
            Children.Remove(_tmpDraggAdornerControl);
            _tmpDraggAdornerControl = null;
        }

        private void TimeLineControl_Drop(object sender, DragEventArgs e)
        {
            DragAdorner = null;

            TimeLineItemControl dropper = e.Data.GetData(typeof(TimeLineItemControl)) as TimeLineItemControl;
            ITimeLineDataItem dropData = null;
            if (dropper == null)
            {
                //dropData = e.Data.GetData(typeof(ITimeLineDataItem)) as ITimeLineDataItem;
                dropData = e.Data.GetData("GongSolutions.Wpf.DragDrop") as ITimeLineDataItem;
                if (dropData != null)
                {
                    //I haven't figured out why but
                    //sometimes when dropping from an external source
                    //the drop event hits twice.
                    //that results in ugly duplicates ending up in the timeline
                    //and it is a mess.
                    if (Items.Contains(dropData))
                        return;
                    //create a new timeline item control from this data
                    dropper = CreateTimeLineItemControl(dropData);
                    dropper.StartTime = StartDate;
                    dropper.InitializeDefaultLength();
                    Children.Remove(_tmpDraggAdornerControl);
                    _tmpDraggAdornerControl = null;
                }
            }
            var dropX = e.GetPosition(this).X;
            int newIndex = GetDroppedNewIndex(dropX);
            var curData = dropper.DataContext as ITimeLineDataItem;
            var curIndex = Items.IndexOf(curData);
            if ((curIndex == newIndex || curIndex + 1 == newIndex) && dropData == null && dropper.Parent == this)//dropdata null is to make sure we aren't failing on adding a new data item into the timeline
            //dropper.parent==this makes it so that we allow a dropper control from another timeline to be inserted in at the start.
            {
                return;//our drag did nothing meaningful so we do nothing.
            }

            if (dropper != null)
            {
                DateTime start = (DateTime)GetValue(StartDateProperty);
                if (newIndex == 0)
                {
                    if (dropData == null)
                    {
                        RemoveTimeLineItemControl(dropper);
                    }
                    if (dropper.Parent != this && dropper.Parent is TimeLineControl)
                    {
                        var tlCtrl = dropper.Parent as TimeLineControl;
                        tlCtrl.RemoveTimeLineItemControl(dropper);
                    }
                    InsertTimeLineItemControlAt(newIndex, dropper);
                    dropper.MoveToNewStartTime(start);
                    MakeRoom(newIndex, dropper.Width);
                }
                else//we are moving this after something.
                {
                    //find out if we are moving the existing one back or forward.
                    var placeAfter = GetTimeLineItemControlAt(newIndex - 1);
                    if (placeAfter != null)
                    {
                        start = placeAfter.EndTime;
                        RemoveTimeLineItemControl(dropper);
                        if (curIndex < newIndex && curIndex >= 0)//-1 is on an insert in which case we definitely don't want to take off on our new index value
                        {
                            //we are moving forward.
                            newIndex--;//when we removed our item, we shifted our insert index back 1
                        }
                        if (dropper.Parent != null && dropper.Parent != this)
                        {
                            var ptl = dropper.Parent as TimeLineControl;
                            ptl.RemoveTimeLineItemControl(dropper);
                        }

                        InsertTimeLineItemControlAt(newIndex, dropper);
                        dropper.MoveToNewStartTime(start);
                        MakeRoom(newIndex, dropper.Width);
                    }
                }
            }
            //ReDrawChildren();
            DrawBackGround();
            e.Handled = true;
        }

        #region drop helpers

        private void InsertTimeLineItemControlAt(int index, TimeLineItemControl adder)
        {
            var Data = adder.DataContext as ITimeLineDataItem;
            if (Items.Contains(Data))
                return;

            adder.PreviewMouseRightButtonDown -= item_PreviewEditButtonDown;
            adder.MouseMove -= item_MouseMove;
            adder.PreviewMouseRightButtonUp -= item_PreviewEditButtonUp;

            adder.PreviewMouseLeftButtonUp -= item_PreviewDragButtonUp;
            adder.PreviewMouseLeftButtonDown -= item_PreviewDragButtonDown;

            adder.PreviewMouseRightButtonDown += item_PreviewEditButtonDown;
            adder.MouseMove += item_MouseMove;
            adder.PreviewMouseRightButtonUp += item_PreviewEditButtonUp;

            adder.PreviewMouseLeftButtonUp += item_PreviewDragButtonUp;
            adder.PreviewMouseLeftButtonDown += item_PreviewDragButtonDown;
            //child 0 is our grid and we want to keep that there.
            Children.Insert(index + 1, adder);
            Items.Insert(index, Data);
        }

        private void RemoveTimeLineItemControl(TimeLineItemControl remover)
        {
            var curData = remover.DataContext as ITimeLineDataItem;
            remover.PreviewMouseRightButtonDown -= item_PreviewEditButtonDown;
            remover.MouseMove -= item_MouseMove;
            remover.PreviewMouseRightButtonUp -= item_PreviewEditButtonUp;

            remover.PreviewMouseLeftButtonUp -= item_PreviewDragButtonUp;
            remover.PreviewMouseLeftButtonDown -= item_PreviewDragButtonDown;
            Items.Remove(curData);
            Children.Remove(remover);
        }

        private int GetDroppedNewIndex(Double dropX)
        {
            Double s = 0;
            Double w = 0;
            Double e = 0;
            for (int i = 0; i < Items.Count(); i++)
            {
                var checker = GetTimeLineItemControlAt(i);
                if (checker == null)
                    continue;
                checker.GetPlacementInfo(ref s, ref w, ref e);
                if (dropX < s)
                {
                    return i;
                }
                if (s < dropX && e > dropX)
                {
                    Double distStart = Math.Abs(dropX - s);
                    Double distEnd = Math.Abs(dropX - e);
                    if (distStart < distEnd)//we dropped closer to the start of this item
                    {
                        return i;
                    }
                    //we are closer to the end of this item
                    return i + 1;
                }
                if (e < dropX && i == Items.Count() - 1)
                {
                    return i + 1;
                }
                if (s < dropX && i == Items.Count() - 1)
                {
                    return i;
                }
            }
            return Items.Count;
        }

        private void MakeRoom(int newIndex, Double width)
        {
            int moveIndex = newIndex + 1;
            //get our forward chain and gap
            Double chainGap = 0;

            //because the grid is child 0 and we are essentially indexing as if it wasn't there
            //the child index of add after is our effective index of next
            var nextCtrl = GetTimeLineItemControlAt(moveIndex);
            if (nextCtrl != null)
            {
                Double nL = 0;
                Double nW = 0;
                Double nE = 0;
                nextCtrl.GetPlacementInfo(ref nL, ref nW, ref nE);

                Double droppedIntoSpace = 0;
                if (newIndex == 0)
                {
                    droppedIntoSpace = nL;
                }
                else
                {
                    var previousControl = GetTimeLineItemControlAt(newIndex - 1);
                    if (previousControl != null)
                    {
                        Double aL = 0;
                        Double aW = 0;
                        Double aE = 0;
                        previousControl.GetPlacementInfo(ref aL, ref aW, ref aE);
                        droppedIntoSpace = nL - aE;
                    }
                }
                Double neededSpace = width - droppedIntoSpace;
                if (neededSpace <= 0)
                    return;

                var forwardChain = GetTimeLineForwardChain(nextCtrl, moveIndex + 1, ref chainGap);

                if (chainGap < neededSpace)
                {
                    while (neededSpace > 0)
                    {
                        //move it to the smaller of our values -gap or remaning space
                        Double move = Math.Min(chainGap, neededSpace);
                        foreach (var tictrl in forwardChain)
                        {
                            tictrl.MoveMe(move);
                            neededSpace -= move;
                        }
                        //get our new chain and new gap
                        forwardChain = GetTimeLineForwardChain(nextCtrl, moveIndex + 1, ref chainGap);
                    }
                }
                else
                {
                    foreach (var tictrl in forwardChain)
                    {
                        tictrl.MoveMe(neededSpace);
                    }
                }
            }//if next ctrl is null we are adding to the very end and there is no work to do to make room.
        }

        #endregion drop helpers

        //NOT WORKING YET AND I DON'T KNOW WHY 8(
        private void DragScroll(DragEventArgs e)
        {
            if (_scrollViewer == null)
            {
                _scrollViewer = GetParentScrollViewer();
            }
            if (_scrollViewer != null)
            {
                var available = LayoutInformation.GetLayoutSlot(this);
                Point scrollPos = e.GetPosition(_scrollViewer);
                Double scrollMargin = 50;
                var actualW = _scrollViewer.ActualWidth;
                if (scrollPos.X >= actualW - scrollMargin &&
                    _scrollViewer.HorizontalOffset <= _scrollViewer.ExtentWidth - _scrollViewer.ViewportWidth)
                {
                    _scrollViewer.LineRight();
                }
                else if (scrollPos.X < scrollMargin && _scrollViewer.HorizontalOffset > 0)
                {
                    _scrollViewer.LineLeft();
                }
                Double actualH = _scrollViewer.ActualHeight;
                if (scrollPos.Y >= actualH - scrollMargin &&
                    _scrollViewer.VerticalOffset <= _scrollViewer.ExtentHeight - _scrollViewer.ViewportHeight)
                {
                    _scrollViewer.LineDown();
                }
                else if (scrollPos.Y < scrollMargin && _scrollViewer.VerticalOffset >= 0)
                {
                    _scrollViewer.LineUp();
                }
            }
        }

        #endregion drag events and fields

        #region edit events etc

        private Double _curX = 0;
        private TimeLineAction _action;

        private void item_PreviewEditButtonUp(object sender, MouseButtonEventArgs e)
        {
            (sender as TimeLineItemControl).ReleaseMouseCapture();
            Keyboard.Focus(this);
        }

        private void item_PreviewEditButtonDown(object sender, MouseButtonEventArgs e)
        {
            var ctrl = sender as TimeLineItemControl;

            _action = ctrl.GetClickAction();
            (sender as TimeLineItemControl).CaptureMouse();
        }

        #region key down and up

        private Boolean _rightCtrlDown = false;
        private Boolean _leftCtrlDown = false;

        protected void OnKeyDown(Object sender, KeyEventArgs e)
        {
            base.OnKeyDown(e);
            if (e.Key == Key.LeftCtrl || e.Key == Key.RightCtrl)
            {
                _rightCtrlDown = e.Key == Key.RightCtrl;
                _leftCtrlDown = e.Key == Key.LeftCtrl;
                ManipulationMode = TimeLineManipulationMode.Linked;
            }
        }

        protected void OnKeyUp(Object sender, KeyEventArgs e)
        {
            if (e.Key == Key.LeftCtrl)
                _leftCtrlDown = false;
            if (e.Key == Key.RightCtrl)
                _rightCtrlDown = false;
            if (!_leftCtrlDown && !_rightCtrlDown)
                ManipulationMode = TimeLineManipulationMode.Linked;
        }

        internal void HandleItemManipulation(TimeLineItemControl ctrl, TimeLineItemChangedEventArgs e)
        {
            Boolean doStretch = false;
            TimeSpan deltaT = e.DeltaTime;
            TimeSpan zeroT = new TimeSpan();
            int direction = deltaT.CompareTo(zeroT);
            if (direction == 0)
                return;//shouldn't happen

            TimeLineItemControl previous = null;
            TimeLineItemControl after = null;
            int afterIndex = -1;
            int previousIndex = -1;
            after = GetTimeLineItemControlStartingAfter(ctrl.StartTime, ref afterIndex);
            previous = GetTimeLineItemControlStartingBefore(ctrl.StartTime, ref previousIndex);
            if (after != null)
                after.ReadyToDraw = false;
            if (ctrl != null)
                ctrl.ReadyToDraw = false;
            Double useDeltaX = e.DeltaX;
            Double cLeft = 0;
            Double cWidth = 0;
            Double cEnd = 0;
            ctrl.GetPlacementInfo(ref cLeft, ref cWidth, ref cEnd);

            switch (e.Action)
            {
                case TimeLineAction.Move:

                    #region move

                    Double chainGap = Double.MaxValue;
                    if (direction > 0)
                    {
                        //find chain connecteds that are after this one
                        //delta each one in that chain that we are pushing
                        List<TimeLineItemControl> afterChain = GetTimeLineForwardChain(ctrl, afterIndex, ref chainGap);

                        if (chainGap < useDeltaX)
                            useDeltaX = chainGap;
                        foreach (var ti in afterChain)
                        {
                            ti.MoveMe(useDeltaX);
                        }

                        //find the size of our chain so we bring it into view
                        var first = afterChain[0];
                        var last = afterChain[afterChain.Count - 1];
                        BringChainIntoView(first, last, direction);
                    }
                    if (direction < 0)
                    {
                        Boolean previousBackToStart = false;
                        List<TimeLineItemControl> previousChain = GetTimeLineBackwardsChain(ctrl, previousIndex, ref previousBackToStart, ref chainGap);
                        if (-chainGap > useDeltaX)
                        {
                            useDeltaX = chainGap;
                        }
                        if (!previousBackToStart)
                        {
                            foreach (var ti in previousChain)
                            {
                                ti.MoveMe(useDeltaX);
                            }
                        }
                        var first = previousChain[0];//previousChain[previousChain.Count - 1];
                        var last = previousChain[previousChain.Count - 1];
                        BringChainIntoView(last, first, direction);
                    }

                    #endregion move

                    break;

                case TimeLineAction.StretchStart:
                    switch (e.Mode)
                    {
                        #region stretchstart

                        case TimeLineManipulationMode.Linked:

                            #region linked

                            Double gap = Double.MaxValue;
                            if (previous != null)
                            {
                                Double pLeft = 0;
                                Double pWidth = 0;
                                Double pEnd = 0;
                                previous.GetPlacementInfo(ref pLeft, ref pWidth, ref pEnd);
                                gap = cLeft - pEnd;
                            }
                            if (direction < 0 && Math.Abs(gap) < Math.Abs(useDeltaX) && Math.Abs(gap) > _bumpThreshold)//if we are negative and not linked, but about to bump
                                useDeltaX = -gap;
                            if (Math.Abs(gap) < _bumpThreshold)
                            {//we are linked
                                if (ctrl.CanDelta(0, useDeltaX) && previous.CanDelta(1, useDeltaX))
                                {
                                    ctrl.MoveStartTime(useDeltaX);
                                    previous.MoveEndTime(useDeltaX);
                                }
                            }
                            else if (ctrl.CanDelta(0, useDeltaX))
                            {
                                ctrl.MoveStartTime(useDeltaX);
                            }

                            break;

                        #endregion linked

                        case TimeLineManipulationMode.Free:

                            #region free

                            gap = Double.MaxValue;
                            doStretch = direction > 0;
                            if (direction < 0)
                            {
                                //disallow us from free stretching into another item

                                if (previous != null)
                                {
                                    Double pLeft = 0;
                                    Double pWidth = 0;
                                    Double pEnd = 0;
                                    previous.GetPlacementInfo(ref pLeft, ref pWidth, ref pEnd);
                                    gap = cLeft - pEnd;
                                }
                                else
                                {
                                    //don't allow us to stretch further than the gap between current and start time
                                    DateTime s = (DateTime)GetValue(StartDateProperty);
                                    gap = cLeft;
                                }
                                doStretch = gap > _bumpThreshold;
                                if (gap < useDeltaX)
                                {
                                    useDeltaX = gap;
                                }
                            }

                            doStretch &= ctrl.CanDelta(0, useDeltaX);

                            if (doStretch)
                            {
                                ctrl.MoveStartTime(useDeltaX);
                            }

                            #endregion free

                            break;

                        default:
                            break;

                            #endregion stretchstart
                    }
                    break;

                case TimeLineAction.StretchEnd:
                    switch (e.Mode)
                    {
                        #region stretchend

                        case TimeLineManipulationMode.Linked:

                            #region linked

                            Double gap = Double.MaxValue;
                            if (after != null)
                            {
                                Double aLeft = 0;
                                Double aWidth = 0;
                                Double aEnd = 0;
                                after.GetPlacementInfo(ref aLeft, ref aWidth, ref aEnd);
                                gap = aLeft - cEnd;
                            }

                            if (direction > 0 && gap > _bumpThreshold && gap < useDeltaX)//if we are positive, not linked but about to bump
                                useDeltaX = -gap;
                            if (gap < _bumpThreshold)
                            {//we are linked
                                if (ctrl.CanDelta(1, useDeltaX) && after.CanDelta(0, useDeltaX))
                                {
                                    ctrl.MoveEndTime(useDeltaX);
                                    after.MoveStartTime(useDeltaX);
                                }
                            }
                            else if (ctrl.CanDelta(0, useDeltaX))
                            {
                                ctrl.MoveEndTime(useDeltaX);
                            }
                            break;

                        #endregion linked

                        case TimeLineManipulationMode.Free:

                            #region free

                            Double nextGap = Double.MaxValue;
                            doStretch = true;
                            if (direction > 0 && after != null)
                            {
                                //disallow us from free stretching into another item
                                Double nLeft = 0;
                                Double nWidth = 0;
                                Double nEnd = 0;
                                after.GetPlacementInfo(ref nLeft, ref nWidth, ref nEnd);
                                nextGap = nLeft - cEnd;
                                doStretch = nextGap > _bumpThreshold;
                                if (nextGap < useDeltaX)
                                    useDeltaX = nextGap;
                            }

                            doStretch &= ctrl.CanDelta(1, useDeltaX);
                            if (doStretch)
                            {
                                ctrl.MoveEndTime(useDeltaX);
                            }

                            break;

                        #endregion free

                        default:
                            break;

                            #endregion stretchend
                    }
                    break;

                default:
                    break;
            }
        }

        private void BringChainIntoView(TimeLineItemControl first, TimeLineItemControl last, int direction)
        {
            Double l1 = 0;
            Double l2 = 0;
            Double w = 0;
            Double w2 = 0;
            Double end = 0;
            first.GetPlacementInfo(ref l1, ref w, ref end);
            last.GetPlacementInfo(ref l2, ref w2, ref end);
            Double chainW = end - l1;
            Double leadBuffer = 4 * UnitSize;
            chainW += leadBuffer;
            if (direction > 0)
            {
                first.BringIntoView(new Rect(new Point(0, 0), new Point(chainW, Height)));
            }
            else
            {
                first.BringIntoView(new Rect(new Point(-leadBuffer, 0), new Point(chainW, Height)));
            }
        }

        #endregion key down and up

        #endregion edit events etc

        /// <summary>
        /// Mouse move is important for both edit and drag behaviors
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void item_MouseMove(object sender, System.Windows.Input.MouseEventArgs e)
        {
            #region drag - left click and move

            TimeLineItemControl ctrl = sender as TimeLineItemControl;
            if (ctrl == null)
                return;

            if (Mouse.LeftButton == MouseButtonState.Pressed)
            {
                if (ctrl.IsExpanded)
                    return;
                var position = Mouse.GetPosition(null);
                if (Math.Abs(position.X - _dragStartPosition.X) > SystemParameters.MinimumHorizontalDragDistance ||
                    Math.Abs(position.Y - _dragStartPosition.Y) > SystemParameters.MinimumVerticalDragDistance)
                {
                    DragDrop.DoDragDrop(this, ctrl, DragDropEffects.Move | DragDropEffects.Scroll);
                    _dragging = true;
                }

                return;
            }

            #endregion drag - left click and move

            #region edits - right click and move

            if (Mouse.Captured != ctrl)
            {
                _curX = Mouse.GetPosition(null).X;
                return;
            }

            Double mouseX = Mouse.GetPosition(null).X;
            Double deltaX = mouseX - _curX;
            TimeSpan deltaT = ctrl.GetDeltaTime(deltaX);
            var curMode = (TimeLineManipulationMode)GetValue(ManipulationModeProperty);
            HandleItemManipulation(ctrl, new TimeLineItemChangedEventArgs()
            {
                Action = _action,
                DeltaTime = deltaT,
                DeltaX = deltaX,
                Mode = curMode
            });

            DrawBackGround();
            _curX = mouseX;

            //When we pressed, this lost focus and we therefore didn't capture any changes to the key status
            //so we check it again after our manipulation finishes.  That way we can be linked and go out of or back into it while dragging
            ManipulationMode = TimeLineManipulationMode.Free;
            _leftCtrlDown = Keyboard.IsKeyDown(Key.LeftCtrl);
            _rightCtrlDown = Keyboard.IsKeyDown(Key.RightCtrl);
            if (_leftCtrlDown || _rightCtrlDown)
            {
                ManipulationMode = TimeLineManipulationMode.Linked;
            }

            #endregion edits - right click and move
        }

        #region get children methods

        /// <summary>
        /// Returns a list of all timeline controls starting with the current one and moving forward
        /// so long as they are contiguous.
        /// </summary>
        /// <param name="current"></param>
        /// <returns></returns>
        private List<TimeLineItemControl> GetTimeLineForwardChain(TimeLineItemControl current, int afterIndex, ref Double chainGap)
        {
            List<TimeLineItemControl> returner = new List<TimeLineItemControl>() { current };
            Double left = 0, width = 0, end = 0;
            current.GetPlacementInfo(ref left, ref width, ref end);
            if (afterIndex < 0)
            {
                //we are on the end of the list so there is no limit.
                chainGap = Double.MaxValue;
                return returner;
            }
            Double bumpThreshold = _bumpThreshold;
            Double lastAddedEnd = end;
            while (afterIndex < Items.Count)
            {
                left = width = end = 0;
                var checker = GetTimeLineItemControlAt(afterIndex++);
                if (checker != null)
                {
                    checker.GetPlacementInfo(ref left, ref width, ref end);
                    Double gap = left - lastAddedEnd;
                    if (gap > bumpThreshold)
                    {
                        chainGap = gap;
                        return returner;
                    }
                    returner.Add(checker);
                    lastAddedEnd = end;
                }
            }
            //we have chained off to the end and thus have no need to worry about our gap
            chainGap = Double.MaxValue;
            return returner;
        }

        /// <summary>
        /// Returns a list of all timeline controls starting with the current one and moving backwoards
        /// so long as they are contiguous.  If the chain reaches back to the start time of the timeline then the
        /// ChainsBackToStart boolean is modified to reflect that.
        /// </summary>
        /// <param name="current"></param>
        /// <returns></returns>
        private List<TimeLineItemControl> GetTimeLineBackwardsChain(TimeLineItemControl current, int prevIndex, ref Boolean ChainsBackToStart, ref Double chainGap)
        {
            List<TimeLineItemControl> returner = new List<TimeLineItemControl>() { current };
            Double left = 0, width = 0, end = 0;
            current.GetPlacementInfo(ref left, ref width, ref end);
            if (prevIndex < 0)
            {
                chainGap = Double.MaxValue;
                ChainsBackToStart = left == 0;
                return returner;
            }

            Double lastAddedLeft = left;
            while (prevIndex >= 0)
            {
                left = width = end = 0;

                var checker = GetTimeLineItemControlAt(prevIndex--);
                if (checker != null)
                {
                    checker.GetPlacementInfo(ref left, ref width, ref end);
                    if (lastAddedLeft - end > _bumpThreshold)
                    {
                        //our chain just broke;
                        chainGap = lastAddedLeft - end;
                        ChainsBackToStart = lastAddedLeft == 0;
                        return returner;
                    }
                    returner.Add(checker);
                    lastAddedLeft = left;
                }
            }
            ChainsBackToStart = lastAddedLeft == 0;
            chainGap = lastAddedLeft;//gap between us and zero;
            return returner;
        }

        private TimeLineItemControl GetTimeLineItemControlStartingBefore(DateTime dateTime, ref int index)
        {
            index = -1;
            for (int i = 0; i < Items.Count; i++)
            {
                var checker = GetTimeLineItemControlAt(i);
                if (checker != null && checker.StartTime == dateTime && i != 0)
                {
                    index = i - 1;
                    return GetTimeLineItemControlAt(i - 1);
                }
            }
            index = -1;
            return null;
        }

        private TimeLineItemControl GetTimeLineItemControlStartingAfter(DateTime dateTime, ref int index)
        {
            for (int i = 0; i < Items.Count; i++)
            {
                var checker = GetTimeLineItemControlAt(i);
                if (checker != null && checker.StartTime > dateTime)
                {
                    index = i;
                    return checker;
                }
            }
            index = -1;
            return null;
        }

        private TimeLineItemControl GetTimeLineItemControlAt(int i)
        {
            //child 0 is our background grid.
            i++;
            if (i <= 0 || i >= Children.Count)
                return null;
            return Children[i] as TimeLineItemControl;
        }

        #endregion get children methods
    }
}