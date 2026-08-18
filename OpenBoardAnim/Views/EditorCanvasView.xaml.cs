using OpenBoardAnim.Models;
using OpenBoardAnim.Utilities;
using OpenBoardAnim.ViewModels;
using System;
using System.Collections.Generic;
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
        public EditorCanvasView()
        {
            InitializeComponent();
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
                    listBoxItem.IsSelected = true;
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
