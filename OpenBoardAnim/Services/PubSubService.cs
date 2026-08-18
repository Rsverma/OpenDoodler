using OpenBoardAnim.Utilities;
using System;

namespace OpenBoardAnim.Services
{
    public enum SubTopic
    {
        SceneReplaced,
        SceneTemplateInserted,
        SceneChanged,
        GraphicAdded,
        ProjectLaunched,
        ProjectExporting,
        ProjectStateRestored
    }

    public interface IPubSubService
    {
        void Publish(SubTopic subTopic, object Message);
        void Subscribe(SubTopic subTopic, Action<object> action);
        void Unsubscribe(SubTopic subTopic, Action<object> action);
    }
    public class PubSubService : IPubSubService
    {
        private readonly Dictionary<SubTopic, List<Action<object>>> _subscribers = [];
        private readonly object _lock = new();

        public void Publish(SubTopic subTopic, Object Message)
        {
            try
            {
                // Snapshot under the lock so a subscriber that Subscribes/Unsubscribes
                // during Publish can't mutate the list mid-iteration, and so the lock
                // isn't held for the (unbounded) duration of running the callbacks.
                Action<object>[] actions;
                lock (_lock)
                {
                    if (!_subscribers.TryGetValue(subTopic, out List<Action<object>> value))
                        return;
                    actions = [.. value];
                }
                foreach (var action in actions)
                    action(Message);
            }
            catch (Exception ex)
            {
                if (Logger.LogError(ex, LogAction.LogAndThrow))
                    throw;
            }
        }

        public void Subscribe(SubTopic subTopic, Action<object> action)
        {
            try
            {
                lock (_lock)
                {
                    if (_subscribers.TryGetValue(subTopic, out List<Action<object>> value))
                    {
                        value.Add(action);
                    }
                    else
                    {
                        _subscribers.Add(subTopic, [action]);
                    }
                }
            }
            catch (Exception ex)
            {
                if (Logger.LogError(ex, LogAction.LogAndThrow))
                    throw;
            }
        }

        public void Unsubscribe(SubTopic subTopic, Action<object> action)
        {
            try
            {
                lock (_lock)
                {
                    if (_subscribers.TryGetValue(subTopic, out List<Action<object>> value))
                    {
                        _ = value.Remove(action);
                    }
                }
            }
            catch (Exception ex)
            {
                if (Logger.LogError(ex, LogAction.LogAndThrow))
                    throw;
            }
        }
    }
}
