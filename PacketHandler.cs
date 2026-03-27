using System;
using MyGameServer.Models;
// using MyGameServer.Network; // Session 클래스가 있는 곳을 참조하세요.

public class PacketHandler
{
    // 핸들러 함수를 static으로 관리
    public static void C_MoveHandler(Session session, IPacket packet)
    {
        C_Move movePacket = packet as C_Move;
        if (movePacket == null || session == null) return;

        // S_Move 패킷 생성 (브로드캐스트용)
        S_Move res = new S_Move
        {
            playerId = session.SessionId, 
            posX = movePacket.posX,
            posY = movePacket.posY,
            posZ = movePacket.posZ,
            rotY = movePacket.rotY
        };

        // GameRoom 혹은 ChatServer의 브로드캐스트 로직 호출
        // session을 인자로 넘겨 '나'를 제외하고 보낼 수 있게 설계합니다.
        GameRoom.Instance.Broadcast(res.Write(), session);
    }
}