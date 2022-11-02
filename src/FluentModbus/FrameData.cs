namespace FluentModbus;

public struct FrameData<T>
    where T: struct
{
    public ushort readStartingAddress;
    public ushort readQuantity;
    public T[] data;
    public int dataOffset;
    public int dataCount;

    public ushort writeStartingAddress;
    public ushort writeQuantity;
    
    public Span<T> GetSpan()
    {
        return new Span<T>(data, dataOffset, dataCount);
    }
}