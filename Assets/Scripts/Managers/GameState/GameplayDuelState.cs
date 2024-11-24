
public class GameplayDuelState : State
{
    public override void Enter()
    {
        GameManager.Instance.ChangeScene("Gameplay-Duel");
    }
}
