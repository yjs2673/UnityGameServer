using System;
using MyGameServer.Models;

public class ItemInfo
{
    public int ItemDbId;
    public short ItemType; // 0: Coin, 1: Exp
    public float PosX, PosZ;
}

public class ItemManager {
    static ItemManager _instance = new ItemManager();
    public static ItemManager Instance => _instance;

    int _itemCounter = 0;
    Dictionary<int, ItemInfo> _items = new Dictionary<int, ItemInfo>();
    object _lock = new object();

    public async Task StartSpawnLoop()
    {
        while (true)
        {
            SpawnRandomItem();
            await Task.Delay(2000); // 2초 대기
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

    // 습득 판정
    public ItemInfo PickUpItem(int itemDbId) {
        lock (_lock) {
            if (_items.Remove(itemDbId, out ItemInfo item)) {
                return item; // 성공적으로 먹음
            }
            return null; // 이미 누가 먹었음
        }
    }
}