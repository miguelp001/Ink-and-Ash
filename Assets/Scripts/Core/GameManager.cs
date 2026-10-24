using System;
using System.Collections.Generic;

namespace InkAndAsh.Core
{
    public enum GameState
    {
        Exploration,
        Campsite,
        Combat,
        Dialogue,
        Paused
    }

#if UNITY_5_3_OR_NEWER || UNITY_EDITOR
    public class GameManager : UnityEngine.MonoBehaviour
#else
    public class GameManager
#endif
    {
        private static GameManager _instance;
        private static readonly object _lock = new object();

        public static GameManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_lock)
                    {
                        if (_instance == null)
                        {
                            _instance = new GameManager();
                        }
                    }
                }
                return _instance;
            }
            protected set => _instance = value;
        }

        public GameState CurrentState { get; private set; } = GameState.Exploration;
        private GameState _previousState = GameState.Exploration;

        public event Action<GameState, GameState> OnGameStateChanged;

        // Runtime system references
        public TimeManager TimeManager { get; set; }
        public object WeatherSystem { get; set; }
        public object CaravanInventory { get; set; }
        public object DataManager { get; set; }

        private readonly Dictionary<Type, object> _registeredSystems = new Dictionary<Type, object>();

        public GameManager()
        {
            if (_instance == null)
            {
                _instance = this;
            }
        }

#if UNITY_5_3_OR_NEWER || UNITY_EDITOR
        protected virtual void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;
            DontDestroyOnLoad(gameObject);
        }
#endif

        public void RegisterSystem<T>(T system) where T : class
        {
            if (system == null) return;
            _registeredSystems[typeof(T)] = system;

            if (system is TimeManager tm) TimeManager = tm;
        }

        public T GetSystem<T>() where T : class
        {
            if (_registeredSystems.TryGetValue(typeof(T), out var system))
            {
                return system as T;
            }

            // Fallback to property match
            if (typeof(T) == typeof(TimeManager)) return TimeManager as T;
            if (WeatherSystem is T ws) return ws;
            if (CaravanInventory is T ci) return ci;
            if (DataManager is T dm) return dm;

            return null;
        }

        public bool ChangeState(GameState newState)
        {
            if (CurrentState == newState) return true;

            if (!IsValidTransition(CurrentState, newState))
            {
                LogWarning($"[GameManager] Invalid state transition rejected: {CurrentState} -> {newState}");
                return false;
            }

            GameState oldState = CurrentState;
            CurrentState = newState;
            OnGameStateChanged?.Invoke(oldState, newState);
            return true;
        }

        public void PauseGame()
        {
            if (CurrentState == GameState.Paused) return;

            _previousState = CurrentState;
            ChangeState(GameState.Paused);

#if UNITY_5_3_OR_NEWER || UNITY_EDITOR
            UnityEngine.Time.timeScale = 0f;
#endif
            if (TimeManager != null)
            {
                TimeManager.IsPaused = true;
            }
        }

        public void ResumeGame()
        {
            if (CurrentState != GameState.Paused) return;

#if UNITY_5_3_OR_NEWER || UNITY_EDITOR
            UnityEngine.Time.timeScale = 1f;
#endif
            if (TimeManager != null)
            {
                TimeManager.IsPaused = false;
            }

            ChangeState(_previousState);
        }

        private static bool IsValidTransition(GameState from, GameState to)
        {
            // Paused can always transition back to previous state, and any state can pause
            if (to == GameState.Paused) return true;
            if (from == GameState.Paused) return true;

            switch (from)
            {
                case GameState.Exploration:
                    return to == GameState.Campsite || to == GameState.Combat || to == GameState.Dialogue;
                case GameState.Campsite:
                    return to == GameState.Exploration;
                case GameState.Combat:
                    return to == GameState.Exploration || to == GameState.Campsite; // Campsite on defeat reload
                case GameState.Dialogue:
                    return to == GameState.Exploration;
                default:
                    return false;
            }
        }

        private static void LogWarning(string message)
        {
#if UNITY_5_3_OR_NEWER || UNITY_EDITOR
            UnityEngine.Debug.LogWarning(message);
#else
            Console.WriteLine(message);
#endif
        }
    }
}

#if STANDALONE_TEST
namespace InkAndAsh.StandaloneTests
{
    using NUnit.Framework;
    using InkAndAsh.Core;

    [TestFixture]
    public class GameManagerProductionTests
    {
        private GameManager _gameManager;

        [SetUp]
        public void Setup()
        {
            _gameManager = new GameManager();
            _gameManager.ChangeState(GameState.Exploration);
        }

        [Test]
        public void GameManager_InitialState_IsExploration()
        {
            Assert.That(_gameManager.CurrentState, Is.EqualTo(GameState.Exploration));
            Assert.That(GameManager.Instance, Is.Not.Null);
        }

        [Test]
        public void GameManager_ValidTransitions_Succeed()
        {
            GameState oldStateCaptured = GameState.Paused;
            GameState newStateCaptured = GameState.Paused;
            _gameManager.OnGameStateChanged += (oldS, newS) =>
            {
                oldStateCaptured = oldS;
                newStateCaptured = newS;
            };

            // Exploration -> Campsite
            bool toCamp = _gameManager.ChangeState(GameState.Campsite);
            Assert.That(toCamp, Is.True);
            Assert.That(_gameManager.CurrentState, Is.EqualTo(GameState.Campsite));
            Assert.That(oldStateCaptured, Is.EqualTo(GameState.Exploration));
            Assert.That(newStateCaptured, Is.EqualTo(GameState.Campsite));

            // Campsite -> Exploration
            bool toExplore = _gameManager.ChangeState(GameState.Exploration);
            Assert.That(toExplore, Is.True);

            // Exploration -> Combat
            bool toCombat = _gameManager.ChangeState(GameState.Combat);
            Assert.That(toCombat, Is.True);

            // Combat -> Campsite (party wipeout reload)
            bool toCampReload = _gameManager.ChangeState(GameState.Campsite);
            Assert.That(toCampReload, Is.True);
        }

        [Test]
        public void GameManager_InvalidTransition_ReturnsFalseAndRejects()
        {
            // Exploration -> Campsite
            _gameManager.ChangeState(GameState.Campsite);

            // Campsite -> Combat is invalid directly
            bool invalid = _gameManager.ChangeState(GameState.Combat);
            Assert.That(invalid, Is.False);
            Assert.That(_gameManager.CurrentState, Is.EqualTo(GameState.Campsite));
        }

        [Test]
        public void GameManager_PauseAndResume_RestoresState()
        {
            var tm = new TimeManager();
            _gameManager.TimeManager = tm;
            _gameManager.ChangeState(GameState.Dialogue);

            _gameManager.PauseGame();
            Assert.That(_gameManager.CurrentState, Is.EqualTo(GameState.Paused));
            Assert.That(tm.IsPaused, Is.True);

            _gameManager.ResumeGame();
            Assert.That(_gameManager.CurrentState, Is.EqualTo(GameState.Dialogue));
            Assert.That(tm.IsPaused, Is.False);
        }

        [Test]
        public void GameManager_RegisterAndGetSystem_ReturnsRegisteredInstance()
        {
            var tm = new TimeManager();
            _gameManager.RegisterSystem(tm);

            var retrieved = _gameManager.GetSystem<TimeManager>();
            Assert.That(retrieved, Is.SameAs(tm));
            Assert.That(_gameManager.TimeManager, Is.SameAs(tm));
        }
    }
}
#endif
