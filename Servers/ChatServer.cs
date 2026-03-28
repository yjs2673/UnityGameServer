using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Distributed;

public class ChatServer
{
    private readonly IDistributedCache _cache;
    private TcpListener? _listener;
    // 접속 중인 클라이언트 세션 매핑 (Key: 유저 고유 ID)
    private Dictionary<int, TcpClient> _clients = new Dictionary<int, TcpClient>();
    private readonly int _port = 7777;

    // 생성자를 통해 Program.cs에서 등록된 Redis 캐시를 주입
    public ChatServer(IDistributedCache cache)
    {
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));
        Console.WriteLine("[ChatServer] Redis 캐시가 정상적으로 주입되었습니다.");
    }

    public async Task Start()
    {
        _listener = new TcpListener(IPAddress.Any, _port);
        _listener.Start();
        Console.WriteLine($"[ChatServer] 소켓 서버 시작 (Port: {_port})");

        while (true)
        {
            // 클라이언트 접속 대기 (비동기 처리)
            TcpClient client = await _listener.AcceptTcpClientAsync();
            _ = HandleClient(client); // 비동기로 개별 클라이언트 처리
        }
    }

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
                int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);
                if (bytesRead == 0) break; // 연결 종료

                string message = Encoding.UTF8.GetString(buffer, 0, bytesRead);

                Console.WriteLine($"[ChatServer] 수신된 데이터: {message}");

                // 간이 패킷 프로토콜
                if (message.StartsWith("ID:"))
                {
                    var parts = message.Split(':');

                    myUserId = int.Parse(message.Split(':')[1]);
                    myNickname = parts.Length > 2 ? parts[2] : $"User {myUserId}";

                    lock (_clients) { _clients[myUserId] = client; }

                    // --- Redis 중복 문제 해결 ---
                    string loginKey = $"login_status:{myUserId}";

                    // 키를 새로 생성하는 게 아니라 동일한 키에 현재 소켓 연결 상태를 기록/갱신
                    await _cache.SetStringAsync(loginKey, "Connected", new DistributedCacheEntryOptions
                    {
                        AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1) // 세션 유지 시간
                    });

                    await Broadcast($"<color=cyan>[시스템] {myNickname}님이 입장하셨습니다.</color>");
                    continue;
                }

                // 모든 클라이언트에게 전송
                Console.WriteLine($"[ChatServer] {myUserId}: {message}");
                await Broadcast(message);
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
                lock (_clients) { _clients.Remove(myUserId); }

                try
                {
                    // 중복 로그인 해제
                    string loginKey = $"login_status:{myUserId}";

                    // 삭제 시도
                    Console.WriteLine($"[ChatServer] Redis 키 삭제 시도: {loginKey}");

                    await _cache.RemoveAsync(loginKey);

                    // 삭제 성공
                    Console.WriteLine($"[ChatServer] Redis 키 삭제 완료: {loginKey}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[ChatServer] Redis 삭제 중 에러: {ex.Message}");
                }

                await Broadcast($"<color=orange>[시스템] {myNickname}님이 퇴장하셨습니다.</color>");
            }
            client.Close();
        }
    }

    private async Task Broadcast(string message)
    {
        byte[] data = Encoding.UTF8.GetBytes(message);
        List<Task> sendTasks = new List<Task>();

        lock (_clients)
        {
            foreach (var client in _clients.Values) // 브로드캐스팅
            {
                sendTasks.Add(client.GetStream().WriteAsync(data, 0, data.Length));
            }
        }
        await Task.WhenAll(sendTasks);
    }
}