
public abstract class State
{
    public GameState StateName { get; protected set; }
    public virtual void Enter() { }
    public virtual void Update() { }
    public virtual void Exit() { }
}
