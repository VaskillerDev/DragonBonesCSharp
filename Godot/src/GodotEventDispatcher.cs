using DragonBones;
using Godot;
using Godot.Collections;

namespace Test.code.dragonbones
{
    public class EventDispatcher<T> : Node
    {
        private readonly Dictionary<string, ListenerDelegate<T>> _listeners =
            new Dictionary<string, ListenerDelegate<T>>();

        public void DispatchEvent(string type, T @event)
        {
            if (!_listeners.ContainsKey(type)) _listeners[type](type, @event);
        }

        public bool HasEventListener(string type)
        {
            return _listeners.ContainsKey(type);
        }

        public void AddEventListener(string type, ListenerDelegate<T> listener)
        {
            if (_listeners.ContainsKey(type))
            {
                var delegates = _listeners[type].GetInvocationList();
                for (int i = 0, l = delegates.Length; i < l; ++i)
                    if (listener == delegates[i] as ListenerDelegate<T>)
                        return;

                _listeners[type] += listener;
            }
            else
            {
                _listeners.Add(type, listener);
            }
        }

        public void RemoveEventListener(string type, ListenerDelegate<T> listener)
        {
            if (!_listeners.ContainsKey(type)) return;

            var delegates = _listeners[type].GetInvocationList();
            for (int i = 0, l = delegates.Length; i < l; ++i)
            {
                if (listener != delegates[i] as ListenerDelegate<T>) continue;
                _listeners[type] -= listener;
                break;
            }

            if (_listeners[type] == null) _listeners.Remove(type);
        }
    }

    public class GodotEventDispatcher : EventDispatcher<EventObject>, IEventDispatcher<EventObject>
    {
        public bool HasDBEventListener(string type)
        {
            return HasEventListener(type);
        }

        public void DispatchDBEvent(string type, EventObject eventObject)
        {
            DispatchEvent(type, eventObject);
        }

        public void AddDBEventListener(string type, ListenerDelegate<EventObject> listener)
        {
            AddEventListener(type, listener);
        }

        public void RemoveDBEventListener(string type, ListenerDelegate<EventObject> listener)
        {
            RemoveEventListener(type, listener);
        }
    }
}