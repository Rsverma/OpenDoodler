using System.Windows.Threading;

namespace OpenBoardAnim.Services
{
    // Thin wrapper over DispatcherTimer - a new instance is created per timer via the
    // Func<IAppTimer> factory registered in App.xaml.cs, since EditorViewModel needs two
    // independent timers (snapshot + backup) with their own intervals/Tick handlers.
    public class DispatcherAppTimer : IAppTimer
    {
        private readonly DispatcherTimer _timer = new();

        public TimeSpan Interval
        {
            get => _timer.Interval;
            set => _timer.Interval = value;
        }

        public event EventHandler Tick
        {
            add => _timer.Tick += value;
            remove => _timer.Tick -= value;
        }

        public void Start() => _timer.Start();
        public void Stop() => _timer.Stop();
    }
}
