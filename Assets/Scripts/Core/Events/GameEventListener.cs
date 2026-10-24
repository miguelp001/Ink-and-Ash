using System;

namespace InkAndAsh.Core.Events
{
#if UNITY_5_3_OR_NEWER || UNITY_EDITOR
    public abstract class GameEventListener<T> : UnityEngine.MonoBehaviour, IGameEventListener<T>
    {
        [UnityEngine.SerializeField] private GameEvent<T> _gameEvent;
        [UnityEngine.SerializeField] private UnityEngine.Events.UnityEvent<T> _response;

        public GameEvent<T> Event
        {
            get => _gameEvent;
            set => _gameEvent = value;
        }

        protected virtual void OnEnable()
        {
            _gameEvent?.RegisterListener(this);
        }

        protected virtual void OnDisable()
        {
            _gameEvent?.UnregisterListener(this);
        }

        public virtual void OnEventRaised(T value)
        {
            _response?.Invoke(value);
        }
    }
#else
    public class GameEventListener<T> : IGameEventListener<T>
    {
        private readonly Action<T> _callback;

        public GameEventListener(Action<T> callback)
        {
            _callback = callback;
        }

        public void OnEventRaised(T value)
        {
            _callback?.Invoke(value);
        }
    }
#endif
}
