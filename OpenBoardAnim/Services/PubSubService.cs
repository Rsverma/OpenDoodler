using System;

namespace OpenBoardAnim.Services
{
    public enum SubTopic
    {
        SceneReplaced,
        SceneChanged,
        GraphicAdded,
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
            if (_subscribers.TryGetValue(subTopic, out List<Action<object>> value))
            {
                value.ForEach(action => action(Message));
            }
        }

        public void Subscribe(SubTopic subTopic, Action<object> action)
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

        public void Unsubscribe(SubTopic subTopic, Action<object> action)
        {
            if (_subscribers.TryGetValue(subTopic, out List<Action<object>> value))
            {
                _ = value.Remove(action);
            }
        }
    }
}
