using System.Collections.Generic;

// 게임 룸(Park) 관리 클래스
public class GameRoom
{
    public static GameRoom Instance { get; } = new GameRoom(); // 싱글톤 패턴

    private List<Session> _sessions = new List<Session>();
    private object _lock = new object();

    // 유저 입장
    public void Enter(Session session)
    {
        lock (_lock) { _sessions.Add(session); }
    }

    // 유저 퇴장
    public void Leave(Session session)
    {
        lock (_lock) { _sessions.Remove(session); }
    }

    // 나를 제외한 모두에게 패킷 브로드캐스트
    public void Broadcast(ArraySegment<byte> packet, Session? exceptMe)
    {
        lock (_lock)
        {
            foreach (Session s in _sessions)
            {
                if (s != exceptMe)
                    s.Send(packet);
            }
        }
    }
}