using System;
using System.Net.Sockets;
using MyGameServer.Models;

public class Session
{
    public int SessionId { get; set; }
    public Socket Socket { get; set; }
    private byte[] _recvBuffer = new byte[1024 * 8];

    // 클라이언트로부터 데이터가 올 때 실행됨
    public void Start()
    {
        try
        {
            Socket.BeginReceive(_recvBuffer, 0, _recvBuffer.Length, SocketFlags.None, OnReceive, null);
        }
        catch (Exception e)
        {
            Console.WriteLine($"Session Start Error: {e.Message}");
        }
    }

    private void OnReceive(IAsyncResult ar)
    {
        try
        {
            int recvLen = Socket.EndReceive(ar);
            if (recvLen <= 0)
            {
                Disconnect();
                return;
            }

            // 받은 바이트 데이터를 패킷으로 변환하도록 핸들러에 전달
            OnReceivePacket(new ArraySegment<byte>(_recvBuffer, 0, recvLen));

            // 다시 받을 준비
            Socket.BeginReceive(_recvBuffer, 0, _recvBuffer.Length, SocketFlags.None, OnReceive, null);
        }
        catch (Exception)
        {
            Disconnect();
        }
    }

    public void OnReceivePacket(ArraySegment<byte> buffer)
    {
        ushort size = BitConverter.ToUInt16(buffer.Array, buffer.Offset);
        ushort id = BitConverter.ToUInt16(buffer.Array, buffer.Offset + 2);

        switch ((PacketId)id)
        {
            case PacketId.C_Move:
                C_Move movePacket = new C_Move();
                movePacket.Read(buffer);
                PacketHandler.C_MoveHandler(this, movePacket);
                break;
        }
    }

    public void Send(ArraySegment<byte> sendBuff)
    {
        Socket.Send(sendBuff.Array, sendBuff.Offset, sendBuff.Count, SocketFlags.None);
    }

    public void Disconnect()
    {
        if (Socket == null) return;

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