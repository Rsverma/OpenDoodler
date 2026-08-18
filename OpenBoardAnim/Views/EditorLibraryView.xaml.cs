using OpenBoardAnim.Utilities;
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
    /// Interaction logic for EditorLibraryView.xaml
    /// </summary>
    public partial class EditorLibraryView : UserControl
    {
        public EditorLibraryView()
        {
            InitializeComponent();
        }

        // The graphics ListBox sits inside GraphicsScrollViewer (which is what should actually
        // scroll, since it also covers the "Load More" button below the list) - even with the
        // ListBox's own ScrollViewer.VerticalScrollBarVisibility set to Disabled, its internal
        // ScrollViewer still swallows MouseWheel before it bubbles to the outer one. Scroll the
        // outer ScrollViewer directly instead of relying on bubbling.
        private void GraphicsListBox_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            try
            {
                GraphicsScrollViewer.ScrollToVerticalOffset(GraphicsScrollViewer.VerticalOffset - e.Delta);
                e.Handled = true;
            }
            catch (Exception ex)
            {
                if (Logger.LogError(ex, LogAction.LogAndShow))
                    throw;
            }
        }
    }
}
