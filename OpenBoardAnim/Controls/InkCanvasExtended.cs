﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Ink;
using System.Windows.Media;

namespace OpenBoardAnim.Controls
{
    /// <summary>
    /// InkCanvasControl class extending the InkCanvas class
    /// </summary>
    public class InkCanvasExtended : InkCanvas
    {
        /// <summary>
        /// Gets or set the eraser shape
        /// </summary>
        public new StylusShape EraserShape
        {
            get { return (StylusShape) GetValue(EraserShapeProperty); }
            set { SetValue(EraserShapeProperty, value); }
        }

        // Using a DependencyProperty as the backing store for EraserShape.  
        // This enables animation, styling, binding, etc...
        public static readonly DependencyProperty EraserShapeProperty =
            DependencyProperty.Register("EraserShape", typeof (StylusShape), typeof (InkCanvasExtended),
                new UIPropertyMetadata(null, OnEraserShapePropertyChanged));

        /// <summary>
        /// Event to handle the property change
        /// </summary>
        /// <param name="d">dependency object</param>
        /// <param name="e">event args</param>
        private static void OnEraserShapePropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var uie = (InkCanvas) d;
            uie.EraserShape = (StylusShape) e.NewValue;
            uie.RenderTransform = new MatrixTransform();
        }
    }
}
