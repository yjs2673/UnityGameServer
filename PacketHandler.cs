using System;
using MyGameServer.Models;

public class PacketHandler
{
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
            rotY = movePacket.rotY,
            isRun = movePacket.isRun,
            isWalk = movePacket.isWalk,
            isJump = movePacket.isJump,
            isDodge = movePacket.isDodge,
            colorIndex = movePacket.colorIndex
        };

        // Console.WriteLine($"Broadcasting Move from Player {session.SessionId}");

        // GameRoom 혹은 ChatServer의 브로드캐스트 로직 호출
        // session을 인자로 넘겨 '나'를 제외하고 보낼 수 있게
        GameRoom.Instance.Broadcast(res.Write(), session);
    }
}