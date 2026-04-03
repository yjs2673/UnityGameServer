using System;
using System.Net.Sockets;
using System.Collections.Generic;
using MyGameServer.Models;

// 클라이언트와의 연결을 관리하는 세션 클래스
public class Session
{
    public int SessionId { get; set; }      // 고유 세션 ID (접속할 때 매니저에서 생성하여 할당)
    public int UserId { get; set; }         // DB의 유저 ID (로그인 패킷 처리 시 할당)
    public string? Nickname { get; set; }   // 유저 닉네임 (로그인 패킷 처리 시 DB에서 조회하여 할당)
    public Socket? Socket { get; set; }     // 클라이언트와의 소켓 연결
    private byte[] _recvBuffer = new byte[1024 * 64]; // 수신 버퍼

    private int _recvBytes = 0; // 현재 버퍼에 쌓인 데이터 양

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
            // 1. 소켓으로부터 데이터를 읽음
            int bytesRead = Socket?.EndReceive(ar) ?? 0;
            if (bytesRead <= 0)
            {
                Disconnect();
                return;
            }

            // 수신된 바이트 수를 누적
            _recvBytes += bytesRead;
            int processPos = 0;

            while (true)
            {
                // [에러 해결] 최소 헤더(4바이트)가 모였는지 확인
                if (_recvBytes - processPos < 4) break;

                // 헤더에서 패킷 사이즈 읽기 (Size는 2바이트 ushort)
                ushort size = BitConverter.ToUInt16(_recvBuffer, processPos);

                // [핵심] 패킷 전체 데이터가 아직 다 안 왔으면 다음 수신을 기다림 (루프 탈출)
                if (_recvBytes - processPos < size) break;

                // [에러 해결] OnProcessPacket 대신 질문자님의 패킷 처리 함수 이름으로 변경
                // 질문자님의 코드에서는 'OnReceivePacket'일 확률이 높습니다.
                ArraySegment<byte> packetSegment = new ArraySegment<byte>(_recvBuffer, processPos, size);
                OnReceivePacket(packetSegment); 

                processPos += size;
            }

            // 3. 처리한 패킷만큼 버퍼에서 제거하고 남은 데이터를 앞으로 밀기
            if (processPos > 0)
            {
                int remaining = _recvBytes - processPos;
                if (remaining > 0)
                {
                    Array.Copy(_recvBuffer, processPos, _recvBuffer, 0, remaining);
                }
                _recvBytes = remaining;
            }

            // 4. [에러 해결] 다음 수신을 위해 BeginReceive 호출
            // 버퍼의 비어있는 공간(_recvBytes 위치부터)에 데이터를 채우도록 설정
            Socket?.BeginReceive(_recvBuffer, _recvBytes, _recvBuffer.Length - _recvBytes, SocketFlags.None, OnReceive, null);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Receive Error: {ex.Message}");
            Disconnect();
        }
    }

    // 패킷 처리 메서드: 받은 데이터를 패킷으로 해석하여 핸들러에 전달
    public void OnReceivePacket(ArraySegment<byte> buffer)
    {
        if (buffer.Array == null)
            return;
        if (buffer.Count < 4)       // 최소 헤더 크기 체크
            return;

        try
        {
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
                case PacketId.C_Voice:
                    C_Voice voicePacket = new C_Voice();
                    voicePacket.Read(buffer);
                    PacketHandler.C_VoiceHandler(this, voicePacket);
                    break;
            }
        }
        catch (Exception ex) // 패킷 파싱 중 예외 발생 시 로그 출력
        {
            Console.WriteLine($"Packet Parse Error: {ex.Message}");
        }
    }

    // 서버에서 클라이언트로 데이터를 보내는 메서드
    public void Send(ArraySegment<byte> sendBuff)
    {
        if (Socket == null || !Socket.Connected)    // 소켓 상태 체크 추가
            return;

        if (sendBuff.Array == null)                 // 버퍼 유효성 체크
            return;

        try
        {
            // 소켓을 통해 데이터를 전송
            Socket.Send(sendBuff.Array, sendBuff.Offset, sendBuff.Count, SocketFlags.None);
        }
        catch (SocketException ex)
        {
            Console.WriteLine($"Send Error ({ex.NativeErrorCode}): {ex.Message}");
            Disconnect(); 
        }
        catch (Exception e)
        {
            Console.WriteLine($"Unexpected Send Error: {e.Message}");
        }
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