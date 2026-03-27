using System.Net;
using System.Net.Sockets;

public class ParkServer
{
    private Socket _listenSocket;
    private int _port = 8888; // 채팅(7777)과 다른 포트 사용 권장

    public void Start()
    {
        _listenSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        _listenSocket.Bind(new IPEndPoint(IPAddress.Any, _port));
        _listenSocket.Listen(100);

        Console.WriteLine($"[ParkServer] 게임 서버 시작 (Port: {_port})");
        AcceptLoop();
    }

    private void AcceptLoop()
    {
        _listenSocket.BeginAccept(OnAccept, null);
    }

    private void OnAccept(IAsyncResult ar)
    {
        try
        {
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
        catch (Exception e) { Console.WriteLine($"Accept Error: {e.Message}"); }
    }
}