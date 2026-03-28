using System.Collections.Generic;
using System.Threading;

public class SessionManager
{
    public static SessionManager Instance { get; } = new SessionManager();

    private int _sessionIdCounter = 0;
    private Dictionary<int, Session> _sessions = new Dictionary<int, Session>();
    private object _lock = new object();

    // 고유 세션 ID 생성 및 세션 등록
    public int GenerateId()
    {
        // 여러 스레드에서 동시에 접속해도 안전하게 ID 증가
        return Interlocked.Increment(ref _sessionIdCounter);
    }

    public void Add(Session session)
    {
        lock (_lock)
        {
            _sessions.Add(session.SessionId, session);
        }
    }

    public void Remove(Session session)
    {
        lock (_lock)
        {
            _sessions.Remove(session.SessionId);
        }
    }

    public Session Find(int id)
    {
        lock (_lock)
        {
            Session session = null;
            _sessions.TryGetValue(id, out session);
            return session;
        }
    }
}