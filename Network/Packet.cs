// 패킷 구조 정의
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
    S_StatUpdate = 9    // 골드/경험치 수치 업데이트 (서버 -> 클라)
}

// 패킷 인터페이스와 각 패킷 클래스 정의
public interface IPacket
{
    ushort Protocol { get; }
    void Read(ArraySegment<byte> segment);
    ArraySegment<byte> Write();
}

// 패킷 클래스들은 IPacket 인터페이스를 구현하여 Read/Write 메서드를 통해 직렬화/역직렬화 로직을 포함
public class SendBufferHelper
{
    // 스레드별로 별도의 버퍼를 사용하여 안전하게 관리
    public static ThreadLocal<byte[]> CurrentBuffer = new ThreadLocal<byte[]>(() => { return new byte[65535]; });
    public static int UsedSize = 0; // 현재 버퍼에서 사용된 크기

    // 패킷을 작성하기 위해 버퍼에서 일정 크기를 예약하고, 나중에 실제 데이터를 쓴 후 최종적으로 사용된 크기만큼 반환하는 방식
    public static ArraySegment<byte> Open(int reserveSize)
    {
        byte[] buffer = CurrentBuffer.Value ?? new byte[65535];
        return new ArraySegment<byte>(buffer, UsedSize, reserveSize);
    }

    // 패킷 작성이 완료된 후, 실제로 사용된 크기만큼 버퍼에서 반환하는 메서드
    public static ArraySegment<byte> Close(int usedSize)
    {
        byte[] buffer = CurrentBuffer.Value ?? new byte[65535];
        ArraySegment<byte> segment = new ArraySegment<byte>(buffer, UsedSize, usedSize);
        UsedSize += usedSize; // 다음 패킷을 위해 사용된 크기만큼 오프셋 이동

        if (UsedSize > 60000) // 버퍼가 거의 다 찼다면 초기화
            UsedSize = 0;

        return segment; // 실제로 사용된 크기만큼 반환
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

        if (!success) // 직렬화 실패 시 빈 패킷 반환
            return new ArraySegment<byte>();
            
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

        if (!success) // 직렬화 실패 시 빈 패킷 반환
            return new ArraySegment<byte>();

        return SendBufferHelper.Close(count);
    }
}

public class C_Login : IPacket
{
    public ushort Protocol => (ushort)PacketId.C_Login;
    public int userId; // 클라이언트가 로그인 시 자신의 DB ID를 서버에 전달 (세션과 유저를 매핑하기 위해)

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
    public int playerId; // 서버가 클라이언트에게 할당해주는 고유 세션 ID (클라이언트는 이걸로 자신을 인식)

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
    public int playerId; // 세션이 종료된 플레이어의 ID (세션이 끊긴 플레이어를 다른 클라이언트들이 화면에서 제거하기 위해)

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
    public int itemDbId;    // Park에 생성될 한 아이템 객체의 번호
    public short itemType;  // 0: Coin, 1: Exp
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

public class C_PickUpItem : IPacket 
{
    public ushort Protocol => (ushort)PacketId.C_PickUpItem;
    public int itemDbId; // Park에 생성된 아이템 중에서 클라이언트가 습득을 시도하는 아이템의 번호 (클라이언트 -> 서버)

    public void Read(ArraySegment<byte> segment)
    {
        ReadOnlySpan<byte> s = new ReadOnlySpan<byte>(segment.Array, segment.Offset, segment.Count);
        itemDbId = BitConverter.ToInt32(s.Slice(4));
    }

    public ArraySegment<byte> Write()
    {
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

public class S_DespawnItem : IPacket 
{
    public ushort Protocol => (ushort)PacketId.S_DespawnItem;
    public int itemDbId; // Park에 생성된 아이템 중에서 누군가 습득에 성공하여 제거해야 하는 아이템의 번호 (서버 -> 클라이언트)

    public void Read(ArraySegment<byte> segment)
    {
        ReadOnlySpan<byte> s = new ReadOnlySpan<byte>(segment.Array, segment.Offset, segment.Count);
        itemDbId = BitConverter.ToInt32(s.Slice(4));
    }

    public ArraySegment<byte> Write()
    {
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