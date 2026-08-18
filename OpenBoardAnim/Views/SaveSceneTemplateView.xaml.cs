using OpenBoardAnim.Models;
using OpenBoardAnim.Utilities;
using System;
using System.Windows;
using System.Windows.Controls;

namespace OpenBoardAnim.Views
{
    /// <summary>
    /// Interaction logic for SaveSceneTemplateView.xaml
    /// </summary>
    public partial class SaveSceneTemplateView : UserControl
    {
        public SaveSceneTemplateView()
        {
            InitializeComponent();
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (DataContext is not SceneTemplateModel model) return;
                if (string.IsNullOrWhiteSpace(model.Name))
                {
                    MessageBox.Show("Enter a name for this template.", "Name Required");
                    return;
                }
                model.SaveTemplate?.Invoke(model);
                Window.GetWindow(this)?.Close();
            }
            catch (Exception ex)
            {
                if (Logger.LogError(ex, LogAction.LogAndShow))
                    throw;
            }
        }
    }
}
