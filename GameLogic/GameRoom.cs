using System.Collections.Generic;

public class GameRoom
{
    public static GameRoom Instance { get; } = new GameRoom();
    private List<Session> _sessions = new List<Session>();
    private object _lock = new object();

    public void Enter(Session session)
    {
        lock (_lock)
        {
            _sessions.Add(session);

            // 새로 들어온 유저(session)에게 기존에 있던 사람들의 정보를 받기
            /* foreach (Session other in _sessions)
            {
                if (other == session) continue;

                S_Move spawnPkt = new S_Move();
                spawnPkt.playerId = other.SessionId;
                
                // other 세션에 저장된 최신 좌표가 있다면 넣기 (없으면 0,0,0)
                session.Send(spawnPkt.Write());
            } */
        }
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