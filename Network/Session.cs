using System;
using System.Net.Sockets;
using MyGameServer.Models;

// 클라이언트와의 연결을 관리하는 세션 클래스
public class Session
{
    public int SessionId { get; set; }  // 고유 세션 ID (접속할 때 매니저에서 생성하여 할당)
    public int UserId { get; set; }     // DB의 유저 ID (로그인 패킷 처리 시 할당)
    public Socket? Socket { get; set; } // 클라이언트와의 소켓 연결
    private byte[] _recvBuffer = new byte[1024 * 8]; // 수신 버퍼

    // 클라이언트로부터 데이터가 올 때 실행
    public void Start()
    {
        try
        {
            // 데이터 수신 시작
            Socket?.BeginReceive(_recvBuffer, 0, _recvBuffer.Length, SocketFlags.None, OnReceive, null);
        }
        catch (Exception e)
        {
            Console.WriteLine($"Session Start Error: {e.Message}");
        }
    }

    // 데이터 수신 콜백
    private void OnReceive(IAsyncResult ar)
    {
        try
        {
            int recvLen = Socket?.EndReceive(ar) ?? 0; // 수신된 데이터 길이
            if (recvLen <= 0) // 데이터 길이가 없다: 클라이언트가 연결을 끊었거나 오류 발생
            {
                Disconnect();
                return;
            }

            // 받은 바이트 데이터를 패킷으로 변환하도록 핸들러에 전달
            OnReceivePacket(new ArraySegment<byte>(_recvBuffer, 0, recvLen));
            // 다시 받을 준비
            Socket?.BeginReceive(_recvBuffer, 0, _recvBuffer.Length, SocketFlags.None, OnReceive, null);
        }
        catch (Exception)
        {
            Disconnect();
        }
    }

    // 패킷 처리 메서드: 받은 데이터를 패킷으로 해석하여 핸들러에 전달
    public void OnReceivePacket(ArraySegment<byte> buffer)
    {
        if (buffer.Array == null)
            return;

        ushort size = BitConverter.ToUInt16(buffer.Array, buffer.Offset);
        ushort id = BitConverter.ToUInt16(buffer.Array, buffer.Offset + 2);

        switch ((PacketId)id) // 패킷 ID에 따른 핸들러 호출
        {
            case PacketId.C_Login:
                C_Login loginPacket = new C_Login();
                loginPacket.Read(buffer);
                PacketHandler.C_LoginHandler(this, loginPacket);
                break;
            case PacketId.C_Move:
                C_Move movePacket = new C_Move();
                movePacket.Read(buffer);
                PacketHandler.C_MoveHandler(this, movePacket);
                break;
            case PacketId.C_PickUpItem:
                C_PickUpItem pickPkt = new C_PickUpItem();
                pickPkt.Read(buffer);
                PacketHandler.C_PickUpItemHandler(this, pickPkt);
                break;
        }
    }

    // 서버에서 클라이언트로 데이터를 보내는 메서드
    public void Send(ArraySegment<byte> sendBuff)
    {
        if (Socket == null)
            return;
        if (sendBuff.Array == null)
            return;

        // 데이터를 소켓을 통해 클라이언트로 전송
        Socket.Send(sendBuff.Array, sendBuff.Offset, sendBuff.Count, SocketFlags.None);
    }

    // 클라이언트와의 연결 종료 처리
    public void Disconnect()
    {
        if (Socket == null)
            return;

        // 퇴장 패킷 생성
        S_Leave leavePkt = new S_Leave { playerId = this.SessionId };
        ArraySegment<byte> sendBuff = leavePkt.Write();

        // 나를 제외한 모든 유저에게 leave 알림
        GameRoom.Instance.Broadcast(sendBuff, this);

        Console.WriteLine($"Client Dicsconnected: {SessionId}");

        // 매니저와 룸에서 제거
        SessionManager.Instance.Remove(this);
        GameRoom.Instance.Leave(this);

        // 소켓 자원 해제
        try
        {
            Socket.Shutdown(SocketShutdown.Both);
            Socket.Close();
        }
        catch { }
    }
}