namespace FluentModbus;

public static class FrameExtension
{
    #region Extend Methods

    internal static readonly Action<ExtendedBinaryWriter, FrameData<byte>> ExtendReadHoldingRegisters = ExtendReadHoldingRegistersMethod;
    internal static readonly Action<ExtendedBinaryWriter, FrameData<byte>> ExtendWriteMultipleRegisters = ExtendWriteMultipleRegistersMethod;
    internal static readonly Action<ExtendedBinaryWriter, FrameData<byte>> ExtendReadCoils = ExtendReadCoilsMethod;
    internal static readonly Action<ExtendedBinaryWriter, FrameData<byte>> ExtendReadDisceteInputs = ExtendReadDisceteInputsMethod;
    internal static readonly Action<ExtendedBinaryWriter, FrameData<byte>> ExtendReadInput = ExtendReadInputMethod;
    internal static readonly Action<ExtendedBinaryWriter, FrameData<byte>> ExtendWriteSingleCoil = ExtendWriteSingleCoilMethod;
    internal static readonly Action<ExtendedBinaryWriter, FrameData<byte>> ExtendWriteSingleRegister = ExtendWriteSingleRegisterMethod;
    internal static readonly Action<ExtendedBinaryWriter, FrameData<byte>> ExtendReadWriteMultipleRegisters = ExtendReadWriteMultipleRegistersMethod;
    
    internal static void ExtendReadHoldingRegistersMethod(ExtendedBinaryWriter writer, FrameData<byte> data)
    {
        writer.Write((byte)ModbusFunctionCode.ReadHoldingRegisters); // 07     Function Code

        if (BitConverter.IsLittleEndian)
        {
            writer.WriteReverse(data.readStartingAddress); // 08-09  Starting Address
            writer.WriteReverse(data.readQuantity); // 10-11  Quantity of Input Registers
        }
        else
        {
            writer.Write(data.readStartingAddress); // 08-09  Starting Address
            writer.Write(data.readQuantity); // 10-11  Quantity of Input Registers
        }
    }

    internal static void ExtendWriteMultipleRegistersMethod(ExtendedBinaryWriter writer, FrameData<byte> data)
    {
        writer.Write((byte)ModbusFunctionCode.WriteMultipleRegisters); // 07     Function Code

        if (BitConverter.IsLittleEndian)
        {
            writer.WriteReverse(data.readStartingAddress); // 08-09  Starting Address
            writer.WriteReverse(data.readQuantity); // 10-11  Quantity of Registers
        }
        else
        {
            writer.Write(data.readStartingAddress); // 08-09  Starting Address
            writer.Write(data.readQuantity); // 10-11  Quantity of Registers
        }

        writer.Write((byte)(data.readQuantity * 2)); // 12     Byte Count = Quantity of Registers * 2
        writer.Write(data.data, data.dataOffset, data.dataCount);
    }

    internal static void ExtendReadCoilsMethod(ExtendedBinaryWriter writer, FrameData<byte> data)
    {
        writer.Write((byte)ModbusFunctionCode.ReadCoils); // 07     Function Code

        if (BitConverter.IsLittleEndian)
        {
            writer.WriteReverse(data.readStartingAddress); // 08-09  Starting Address
            writer.WriteReverse(data.readQuantity); // 10-11  Quantity of Coils
        }
        else
        {
            writer.Write(data.readStartingAddress); // 08-09  Starting Address
            writer.Write(data.readQuantity); // 10-11  Quantity of Coils
        }
    }

    internal static void ExtendReadDisceteInputsMethod(ExtendedBinaryWriter writer, FrameData<byte> data)
    {
        writer.Write((byte)ModbusFunctionCode.ReadDiscreteInputs); // 07     Function Code

        if (BitConverter.IsLittleEndian)
        {
            writer.WriteReverse(data.readStartingAddress); // 08-09  Starting Address
            writer.WriteReverse(data.readQuantity); // 10-11  Quantity of Coils
        }
        else
        {
            writer.Write(data.readStartingAddress); // 08-09  Starting Address
            writer.Write(data.readQuantity); // 10-11  Quantity of Coils
        }
    }

    internal static void ExtendReadInputMethod(ExtendedBinaryWriter writer, FrameData<byte> data)
    {
        writer.Write((byte)ModbusFunctionCode.ReadInputRegisters); // 07     Function Code

        if (BitConverter.IsLittleEndian)
        {
            writer.WriteReverse(data.readStartingAddress); // 08-09  Starting Address
            writer.WriteReverse(data.readQuantity); // 10-11  Quantity of Input Registers
        }
        else
        {
            writer.Write(data.readStartingAddress); // 08-09  Starting Address
            writer.Write(data.readQuantity); // 10-11  Quantity of Input Registers
        }
    }

    internal static void ExtendWriteSingleCoilMethod(ExtendedBinaryWriter writer, FrameData<byte> data)
    {
        writer.Write((byte)ModbusFunctionCode.WriteSingleCoil); // 07     Function Code

        if (BitConverter.IsLittleEndian)
        {
            writer.WriteReverse(data.readStartingAddress); // 08-09  Starting Address
            writer.WriteReverse(data.readQuantity); // 10-11  Value
        }
        else
        {
            writer.Write(data.readStartingAddress); // 08-09  Starting Address
            writer.Write(data.readQuantity); // 10-11  Value
        }
    }

    internal static void ExtendWriteSingleRegisterMethod(ExtendedBinaryWriter writer, FrameData<byte> data)
    {
        writer.Write((byte)ModbusFunctionCode.WriteSingleRegister); // 07     Function Code

        if (BitConverter.IsLittleEndian)
            writer.WriteReverse(data.readStartingAddress); // 08-09  Starting Address
        else
            writer.Write(data.readStartingAddress); // 08-09  Starting Address

        writer.Write(data.data); // 10-11  Value
    }

    internal static void ExtendReadWriteMultipleRegistersMethod(ExtendedBinaryWriter writer, FrameData<byte> data)
    {
        writer.Write((byte)ModbusFunctionCode.ReadWriteMultipleRegisters); // 07     Function Code

        if (BitConverter.IsLittleEndian)
        {
            writer.WriteReverse(data.readStartingAddress); // 08-09  Read Starting Address
            writer.WriteReverse(data.readQuantity); // 10-11  Quantity to Read
            writer.WriteReverse(data.writeStartingAddress); // 12-13  Read Starting Address
            writer.WriteReverse(data.writeQuantity); // 14-15  Quantity to Write
        }
        else
        {
            writer.Write(data.readStartingAddress); // 08-09  Read Starting Address
            writer.Write(data.readQuantity); // 10-11  Quantity to Read
            writer.Write(data.writeStartingAddress); // 12-13  Read Starting Address
            writer.Write(data.writeQuantity); // 14-15  Quantity to Write
        }

        writer.Write((byte)(data.writeQuantity * 2)); // 16     Byte Count = Quantity to Write * 2

        writer.Write(data.data, data.dataOffset, data.dataCount);
    }

    #endregion
}