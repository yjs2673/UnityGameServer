using System.Collections.Generic;

public class GameRoom
{
    public static GameRoom Instance { get; } = new GameRoom();
    private List<Session> _sessions = new List<Session>();
    private object _lock = new object();

    public void Enter(Session session)
    {
        lock (_lock) { _sessions.Add(session); }
    }

    public void Leave(Session session)
    {
        lock (_lock) { _sessions.Remove(session); }
    }

    // 나를 제외한 모두에게 보낼 때 사용
    public void Broadcast(ArraySegment<byte> packet, Session excludeSelf)
    {
        lock (_lock)
        {
            foreach (Session s in _sessions)
            {
                if (s != excludeSelf)
                    s.Send(packet);
            }
        }
    }
}