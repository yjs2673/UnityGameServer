using System;
using MyGameServer.Models;
using MyGameServer.Controllers;

// 패킷 핸들러 클래스: 클라이언트로부터 받은 패킷을 처리하는 메서드들을 정의
public class PacketHandler
{
    public static void C_LoginHandler(Session session, IPacket packet)
    {
        C_Login? loginPacket = packet as C_Login;
        if (loginPacket == null)
            return;
        if (session == null)
            return;

        // 세션 객체의 UserId에 클라이언트가 보낸 실제 DB ID를 할당
        session.UserId = loginPacket.userId;
        Console.WriteLine($"[Login] Session {session.SessionId} is now mapped to User {session.UserId}");
    }

    public static void C_MoveHandler(Session session, IPacket packet)
    {
        C_Move? movePacket = packet as C_Move;
        if (movePacket == null)
            return;
        if (session == null)
            return;

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

        // 자신 제외 모든 클라이언트에게 움직임 브로드캐스트
        GameRoom.Instance.Broadcast(res.Write(), session);
    }

    public static async void C_PickUpItemHandler(Session session, IPacket packet)
    {
        C_PickUpItem? pickPkt = packet as C_PickUpItem;
        if (pickPkt == null)
            return;

        // 아이템 매니저 검증
        ItemInfo? info = ItemManager.Instance.PickUpItem(pickPkt.itemDbId);
        if (info == null) // 이미 누가 먹었거나 없는 아이템
            return; 

        // 모든 클라이언트에게 아이템 제거 브로드캐스트
        S_DespawnItem despawn = new S_DespawnItem { itemDbId = info.ItemDbId };
        GameRoom.Instance.Broadcast(despawn.Write(), null);

        // DB 반영: 소켓 세션에 저장된 UserId를 사용하여 DB에서 유저를 찾기
        using (AppDbContext db = new AppDbContext()) // DB 컨텍스트 생성
        {
            var user = await db.Users.FindAsync(session.UserId);
            if (user == null)
                return;

            if (info.ItemType == 0)
                user.Gold += 10;
            else
            {
                user.Exp += 1;
                while (user.Exp >= 100)
                {
                    user.Exp -= 100;
                    user.Level += 1;
                }
            }

            // DB에 변경사항 저장
            await db.SaveChangesAsync();

            // 아이템을 먹은 당사자에게만 최신 데이터 전송
            S_StatUpdate stat = new S_StatUpdate
            {
                gold = user.Gold,
                exp = user.Exp
            };
            session.Send(stat.Write());
        }
    }
}