using System.Collections.Generic;
using System.Threading;

// 세션 관리 클래스: 접속한 클라이언트 세션을 관리하고 고유 ID를 생성
public class SessionManager
{
    // 싱글톤 인스턴스: 프로그램 전체에서 하나의 세션 매니저만 존재
    public static SessionManager Instance { get; } = new SessionManager();

    private int _sessionIdCounter = 0;      // 고유 세션 ID 생성용 카운터
    private Dictionary<int, Session> _sessions = new Dictionary<int, Session>(); // 세션 ID -> Session 객체 매핑
    private object _lock = new object();    // 세션 딕셔너리에 대한 동기화 객체

    // 고유 세션 ID 생성 및 세션 등록
    public int GenerateId()
    {
        // 여러 스레드에서 동시에 접속해도 안전하게 ID 증가
        return Interlocked.Increment(ref _sessionIdCounter);
    }

    // 세션 등록
    public void Add(Session session)
    {
        lock (_lock)
        {
            _sessions.Add(session.SessionId, session);
        }
    }

    // 세션 제거
    public void Remove(Session session)
    {
        lock (_lock)
        {
            _sessions.Remove(session.SessionId);
        }
    }

    // 세션 조회
    public Session? Find(int id)
    {
        lock (_lock)
        {
            Session? session = null;
            _sessions.TryGetValue(id, out session);
            return session;
        }
    }
}