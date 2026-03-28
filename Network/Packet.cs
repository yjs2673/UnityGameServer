public enum PacketId : ushort
{
    C_Move = 1,
    S_Move = 2,
    C_Login = 3,        // 유저 ID
    S_Login = 4,
    S_Leave = 5,
    S_SpawnItem = 6,    // 아이템 생성 (서버 -> 클라)
    C_PickUpItem = 7,   // 아이템 습득 시도 (클라 -> 서버)
    S_DespawnItem = 8,  // 아이템 제거 (서버 -> 클라)
    S_StatUpdate = 9   // 골드/경험치 수치 업데이트 (서버 -> 클라)
}

public interface IPacket
{
    ushort Protocol { get; }
    void Read(ArraySegment<byte> segment);
    ArraySegment<byte> Write();
}

public class SendBufferHelper
{
    // 스레드별로 별도의 버퍼를 사용하여 안전하게 관리
    public static ThreadLocal<byte[]> CurrentBuffer = new ThreadLocal<byte[]>(() => { return new byte[65535]; });
    public static int UsedSize = 0;

    public static ArraySegment<byte> Open(int reserveSize)
    {
        return new ArraySegment<byte>(CurrentBuffer.Value, UsedSize, reserveSize);
    }

    public static ArraySegment<byte> Close(int usedSize)
    {
        ArraySegment<byte> segment = new ArraySegment<byte>(CurrentBuffer.Value, UsedSize, usedSize);
        UsedSize += usedSize; // 일정 크기가 차면 초기화하는 로직 필요
        
        if (UsedSize > 60000) UsedSize = 0;
        return segment;
    }
}

public class C_Move : IPacket
{
    public ushort Protocol => (ushort)PacketId.C_Move;
    // --- 캐릭터 위치 ---
    public float posX, posY, posZ;
    public float rotY;
    // --- 애니메이션 상태 ---
    public bool isRun;
    public bool isWalk;
    public bool isJump;
    public bool isDodge;
    // --- 캐릭터 색 ---
    public int colorIndex;

    // 역직렬화: 바이트 배열에서 데이터를 뽑아내 변수에 저장 (서버가 받음)
    public void Read(ArraySegment<byte> segment)
    {
        ReadOnlySpan<byte> s = new ReadOnlySpan<byte>(segment.Array, segment.Offset, segment.Count);
        int count = 0;

        // 헤더(Size, PacketId) 이후부터 읽기
        count += sizeof(ushort);
        count += sizeof(ushort);

        this.posX = BitConverter.ToSingle(s.Slice(count)); count += sizeof(float);
        this.posY = BitConverter.ToSingle(s.Slice(count)); count += sizeof(float);
        this.posZ = BitConverter.ToSingle(s.Slice(count)); count += sizeof(float);
        this.rotY = BitConverter.ToSingle(s.Slice(count)); count += sizeof(float);
        this.isRun = s[count] != 0; count += 1;
        this.isWalk = s[count] != 0; count += 1;
        this.isJump = s[count] != 0; count += 1;
        this.isDodge = s[count] != 0; count += 1;
        this.colorIndex = BitConverter.ToInt32(s.Slice(count)); count += 4;
    }

    // 직렬화: 변수의 데이터를 바이트 배열로 변환 (클라이언트가 보냄)
    public ArraySegment<byte> Write()
    {
        ArraySegment<byte> segment = SendBufferHelper.Open(4096);
        ushort count = 0;
        bool success = true;

        Span<byte> s = new Span<byte>(segment.Array, segment.Offset, segment.Count);

        // Size
        ushort sizePlaceholder = 0;
        success &= BitConverter.TryWriteBytes(s.Slice(count), sizePlaceholder);
        count += sizeof(ushort);

        // Protocol
        ushort protocol = (ushort)PacketId.C_Move;
        success &= BitConverter.TryWriteBytes(s.Slice(count), protocol);
        count += sizeof(ushort);

        // Floats
        success &= BitConverter.TryWriteBytes(s.Slice(count), this.posX); count += 4;
        success &= BitConverter.TryWriteBytes(s.Slice(count), this.posY); count += 4;
        success &= BitConverter.TryWriteBytes(s.Slice(count), this.posZ); count += 4;
        success &= BitConverter.TryWriteBytes(s.Slice(count), this.rotY); count += 4;

        // Bools
        s[count] = (byte)(this.isRun ? 1 : 0); count += 1;
        s[count] = (byte)(this.isWalk ? 1 : 0); count += 1;
        s[count] = (byte)(this.isJump ? 1 : 0); count += 1;
        s[count] = (byte)(this.isDodge ? 1 : 0); count += 1;

        // Ints
        BitConverter.TryWriteBytes(s.Slice(count), this.colorIndex); count += 4;

        // 전체 패킷 Size 기록
        success &= BitConverter.TryWriteBytes(s.Slice(0), (ushort)count);

        if (!success) return null;
        return SendBufferHelper.Close(count);
    }
}

public class S_Move : IPacket
{
    public ushort Protocol => (ushort)PacketId.S_Move;
    // --- 캐릭터 위치 ---
    public int playerId;
    public float posX, posY, posZ;
    public float rotY;
    // --- 애니메이션 상태 ---
    public bool isRun;
    public bool isWalk;
    public bool isJump;
    public bool isDodge;
    // --- 캐릭터 색 ---
    public int colorIndex;

    // 역직렬화: 바이트 배열에서 데이터를 뽑아내 변수에 저장 (서버가 받음)
    public void Read(ArraySegment<byte> segment)
    {
        ReadOnlySpan<byte> s = new ReadOnlySpan<byte>(segment.Array, segment.Offset, segment.Count);
        int count = 0;

        // 헤더(Size, PacketId) 이후부터 읽기
        count += sizeof(ushort);
        count += sizeof(ushort);

        this.playerId = BitConverter.ToInt32(s.Slice(count)); count += sizeof(int);
        this.posX = BitConverter.ToSingle(s.Slice(count)); count += sizeof(float);
        this.posY = BitConverter.ToSingle(s.Slice(count)); count += sizeof(float);
        this.posZ = BitConverter.ToSingle(s.Slice(count)); count += sizeof(float);
        this.rotY = BitConverter.ToSingle(s.Slice(count)); count += sizeof(float);
        this.isRun = s[count] != 0; count += 1;
        this.isWalk = s[count] != 0; count += 1;
        this.isJump = s[count] != 0; count += 1;
        this.isDodge = s[count] != 0; count += 1;
        this.colorIndex = BitConverter.ToInt32(s.Slice(count)); count += 4;
    }

    // 직렬화: 변수의 데이터를 바이트 배열로 변환 (클라이언트가 보냄)
    public ArraySegment<byte> Write()
    {
        ArraySegment<byte> segment = SendBufferHelper.Open(4096);
        ushort count = 0;
        bool success = true;

        Span<byte> s = new Span<byte>(segment.Array, segment.Offset, segment.Count);

        // Size
        count += sizeof(ushort);

        // Protocol
        success &= BitConverter.TryWriteBytes(s.Slice(count), (ushort)PacketId.S_Move);
        count += sizeof(ushort);

        // PlayerId
        success &= BitConverter.TryWriteBytes(s.Slice(count), this.playerId);
        count += sizeof(int);

        // Floats
        success &= BitConverter.TryWriteBytes(s.Slice(count), this.posX); count += 4;
        success &= BitConverter.TryWriteBytes(s.Slice(count), this.posY); count += 4;
        success &= BitConverter.TryWriteBytes(s.Slice(count), this.posZ); count += 4;
        success &= BitConverter.TryWriteBytes(s.Slice(count), this.rotY); count += 4;

        // Bools
        s[count] = (byte)(this.isRun ? 1 : 0); count += 1;
        s[count] = (byte)(this.isWalk ? 1 : 0); count += 1;
        s[count] = (byte)(this.isJump ? 1 : 0); count += 1;
        s[count] = (byte)(this.isDodge ? 1 : 0); count += 1;

        // Ints
        BitConverter.TryWriteBytes(s.Slice(count), this.colorIndex); count += 4;

        // 전체 패킷 Size 기록
        success &= BitConverter.TryWriteBytes(s.Slice(0), (ushort)count);

        if (!success) return null;
        return SendBufferHelper.Close(count);
    }
}

public class C_Login : IPacket
{
    public ushort Protocol => (ushort)PacketId.C_Login;
    public int userId; // DB의 User PK 값

    public void Read(ArraySegment<byte> segment)
    {
        ReadOnlySpan<byte> s = new ReadOnlySpan<byte>(segment.Array, segment.Offset, segment.Count);
        userId = BitConverter.ToInt32(s.Slice(4)); // Size(2) + Protocol(2)
    }

    public ArraySegment<byte> Write()
    {
        ArraySegment<byte> segment = SendBufferHelper.Open(4096);
        ushort count = 0;
        Span<byte> s = new Span<byte>(segment.Array, segment.Offset, segment.Count);
        count += 2; // Size 예약
        BitConverter.TryWriteBytes(s.Slice(count), Protocol); count += 2;
        BitConverter.TryWriteBytes(s.Slice(count), userId); count += 4;
        BitConverter.TryWriteBytes(s.Slice(0), count); // 최종 Size 기록
        return SendBufferHelper.Close(count);
    }
}

public class S_Login : IPacket
{
    public ushort Protocol => (ushort)PacketId.S_Login;
    public int playerId;

    public void Read(ArraySegment<byte> segment)
    {
        ReadOnlySpan<byte> s = new ReadOnlySpan<byte>(segment.Array, segment.Offset, segment.Count);
        this.playerId = BitConverter.ToInt32(s.Slice(4)); // Size(2) + Protocol(2)
    }

    public ArraySegment<byte> Write()
    {
        ArraySegment<byte> segment = SendBufferHelper.Open(4096);
        ushort count = 0;
        Span<byte> s = new Span<byte>(segment.Array, segment.Offset, segment.Count);
        count += 2; // Size 예약
        BitConverter.TryWriteBytes(s.Slice(count), (ushort)PacketId.S_Login); count += 2;
        BitConverter.TryWriteBytes(s.Slice(count), this.playerId); count += 4;
        BitConverter.TryWriteBytes(s.Slice(0), count); // 최종 Size 기록
        return SendBufferHelper.Close(count);
    }
}

public class S_Leave : IPacket
{
    public ushort Protocol => (ushort)PacketId.S_Leave;
    public int playerId;

    public void Read(ArraySegment<byte> segment)
    {
        ReadOnlySpan<byte> s = new ReadOnlySpan<byte>(segment.Array, segment.Offset, segment.Count);
        this.playerId = BitConverter.ToInt32(s.Slice(4));
    }

    public ArraySegment<byte> Write()
    {
        ArraySegment<byte> segment = SendBufferHelper.Open(4096);
        ushort count = 0;
        Span<byte> s = new Span<byte>(segment.Array, segment.Offset, segment.Count);
        count += 2; // Size 예약
        BitConverter.TryWriteBytes(s.Slice(count), (ushort)PacketId.S_Leave); count += 2;
        BitConverter.TryWriteBytes(s.Slice(count), this.playerId); count += 4;
        BitConverter.TryWriteBytes(s.Slice(0), count); // 최종 Size 기록
        return SendBufferHelper.Close(count);
    }
}

public class S_SpawnItem : IPacket
{
    public ushort Protocol => (ushort)PacketId.S_SpawnItem;
    public int itemDbId;
    public short itemType; // 0: Coin, 1: Exp
    public float posX, posZ;

    public void Read(ArraySegment<byte> segment)
    {
        ReadOnlySpan<byte> s = new ReadOnlySpan<byte>(segment.Array, segment.Offset, segment.Count);
        int count = 4; // Size(2) + Protocol(2)
        itemDbId = BitConverter.ToInt32(s.Slice(count)); count += 4;
        itemType = BitConverter.ToInt16(s.Slice(count)); count += 2;
        posX = BitConverter.ToSingle(s.Slice(count)); count += 4;
        posZ = BitConverter.ToSingle(s.Slice(count)); count += 4;
    }

    public ArraySegment<byte> Write()
    {
        ArraySegment<byte> segment = SendBufferHelper.Open(4096);
        ushort count = 0;
        Span<byte> s = new Span<byte>(segment.Array, segment.Offset, segment.Count);
        count += 2; // Size 예약
        BitConverter.TryWriteBytes(s.Slice(count), Protocol); count += 2;
        BitConverter.TryWriteBytes(s.Slice(count), itemDbId); count += 4;
        BitConverter.TryWriteBytes(s.Slice(count), itemType); count += 2;
        BitConverter.TryWriteBytes(s.Slice(count), posX); count += 4;
        BitConverter.TryWriteBytes(s.Slice(count), posZ); count += 4;
        BitConverter.TryWriteBytes(s.Slice(0), count); // 최종 Size 기록
        return SendBufferHelper.Close(count);
    }
}

public class C_PickUpItem : IPacket {
    public ushort Protocol => (ushort)PacketId.C_PickUpItem;
    public int itemDbId;

    public void Read(ArraySegment<byte> segment) {
        ReadOnlySpan<byte> s = new ReadOnlySpan<byte>(segment.Array, segment.Offset, segment.Count);
        itemDbId = BitConverter.ToInt32(s.Slice(4));
    }

    public ArraySegment<byte> Write() {
        ArraySegment<byte> segment = SendBufferHelper.Open(4096);
        ushort count = 0;
        Span<byte> s = new Span<byte>(segment.Array, segment.Offset, segment.Count);
        count += 2; // Size 예약
        BitConverter.TryWriteBytes(s.Slice(count), Protocol); count += 2;
        BitConverter.TryWriteBytes(s.Slice(count), itemDbId); count += 4;
        BitConverter.TryWriteBytes(s.Slice(0), count); // 최종 Size 기록
        return SendBufferHelper.Close(count);
    }
}

public class S_DespawnItem : IPacket {
    public ushort Protocol => (ushort)PacketId.S_DespawnItem;
    public int itemDbId;

    public void Read(ArraySegment<byte> segment) {
        ReadOnlySpan<byte> s = new ReadOnlySpan<byte>(segment.Array, segment.Offset, segment.Count);
        itemDbId = BitConverter.ToInt32(s.Slice(4));
    }

    public ArraySegment<byte> Write() {
        ArraySegment<byte> segment = SendBufferHelper.Open(4096);
        ushort count = 0;
        Span<byte> s = new Span<byte>(segment.Array, segment.Offset, segment.Count);
        count += 2; // Size 예약
        BitConverter.TryWriteBytes(s.Slice(count), Protocol); count += 2;
        BitConverter.TryWriteBytes(s.Slice(count), itemDbId); count += 4;
        BitConverter.TryWriteBytes(s.Slice(0), count); // 최종 Size 기록
        return SendBufferHelper.Close(count);
    }
}

public class S_StatUpdate : IPacket
{
    public ushort Protocol => (ushort)PacketId.S_StatUpdate;
    public int gold;
    public int exp;

    public void Read(ArraySegment<byte> segment)
    {
        ReadOnlySpan<byte> s = new ReadOnlySpan<byte>(segment.Array, segment.Offset, segment.Count);
        int count = 4;
        gold = BitConverter.ToInt32(s.Slice(count)); count += 4;
        exp = BitConverter.ToInt32(s.Slice(count)); count += 4;
    }

    public ArraySegment<byte> Write()
    {
        ArraySegment<byte> segment = SendBufferHelper.Open(4096);
        ushort count = 0;
        Span<byte> s = new Span<byte>(segment.Array, segment.Offset, segment.Count);
        count += 2; // Size 예약
        BitConverter.TryWriteBytes(s.Slice(count), Protocol); count += 2;
        BitConverter.TryWriteBytes(s.Slice(count), gold); count += 4;
        BitConverter.TryWriteBytes(s.Slice(count), exp); count += 4;
        BitConverter.TryWriteBytes(s.Slice(0), count); // 최종 Size 기록
        return SendBufferHelper.Close(count);
    }
}