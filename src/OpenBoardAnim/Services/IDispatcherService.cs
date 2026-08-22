namespace OpenBoardAnim.Services
{
    public interface IDispatcherService
    {
        // Posts action to run after the current call stack unwinds, matching
        // Dispatcher.BeginInvoke's fire-and-forget semantics.
        void BeginInvoke(Action action);
    }
}
