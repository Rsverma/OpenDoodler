﻿using OpenBoardAnim.Helpers;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace OpenBoardAnim.Util
{
    /// <summary>
    /// Frame rate monitor. 
    /// </summary>
    public static class FrameRate
    {
        #region Private Variables

        private static Stopwatch _stopwatch = new Stopwatch();
        private static int _interval = 15;
        private static bool _started = true;
        private static bool _fixedFrameRate = false;

        #endregion

        /// <summary>
        /// Prepares the FrameRate monitor.
        /// </summary>
        /// <param name="interval">The selected interval of each snapshot.</param>
        public static void Start(int interval)
        {
            _interval = interval;
            _fixedFrameRate = Settings.Default.FixedFrameRate;
        }

        /// <summary>
        /// Gets the diff between the last call.
        /// </summary>
        /// <returns>The ammount of seconds.</returns>
        public static int GetMilliseconds(int? framerate = null)
        {
            if (framerate.HasValue)
                return framerate.Value;

            if (_fixedFrameRate)
                return _interval;

            if (_started)
            {
                _started = false;
                _stopwatch.Start();
                return _interval;
            }

            int mili = (int)_stopwatch.ElapsedMilliseconds;
            _stopwatch.Restart();
            return mili;
        }

        /// <summary>
        /// Determine that a stop/pause of the recording.
        /// </summary>
        public static void Stop()
        {
            _started = true;
            _stopwatch.Stop();
        }
    }
}
