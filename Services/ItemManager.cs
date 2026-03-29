using System;
using MyGameServer.Models;

// 공원 드랍 아이템 정보 클래스
public class ItemInfo
{
    public int ItemDbId;    // 아이템 개체의 고유 ID
    public short ItemType;  // 0: Coin, 1: Exp
    public float PosX, PosZ;
}

// 아이템 매니저 클래스: 아이템 생성 및 획득 관리
public class ItemManager
{
    static ItemManager _instance = new ItemManager();   // 싱글톤 패턴
    public static ItemManager Instance => _instance;    // 싱글톤 인스턴스 접근자

    int _itemCounter = 0; // 아이템 고유 ID 생성용 카운터
    Dictionary<int, ItemInfo> _items = new Dictionary<int, ItemInfo>(); // 현재 존재하는 아이템들 (Key: ItemDbId)
    object _lock = new object();

    // 3초마다 랜덤 위치에 아이템 생성
    public async Task StartSpawnLoop()
    {
        while (true)
        {
            SpawnRandomItem();
            await Task.Delay(3000); // 3초 대기
        }
    }

    // 아이템 생성
    public void SpawnRandomItem()
    {
        lock (_lock)
        {
            ItemInfo item = new ItemInfo
            {
                ItemDbId = ++_itemCounter,
                ItemType = (short)new Random().Next(0, 2), // 0: Coin, 1: Exp
                PosX = (float)(new Random().NextDouble() * 40 - 20),
                PosZ = (float)(new Random().NextDouble() * 40 - 20)
            };

            _items.Add(item.ItemDbId, item);

            // 모든 유저에게 생성 패킷 브로드캐스트
            S_SpawnItem pkt = new S_SpawnItem
            {
                itemDbId = item.ItemDbId,
                itemType = item.ItemType,
                posX = item.PosX,
                posZ = item.PosZ
            };
            Console.WriteLine($"[ItemSpawn] ID:{item.ItemDbId} Type:{item.ItemType} Pos:({item.PosX}, {item.PosZ})");
            GameRoom.Instance.Broadcast(pkt.Write(), null);
        }
    }

    // 획득 판정
    public ItemInfo? PickUpItem(int itemDbId)
    {
        lock (_lock)
        {
            if (_items.Remove(itemDbId, out ItemInfo? item))
                return item; // 성공적으로 획득
                
            return null; // 이미 누가 획득
        }
    }
}