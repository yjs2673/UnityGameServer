public enum PacketId : ushort
{
    C_Move = 1,
    S_Move = 2,
    S_Login = 3
}

public interface IPacket
{
    ushort Protocol { get; }
    void Read(ArraySegment<byte> segment);
    ArraySegment<byte> Write();
}

public class C_Move : IPacket
{
    public ushort Protocol => (ushort)PacketId.C_Move;
    public float posX, posY, posZ;
    public float rotY;

    // 역직렬화: 바이트 배열에서 데이터를 뽑아내 변수에 저장 (서버가 받음)
    public void Read(ArraySegment<byte> segment)
    {
        ReadOnlySpan<byte> s = new ReadOnlySpan<byte>(segment.Array, segment.Offset, segment.Count);
        int count = 0;

        // 헤더(Size, PacketId) 이후부터 읽기
        count += sizeof(ushort); // Size 건너뛰기
        count += sizeof(ushort); // Protocol 건너뛰기

        this.posX = BitConverter.ToSingle(s.Slice(count));
        count += sizeof(float);
        this.posY = BitConverter.ToSingle(s.Slice(count));
        count += sizeof(float);
        this.posZ = BitConverter.ToSingle(s.Slice(count));
        count += sizeof(float);
        this.rotY = BitConverter.ToSingle(s.Slice(count));
        count += sizeof(float);
    }

    // 직렬화: 변수의 데이터를 바이트 배열로 변환 (클라이언트가 보냄)
    public ArraySegment<byte> Write()
    {
        ArraySegment<byte> segment = SendBufferHelper.Open(4096); // 적당한 크기 할당
        ushort count = 0;
        bool success = true;

        Span<byte> s = new Span<byte>(segment.Array, segment.Offset, segment.Count);

        // 헤더 공간 비워두고 데이터부터 기입
        count += sizeof(ushort);
        success &= BitConverter.TryWriteBytes(s.Slice(count), (ushort)PacketId.C_Move);
        count += sizeof(ushort);

        success &= BitConverter.TryWriteBytes(s.Slice(count), this.posX);
        count += sizeof(float);
        success &= BitConverter.TryWriteBytes(s.Slice(count), this.posY);
        count += sizeof(float);
        success &= BitConverter.TryWriteBytes(s.Slice(count), this.posZ);
        count += sizeof(float);
        success &= BitConverter.TryWriteBytes(s.Slice(count), this.rotY);
        count += sizeof(float);

        // 마지막에 전체 패킷 크기(Size) 기록
        success &= BitConverter.TryWriteBytes(s.Slice(0), count);

        if (!success) return null;
        return SendBufferHelper.Close(count);
    }
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
        UsedSize += usedSize;
        // 실제 구현에서는 일정 크기가 차면 초기화하는 로직 필요
        if (UsedSize > 60000) UsedSize = 0;
        return segment;
    }
}

public class S_Move : IPacket
{
    public ushort Protocol => (ushort)PacketId.S_Move;
    public int playerId;
    public float posX, posY, posZ;
    public float rotY;

    public void Read(ArraySegment<byte> segment)
    {
        ReadOnlySpan<byte> s = new ReadOnlySpan<byte>(segment.Array, segment.Offset, segment.Count);
        int count = 0;

        count += sizeof(ushort); // Size 건너뛰기
        count += sizeof(ushort); // Protocol 건너뛰기

        this.playerId = BitConverter.ToInt32(s.Slice(count));
        count += sizeof(int);
        this.posX = BitConverter.ToSingle(s.Slice(count));
        count += sizeof(float);
        this.posY = BitConverter.ToSingle(s.Slice(count));
        count += sizeof(float);
        this.posZ = BitConverter.ToSingle(s.Slice(count));
        count += sizeof(float);
        this.rotY = BitConverter.ToSingle(s.Slice(count));
        count += sizeof(float);
    }

    public ArraySegment<byte> Write()
    {
        ArraySegment<byte> segment = SendBufferHelper.Open(4096);
        ushort count = 0;
        bool success = true;

        Span<byte> s = new Span<byte>(segment.Array, segment.Offset, segment.Count);

        count += sizeof(ushort); // Size 공간
        success &= BitConverter.TryWriteBytes(s.Slice(count), (ushort)PacketId.S_Move);
        count += sizeof(ushort);

        success &= BitConverter.TryWriteBytes(s.Slice(count), this.playerId);
        count += sizeof(int);
        success &= BitConverter.TryWriteBytes(s.Slice(count), this.posX);
        count += sizeof(float);
        success &= BitConverter.TryWriteBytes(s.Slice(count), this.posY);
        count += sizeof(float);
        success &= BitConverter.TryWriteBytes(s.Slice(count), this.posZ);
        count += sizeof(float);
        success &= BitConverter.TryWriteBytes(s.Slice(count), this.rotY);
        count += sizeof(float);

        success &= BitConverter.TryWriteBytes(s.Slice(0), count);

        if (!success) return null;
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
        this.playerId = BitConverter.ToInt32(s.Slice(4)); // Size(2) + Protocol(2) 이후
    }

    public ArraySegment<byte> Write()
    {
        ArraySegment<byte> segment = SendBufferHelper.Open(4096);
        ushort count = 0;
        Span<byte> s = new Span<byte>(segment.Array, segment.Offset, segment.Count);
        count += 2; // Size 공간
        BitConverter.TryWriteBytes(s.Slice(count), (ushort)PacketId.S_Login); count += 2;
        BitConverter.TryWriteBytes(s.Slice(count), this.playerId); count += 4;
        BitConverter.TryWriteBytes(s.Slice(0), count);
        return SendBufferHelper.Close(count);
    }
}