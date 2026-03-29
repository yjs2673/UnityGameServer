using System.Net;
using System.Net.Sockets;

// 공원 서버 클래스: TCP 소켓을 사용하여 클라이언트와 통신
public class ParkServer
{
    private Socket? _listenSocket; // TCP 리스너 (클라이언트 접속 대기용)
    private int _port = 8888;      // 공원 서버 포트 번호

    // 서버 시작
    public void Start()
    {
        _listenSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp); // TCP 소켓 생성
        _listenSocket.Bind(new IPEndPoint(IPAddress.Any, _port)); // 모든 네트워크 인터페이스에서 접속 대기
        _listenSocket.Listen(100); // 최대 100개의 대기 큐 설정

        Console.WriteLine($"[ParkServer] 게임 서버 시작 (Port: {_port})");

        _ = ItemManager.Instance.StartSpawnLoop(); // 비동기로 아이템 생성 루프 시작

        AcceptLoop();
    }

    // 비동기 클라이언트 접속 대기 루프
    private void AcceptLoop()
    {
        _listenSocket?.BeginAccept(OnAccept, null);
    }

    // 클라이언트 접속 처리 콜백
    private void OnAccept(IAsyncResult ar)
    {
        try
        {
            if (_listenSocket == null)
                return;

             // 클라이언트 소켓 완성
            Socket clientSocket = _listenSocket.EndAccept(ar);

            // 세션 생성 및 등록
            Session session = new Session { Socket = clientSocket };
            session.SessionId = SessionManager.Instance.GenerateId();
            SessionManager.Instance.Add(session);

            // 접속하자마자 본인에게 ID 패킷 전송
            S_Login loginPkt = new S_Login { playerId = session.SessionId };
            session.Send(loginPkt.Write());

            // Park(공원) 룸에 입장시킴
            GameRoom.Instance.Enter(session);

            // 데이터 수신 시작
            session.Start();

            Console.WriteLine($"[ParkServer] 유저 입장: {session.SessionId}");
            AcceptLoop();
        }
        catch (Exception e)
        {
            Console.WriteLine($"Accept Error: {e.Message}");
        }
    }
}