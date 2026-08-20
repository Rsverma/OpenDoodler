using OpenBoardAnim.Controls;
using OpenBoardAnim.Models;
using OpenBoardAnim.Utilities;
using OpenBoardAnim.ViewModels;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace OpenBoardAnim.Views
{
    /// <summary>
    /// Interaction logic for EditorCanvasView.xaml
    /// </summary>
    public partial class EditorCanvasView : UserControl
    {
        // Snap guides are drawn via an Adorner on the item-hosting Canvas rather than a second,
        // independently-sized overlay element - an Adorner's OnRender coordinate space is
        // guaranteed by WPF to exactly match AdornedElement's own local coordinate space (the
        // same one graphics' X/Y already live in), which a hand-tracked sibling Canvas could
        // only approximate and drifted out of alignment away from the top-left origin under
        // WPF's per-element layout rounding.
        private Canvas _itemsCanvas;
        private SnapGuideAdorner _snapGuideAdorner;
        private bool _isSubscribedToCanvasViewModel;

        public EditorCanvasView()
        {
            InitializeComponent();
            Loaded += EditorCanvasView_Loaded;
        }

        private void EditorCanvasView_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_itemsCanvas == null)
                {
                    _itemsCanvas = FindDescendant<Canvas>(GraphicsListBox);
                    AdornerLayer adornerLayer = _itemsCanvas == null ? null : AdornerLayer.GetAdornerLayer(_itemsCanvas);
                    if (adornerLayer != null)
                    {
                        _snapGuideAdorner = new SnapGuideAdorner(_itemsCanvas);
                        adornerLayer.Add(_snapGuideAdorner);
                    }
                }

                if (!_isSubscribedToCanvasViewModel && DataContext is EditorCanvasViewModel canvasViewModel)
                {
                    canvasViewModel.PropertyChanged += CanvasViewModel_PropertyChanged;
                    _isSubscribedToCanvasViewModel = true;
                    UpdateSnapGuideAdorner(canvasViewModel);
                }
            }
            catch (Exception ex)
            {
                if (Logger.LogError(ex, LogAction.LogAndShow))
                    throw;
            }
        }

        private void CanvasViewModel_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            try
            {
                bool isGuideProperty = e.PropertyName is nameof(EditorCanvasViewModel.IsSnapGuideXVisible)
                    or nameof(EditorCanvasViewModel.SnapGuideX)
                    or nameof(EditorCanvasViewModel.IsSnapGuideYVisible)
                    or nameof(EditorCanvasViewModel.SnapGuideY);
                if (isGuideProperty && sender is EditorCanvasViewModel canvasViewModel)
                    UpdateSnapGuideAdorner(canvasViewModel);
            }
            catch (Exception ex)
            {
                if (Logger.LogError(ex, LogAction.LogAndShow))
                    throw;
            }
        }

        private void UpdateSnapGuideAdorner(EditorCanvasViewModel canvasViewModel)
        {
            _snapGuideAdorner?.UpdateGuides(canvasViewModel.IsSnapGuideXVisible, canvasViewModel.SnapGuideX,
                canvasViewModel.IsSnapGuideYVisible, canvasViewModel.SnapGuideY);
        }

        private static T FindDescendant<T>(DependencyObject root) where T : DependencyObject
        {
            int count = VisualTreeHelper.GetChildrenCount(root);
            for (int i = 0; i < count; i++)
            {
                DependencyObject child = VisualTreeHelper.GetChild(root, i);
                if (child is T match) return match;
                T found = FindDescendant<T>(child);
                if (found != null) return found;
            }
            return null;
        }

        // Actions.SelectedGraphic (bound as ListBox.SelectedItem) only ever reflects one
        // item even in Extended selection mode - the full multi-selection has to be read
        // from SelectedItems directly and pushed into Actions.SelectedGraphics for group
        // Delete/Nudge to see it.
        private void GraphicsListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                if (sender is not ListBox listBox) return;
                if (FindAncestor<EditorView>(listBox) is not EditorView editorView) return;
                if (editorView.DataContext is not EditorViewModel editorViewModel) return;
                editorViewModel.Actions.SelectedGraphics = listBox.SelectedItems.Cast<GraphicModelBase>().ToList();
            }
            catch (Exception ex)
            {
                if (Logger.LogError(ex, LogAction.LogAndShow))
                    throw;
            }
        }

        // ListBoxItem doesn't select itself on right-click by default (only left-click does,
        // via MoveThumb_PreviewMouseLeftButtonDown), so without this the context menu would
        // act on whatever was selected before, not the item actually right-clicked.
        private void GraphicItem_PreviewMouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            try
            {
                if (sender is not DependencyObject element) return;
                if (FindAncestor<ListBoxItem>(element) is not ListBoxItem listBoxItem) return;
                if (FindAncestor<ListBox>(listBoxItem) is not ListBox listBox) return;
                if (!listBoxItem.IsSelected)
                {
                    listBox.SelectedItems.Clear();
                    // A grouped item selects every graphic sharing its GroupId too, so the
                    // context menu's Delete/Lock/Ungroup act on the whole group, matching the
                    // same expansion MoveThumb does for a plain left-click.
                    if (listBoxItem.DataContext is GraphicModelBase model && model.GroupId.HasValue)
                    {
                        foreach (object item in listBox.Items)
                        {
                            if (item is GraphicModelBase graphic && graphic.GroupId == model.GroupId)
                                listBox.SelectedItems.Add(graphic);
                        }
                    }
                    else
                    {
                        listBoxItem.IsSelected = true;
                    }
                }
            }
            catch (Exception ex)
            {
                if (Logger.LogError(ex, LogAction.LogAndShow))
                    throw;
            }
        }

        private static T FindAncestor<T>(DependencyObject current) where T : DependencyObject
        {
            while (current != null)
            {
                if (current is T match)
                    return match;
                current = VisualTreeHelper.GetParent(current);
            }
            return null;
        }
    }
}
