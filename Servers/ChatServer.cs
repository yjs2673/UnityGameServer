using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Distributed;

// 채팅 서버 클래스: TCP 소켓을 사용하여 클라이언트와 통신
public class ChatServer
{
    private readonly IDistributedCache _cache;  // Redis 캐시 (로그인 상태 관리용)
    private TcpListener? _listener;             // TCP 리스너 (클라이언트 접속 대기용)
    private Dictionary<int, TcpClient> _clients = new Dictionary<int, TcpClient>(); // 접속 중인 클라이언트 세션 매핑 (Key: 유저 고유 ID)
    private readonly int _port = 7777;          // 채팅 서버 포트 번호

    // 생성자: Redis 캐시 주입
    public ChatServer(IDistributedCache cache)
    {
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));
        Console.WriteLine("[ChatServer] Redis 캐시가 정상적으로 주입되었습니다.");
    }

    // 채팅 서버 시작
    public async Task Start()
    {
        _listener = new TcpListener(IPAddress.Any, _port); // 모든 네트워크 인터페이스에서 접속 대기
        _listener.Start(); // TCP 리스너 시작
        Console.WriteLine($"[ChatServer] 소켓 서버 시작 (Port: {_port})");

        while (true)
        {
            // 클라이언트 접속 대기: 비동기로 개별 클라이언트 처리
            TcpClient client = await _listener.AcceptTcpClientAsync();
            _ = HandleClient(client);
        }
    }

    // 개별 클라이언트 처리: 로그인 정보 수신 및 메시지 전송
    private async Task HandleClient(TcpClient client)
    {
        int myUserId = 0;
        string myNickname = "Unknown";
        NetworkStream? stream = null;

        try
        {
            stream = client.GetStream();
            byte[] buffer = new byte[1024];

            while (true)
            {
                 // 클라이언트로부터 데이터 수신 (비동기)
                int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);
                if (bytesRead == 0)
                    break; // 연결 종료

                 // 수신된 메시지를 문자열로 변환
                string message = Encoding.UTF8.GetString(buffer, 0, bytesRead);

                Console.WriteLine($"[ChatServer] 수신된 데이터: {message}");

                // 간이 패킷 프로토콜
                if (message.StartsWith("ID:"))
                {
                    var parts = message.Split(':');

                    myUserId = int.Parse(message.Split(':')[1]);
                    myNickname = parts.Length > 2 ? parts[2] : $"User {myUserId}";

                    // 클라이언트 세션 저장 (유저 ID 기준)
                    lock (_clients) { _clients[myUserId] = client; }

                    // Redis 중복 방지 위해 로그인 상태 등록
                    string loginKey = $"login_status:{myUserId}";

                    // 키를 새로 생성하는 게 아니라 동일한 키에 현재 소켓 연결 상태를 기록/갱신
                    await _cache.SetStringAsync(loginKey, "Connected", new DistributedCacheEntryOptions
                    {
                        AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1) // 세션 유지 시간
                    });

                    await SendMessage($"<color=cyan>[시스템] {myNickname}님이 입장하셨습니다.</color>");
                    continue;
                }

                // 일반 메시지 전송
                Console.WriteLine($"[ChatServer] {myUserId}: {message}");
                await SendMessage(message);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ChatServer] 유저 {myNickname} 연결 강제 종료: {ex.Message}");
        }
        finally
        {
            // 연결 종료
            if (myUserId != 0)
            {
                // 클라이언트 세션 제거
                lock (_clients) { _clients.Remove(myUserId); }

                try
                {
                    // 중복 로그인 해제
                    string loginKey = $"login_status:{myUserId}";

                    // Redis에서 로그인 상태 제거 (키 삭제)
                    Console.WriteLine($"[ChatServer] Redis 키 삭제 시도: {loginKey}");
                    await _cache.RemoveAsync(loginKey);

                    // 삭제 성공
                    Console.WriteLine($"[ChatServer] Redis 키 삭제 완료: {loginKey}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[ChatServer] Redis 삭제 중 에러: {ex.Message}");
                }

                // 퇴장 메시지 전송
                await SendMessage($"<color=orange>[시스템] {myNickname}님이 퇴장하셨습니다.</color>");
            }

            // 소켓 연결 종료
            client.Close();
        }
    }

    // 모든 클라이언트에게 메시지 전송
    private async Task SendMessage(string message)
    {
        byte[] data = Encoding.UTF8.GetBytes(message);  // 메시지를 바이트 배열로 변환
        List<Task> sendTasks = new List<Task>();        // 모든 클라이언트에게 메시지 전송 (비동기)

        lock (_clients) // 클라이언트 목록에 대한 동기화된 접근
        {
            // 각 클라이언트의 네트워크 스트림에 메시지 전송 (비동기)
            foreach (var client in _clients.Values)
                sendTasks.Add(client.GetStream().WriteAsync(data, 0, data.Length));
        }

        // 모든 전송 작업이 완료될 때까지 대기
        await Task.WhenAll(sendTasks);
    }
}