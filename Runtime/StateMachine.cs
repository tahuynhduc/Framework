using System.Collections.Generic;
using UnityEngine;

public class StateMachine<T, TEState> where TEState : System.Enum 
{
    private readonly Dictionary<TEState, IState<T>> _stateCache = new();
    private readonly T _owner;

    public IState<T> CurrentState { get; private set; }
    public TEState CurrentId { get; private set; }

    public StateMachine(T owner)
    {
        _owner = owner;
    }

    public void Register(TEState id, IState<T> state)
    {
        _stateCache.TryAdd(id, state);
    }

    public void ChangeState(TEState id)
    {
        ExitState(id);
        EnterState(id);
    }

    public void ExitState(TEState id)
    {
        if (!_stateCache.TryGetValue(id, out IState<T> nextState))
        {
            Debug.LogWarning($"[FSM] State '{id}' chưa được đăng ký trong máy của {_owner}!");
            return;
        }

        if (EqualityComparer<TEState>.Default.Equals(CurrentId, id) && CurrentState != null) 
            return;

        CurrentState?.Exit(_owner);
    }

    public void EnterState(TEState id)
    {
        if (!_stateCache.TryGetValue(id, out IState<T> nextState))
        {
            Debug.LogWarning($"[FSM] State '{id}' chưa được đăng ký trong máy của {_owner}!");
            return;
        }

        if (EqualityComparer<TEState>.Default.Equals(CurrentId, id) && CurrentState != null) 
            return;
        
        CurrentState = nextState;
        CurrentId = id;

        CurrentState.Enter(_owner);
    }
    
    public void Update(float deltaTime)
    {
        CurrentState?.Execute(_owner, deltaTime);
    }
    
    public bool TryGetState<TState>(out TState concreteState) where TState : class
    {
        if (CurrentState is TState castedState)
        {
            concreteState = castedState;
            return true;
        }

        concreteState = null;
        return false;
    }
}

public interface IState<T>
{
    void Enter(T owner);
    void Execute(T owner, float deltaTime);
    void Exit(T owner);
}