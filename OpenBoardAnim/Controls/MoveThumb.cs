using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls.Primitives;
using System.Windows.Controls;
using OpenBoardAnim.Models;
using OpenBoardAnim.Utilities;

namespace OpenBoardAnim.Controls
{
    public class MoveThumb : Thumb
    {
        public MoveThumb()
        {
            DragDelta += new DragDeltaEventHandler(this.MoveThumb_DragDelta);
        }

        private void MoveThumb_DragDelta(object sender, DragDeltaEventArgs e)
        {
            try
            {
                Control designerItem = this.DataContext as Control;

                if (designerItem != null)
                {
                    var model = designerItem.DataContext as GraphicModelBase;
                    if (model != null)
                    {

                        model.X += e.HorizontalChange;
                        model.Y += e.VerticalChange;
                    }
                }
            }
            catch (Exception ex)
            {
                if (Logger.LogError(ex, LogAction.LogAndShow))
                    throw;
            }
        }
    }
}
