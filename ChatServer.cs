using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

public class ChatServer
{
    private TcpListener? _listener;
    // 접속 중인 클라이언트 세션 매핑 (Key: 유저 고유 ID)
    private Dictionary<int, TcpClient> _clients = new Dictionary<int, TcpClient>();
    private readonly int _port = 7777;

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
        NetworkStream stream = client.GetStream();
        byte[] buffer = new byte[1024];

        try
        {
            while (true)
            {
                int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);
                if (bytesRead == 0) break; // 연결 종료

                string message = Encoding.UTF8.GetString(buffer, 0, bytesRead);

                Console.WriteLine($"[ChatServer] 수신된 데이터: {message}");
                
                // 간이 패킷 프로토콜
                // 처음 접속 시 "ID:5" 형식으로 등록
                if (message.StartsWith("ID:"))
                {
                    myUserId = int.Parse(message.Split(':')[1]);
                    lock (_clients) { _clients[myUserId] = client; }
                    Console.WriteLine($"[ChatServer] 유저 {myUserId}번 접속 성공");
                    continue;
                }

                // 모든 클라이언트에게 전송
                Console.WriteLine($"[ChatServer] {myUserId}: {message}");
                await Broadcast(message);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Error] {ex.Message}");
        }
        finally
        {
            // 연결 종료
            if (myUserId != 0) lock (_clients) { _clients.Remove(myUserId); }
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