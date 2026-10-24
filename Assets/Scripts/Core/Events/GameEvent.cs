using System;
using System.Collections.Generic;

namespace InkAndAsh.Core.Events
{
    public interface IGameEventListener<T>
    {
        void OnEventRaised(T value);
    }

    public interface IGameEvent<T>
    {
        void Raise(T value);
        void RegisterListener(IGameEventListener<T> listener);
        void UnregisterListener(IGameEventListener<T> listener);
        void RegisterAction(Action<T> action);
        void UnregisterAction(Action<T> action);
    }

#if UNITY_5_3_OR_NEWER || UNITY_EDITOR
    public abstract class GameEvent<T> : UnityEngine.ScriptableObject, IGameEvent<T>
#else
    public abstract class GameEvent<T> : IGameEvent<T>
#endif
    {
        private readonly List<IGameEventListener<T>> _listeners = new List<IGameEventListener<T>>();
        private event Action<T> _actionListeners;

        public void Raise(T value)
        {
            for (int i = _listeners.Count - 1; i >= 0; i--)
            {
                if (i < _listeners.Count)
                {
                    _listeners[i]?.OnEventRaised(value);
                }
            }
            _actionListeners?.Invoke(value);
        }

        public void RegisterListener(IGameEventListener<T> listener)
        {
            if (listener != null && !_listeners.Contains(listener))
            {
                _listeners.Add(listener);
            }
        }

        public void UnregisterListener(IGameEventListener<T> listener)
        {
            if (listener != null)
            {
                _listeners.Remove(listener);
            }
        }

        public void RegisterAction(Action<T> action)
        {
            if (action != null)
            {
                _actionListeners += action;
            }
        }

        public void UnregisterAction(Action<T> action)
        {
            if (action != null)
            {
                _actionListeners -= action;
            }
        }
    }

    public class SimpleGameEvent<T> : GameEvent<T>
    {
    }
}

#if STANDALONE_TEST
namespace InkAndAsh.StandaloneTests
{
    using System.Collections.Generic;
    using NUnit.Framework;
    using InkAndAsh.Core.Events;

    [TestFixture]
    public class GameEventsProductionTests
    {
        private class TestListener<T> : IGameEventListener<T>
        {
            public T ReceivedValue { get; private set; }
            public int CallCount { get; private set; }

            public void OnEventRaised(T value)
            {
                ReceivedValue = value;
                CallCount++;
            }
        }

        [Test]
        public void SimpleGameEvent_RegisterAndRaise_DispatchesToListener()
        {
            var gameEvent = new SimpleGameEvent<string>();
            var listener = new TestListener<string>();

            gameEvent.RegisterListener(listener);
            gameEvent.Raise("Pilgrimage Begins");

            Assert.That(listener.CallCount, Is.EqualTo(1));
            Assert.That(listener.ReceivedValue, Is.EqualTo("Pilgrimage Begins"));
        }

        [Test]
        public void SimpleGameEvent_UnregisterListener_StopsDispatch()
        {
            var gameEvent = new SimpleGameEvent<int>();
            var listener = new TestListener<int>();

            gameEvent.RegisterListener(listener);
            gameEvent.Raise(42);
            Assert.That(listener.CallCount, Is.EqualTo(1));

            gameEvent.UnregisterListener(listener);
            gameEvent.Raise(100);
            Assert.That(listener.CallCount, Is.EqualTo(1));
        }

        [Test]
        public void ConcreteEvents_RaisePayloads_Successfully()
        {
            var combatEvent = new OnCombatStartedEvent();
            CombatStartData receivedData = default;
            combatEvent.RegisterAction(data => receivedData = data);

            var startData = new CombatStartData(
                new List<string> { "hero_vicar" },
                new List<string> { "enemy_wolf" },
                "biome_forest"
            );
            combatEvent.Raise(startData);

            Assert.That(receivedData.biomeId, Is.EqualTo("biome_forest"));
            Assert.That(receivedData.partyMemberIds.Count, Is.EqualTo(1));
        }
    }
}
#endif
