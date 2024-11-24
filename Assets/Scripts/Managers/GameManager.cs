using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Game State")] 
    [SerializeField] private GameState currentState;
    private Dictionary<GameState, State> _gameStateDictionary;
    private Action<State> _onGameStateChange;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        _gameStateDictionary = new Dictionary<GameState, State>()
        {
            { GameState.Base, new BaseState() },
            { GameState.Menu, new MenuState() },
            { GameState.GameplayDuel, new GameplayDuelState() },
        };
        var startState = currentState;
        currentState = GameState.Base;
        ChangeState(startState);
    }

    private void Update()
    {
        _gameStateDictionary[currentState].Update();
    }
    
    public void SetStateToMenu() => ChangeState(GameState.Menu);
    public void SetStateToDuel() => ChangeState(GameState.GameplayDuel);
    public void SetStateToWave() => ChangeState(GameState.GameplayWave);

    
    public void ChangeState(GameState newState)
    {
        if (currentState == newState) return;
        _gameStateDictionary[currentState].Exit();
        currentState = newState;
        _gameStateDictionary[currentState].Enter();
        _onGameStateChange?.Invoke(_gameStateDictionary[currentState]);
    }

    public void ChangeScene(string sceneName)
    {
        SceneManager.LoadScene(sceneName);
    }

    public void AddOnGameStateChangeListener(Action<State> listener)
    {
        _onGameStateChange += listener;
    }
    
    public void RemoveOnGameStateChangeListener(Action<State> listener)
    {
        _onGameStateChange -= listener;
    }
}

public enum GameState
{
    Base,
    // 
    Menu,
    // Gameplay
    GameplayDuel,
    GameplayWave
    
    
}


