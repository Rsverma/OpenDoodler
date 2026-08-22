namespace OpenBoardAnim.Services
{
    public interface IAppTimer
    {
        TimeSpan Interval { get; set; }
        event EventHandler Tick;
        void Start();
        void Stop();
    }
}
