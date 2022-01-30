using System;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using OpenBoardAnim.Util;

namespace OpenBoardAnim.Windows.Other
{
    public partial class Startup : Window
    {
        public Startup()
        {
            InitializeComponent();
        }

        #region Events

        private void Editor_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            var editor = new Editor();
            GenericShowDialog(editor);
        }

        #endregion

        #region Methods

        private void GenericShowDialog(Window window)
        {
            Hide();

            window.Owner = this;
            Application.Current.MainWindow = window;

            window.ShowDialog();

            Close();
        }

        #endregion
    }
}
