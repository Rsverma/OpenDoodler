using OpenBoardAnim.Utilities;
using System;

namespace OpenBoardAnim.Services
{
    public enum SubTopic
    {
        SceneReplaced,
        SceneChanged,
        GraphicAdded,
        ProjectLaunched,
        ProjectExporting
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

        public void Publish(SubTopic subTopic, Object Message)
        {
            try
            {
                if (_subscribers.TryGetValue(subTopic, out List<Action<object>> value))
                {
                    value.ForEach(action => action(Message));
                }
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
                if (_subscribers.TryGetValue(subTopic, out List<Action<object>> value))
                {
                    value.Add(action);
                }
                else
                {
                    _subscribers.Add(subTopic, [action]);
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
                if (_subscribers.TryGetValue(subTopic, out List<Action<object>> value))
                {
                    _ = value.Remove(action);
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
