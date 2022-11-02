using System.Runtime.InteropServices;

namespace FluentModbus
{
    /// <summary>
    /// A base class for Modbus client implementations.
    /// </summary>
    public abstract partial class ModbusClient
    {
        #region Properties

        protected private bool SwapBytes { get; set; }

        #endregion

        #region Methods

        /// <summary>
        /// Sends the requested modbus message and waits for the response.
        /// </summary>
        /// <param name="unitIdentifier">The unit identifier.</param>
        /// <param name="functionCode">The function code.</param>
        /// <param name="extendFrame">An action to be called to extend the prepared Modbus frame with function code specific data.</param>
        protected abstract Span<byte> TransceiveFrame<T>(byte unitIdentifier, ModbusFunctionCode functionCode, Action<ExtendedBinaryWriter, FrameData<T>> extendFrame, FrameData<T> data) where T : struct;

        internal void ProcessError(ModbusFunctionCode functionCode, ModbusExceptionCode exceptionCode)
        {
            switch (exceptionCode)
            {
                case ModbusExceptionCode.IllegalFunction:
                    throw new ModbusException(exceptionCode, ErrorMessage.ModbusClient_0x01_IllegalFunction);

                case ModbusExceptionCode.IllegalDataAddress:
                    throw new ModbusException(exceptionCode, ErrorMessage.ModbusClient_0x02_IllegalDataAddress);

                case ModbusExceptionCode.IllegalDataValue:

                    switch (functionCode)
                    {
                        case ModbusFunctionCode.WriteMultipleRegisters:
                            throw new ModbusException(exceptionCode, ErrorMessage.ModbusClient_0x03_IllegalDataValue_0x7B);

                        case ModbusFunctionCode.ReadHoldingRegisters:
                        case ModbusFunctionCode.ReadInputRegisters:
                            throw new ModbusException(exceptionCode, ErrorMessage.ModbusClient_0x03_IllegalDataValue_0x7D);

                        case ModbusFunctionCode.ReadCoils:
                        case ModbusFunctionCode.ReadDiscreteInputs:
                            throw new ModbusException(exceptionCode, ErrorMessage.ModbusClient_0x03_IllegalDataValue_0x7D0);

                        default:
                            throw new ModbusException(exceptionCode, ErrorMessage.ModbusClient_0x03_IllegalDataValue);
                    }

                case ModbusExceptionCode.ServerDeviceFailure:
                    throw new ModbusException(exceptionCode, ErrorMessage.ModbusClient_0x04_ServerDeviceFailure);

                case ModbusExceptionCode.Acknowledge:
                    throw new ModbusException(exceptionCode, ErrorMessage.ModbusClient_0x05_Acknowledge);

                case ModbusExceptionCode.ServerDeviceBusy:
                    throw new ModbusException(exceptionCode, ErrorMessage.ModbusClient_0x06_ServerDeviceBusy);

                case ModbusExceptionCode.MemoryParityError:
                    throw new ModbusException(exceptionCode, ErrorMessage.ModbusClient_0x08_MemoryParityError);

                case ModbusExceptionCode.GatewayPathUnavailable:
                    throw new ModbusException(exceptionCode, ErrorMessage.ModbusClient_0x0A_GatewayPathUnavailable);

                case ModbusExceptionCode.GatewayTargetDeviceFailedToRespond:
                    throw new ModbusException(exceptionCode, ErrorMessage.ModbusClient_0x0B_GatewayTargetDeviceFailedToRespond);

                default:
                    throw new ArgumentOutOfRangeException(ErrorMessage.ModbusClient_InvalidExceptionCode);
            }
        }

        private ushort ConvertSize<T>(ushort count)
        {
            var size = typeof(T) == typeof(bool) ? 1 : Marshal.SizeOf<T>();
            size = count * size;

            if (size % 2 != 0)
                throw new ArgumentOutOfRangeException(ErrorMessage.ModbusClient_QuantityMustBePositiveInteger);

            var quantity = (ushort)(size / 2);

            return quantity;
        }

        private byte ConvertUnitIdentifier(int unitIdentifier)
        {
            if (!(0 <= unitIdentifier && unitIdentifier <= byte.MaxValue))
                throw new Exception(ErrorMessage.ModbusClient_InvalidUnitIdentifier);

            return (byte)unitIdentifier;
        }

        private ushort ConvertUshort(int value)
        {
            if (!(0 <= value && value <= ushort.MaxValue))
                throw new Exception(ErrorMessage.Modbus_InvalidValueUShort);

            return (ushort)value;
        }

        #endregion

        // class 0

        /// <summary>
        /// Reads the specified number of values of type <typeparamref name="T"/> from the holding registers.
        /// </summary>
        /// <typeparam name="T">Determines the type of the returned data.</typeparam>
        /// <param name="unitIdentifier">The unit identifier is used to communicate via devices such as bridges, routers and gateways that use a single IP address to support multiple independent Modbus end units. Thus, the unit identifier is the address of a remote slave connected on a serial line or on other buses. Use the default values 0x00 or 0xFF when communicating to a Modbus server that is directly connected to a TCP/IP network.</param>
        /// <param name="startingAddress">The holding register start address for the read operation.</param>
        /// <param name="count">The number of elements of type <typeparamref name="T"/> to read.</param>
        public Span<T> ReadHoldingRegisters<T>(int unitIdentifier, int startingAddress, int count) where T : unmanaged
        {
            var unitIdentifierConverted = ConvertUnitIdentifier(unitIdentifier);
            var startingAddressConverted = ConvertUshort(startingAddress);
            var countConverted = ConvertUshort(count);

            var dataset = MemoryMarshal.Cast<byte, T>(
                ReadHoldingRegisters(unitIdentifierConverted, startingAddressConverted, ConvertSize<T>(countConverted)));

            if (SwapBytes)
                ModbusUtils.SwitchEndianness(dataset);

            return dataset;
        }

        /// <summary>
        /// Low level API. Use the generic version of this method for easier access. Reads the specified number of values as byte array from the holding registers.
        /// </summary>
        /// <param name="unitIdentifier">The unit identifier is used to communicate via devices such as bridges, routers and gateways that use a single IP address to support multiple independent Modbus end units. Thus, the unit identifier is the address of a remote slave connected on a serial line or on other buses. Use the default values 0x00 or 0xFF when communicating to a Modbus server that is directly connected to a TCP/IP network.</param>
        /// <param name="startingAddress">The holding register start address for the read operation.</param>
        /// <param name="quantity">The number of holding registers (16 bit per register) to read.</param>
        public Span<byte> ReadHoldingRegisters(byte unitIdentifier, ushort startingAddress, ushort quantity)
        {
            var frameData = new FrameData<byte>();
            frameData.readQuantity = quantity;
            frameData.readStartingAddress = startingAddress;
            
            var buffer = TransceiveFrame(unitIdentifier, ModbusFunctionCode.ReadHoldingRegisters, FrameExtension.ExtendReadHoldingRegisters, frameData).Slice(2);

            if (buffer.Length < quantity * 2)
                throw new ModbusException(ErrorMessage.ModbusClient_InvalidResponseMessageLength);

            return buffer;
        }

        /// <summary>
        /// Writes the provided array of type <typeparamref name="T"/> to the holding registers.
        /// </summary>
        /// <typeparam name="T">Determines the type of the provided data.</typeparam>
        /// <param name="unitIdentifier">The unit identifier is used to communicate via devices such as bridges, routers and gateways that use a single IP address to support multiple independent Modbus end units. Thus, the unit identifier is the address of a remote slave connected on a serial line or on other buses. Use the default values 0x00 or 0xFF when communicating to a Modbus server that is directly connected to a TCP/IP network.</param>
        /// <param name="startingAddress">The holding register start address for the write operation.</param>
        /// <param name="dataset">The data of type <typeparamref name="T"/> to write to the server.</param>
        public void WriteMultipleRegisters<T>(int unitIdentifier, int startingAddress, T[] dataset) where T : unmanaged
        {
            var unitIdentifierConverted = ConvertUnitIdentifier(unitIdentifier);
            var startingAddressConverted = ConvertUshort(startingAddress);

            if (SwapBytes)
                ModbusUtils.SwitchEndianness(dataset.AsSpan());

            WriteMultipleRegisters(unitIdentifierConverted, startingAddressConverted, MemoryMarshal.Cast<T, byte>(dataset).ToArray());
        }

        /// <summary>
        /// Low level API. Use the generic version of this method for easier access. Writes the provided byte array to the holding registers.
        /// </summary>
        /// <param name="unitIdentifier">The unit identifier is used to communicate via devices such as bridges, routers and gateways that use a single IP address to support multiple independent Modbus end units. Thus, the unit identifier is the address of a remote slave connected on a serial line or on other buses. Use the default values 0x00 or 0xFF when communicating to a Modbus server that is directly connected to a TCP/IP network.</param>
        /// <param name="startingAddress">The holding register start address for the write operation.</param>
        /// <param name="dataset">The byte array to write to the server. A minimum of two bytes is required.</param>
        public void WriteMultipleRegisters(byte unitIdentifier, ushort startingAddress, byte[] dataset)
        {
            if (dataset.Length < 2 || dataset.Length % 2 != 0)
                throw new ArgumentOutOfRangeException(ErrorMessage.ModbusClient_ArrayLengthMustBeGreaterThanTwoAndEven);

            var quantity = dataset.Length / 2;
            var frameData = new FrameData<byte>
            {
                readStartingAddress = startingAddress,
                readQuantity = (ushort)quantity,
                data = dataset,
                dataOffset = 0,
                dataCount = dataset.Length
            };

            TransceiveFrame(unitIdentifier, ModbusFunctionCode.WriteMultipleRegisters, FrameExtension.ExtendWriteMultipleRegisters, frameData);
        }


        // class 1

        /// <summary>
        /// Reads the specified number of coils as byte array. Each bit of the returned array represents a single coil.
        /// </summary>
        /// <param name="unitIdentifier">The unit identifier is used to communicate via devices such as bridges, routers and gateways that use a single IP address to support multiple independent Modbus end units. Thus, the unit identifier is the address of a remote slave connected on a serial line or on other buses. Use the default values 0x00 or 0xFF when communicating to a Modbus server that is directly connected to a TCP/IP network.</param>
        /// <param name="startingAddress">The coil start address for the read operation.</param>
        /// <param name="quantity">The number of coils to read.</param>
        public Span<byte> ReadCoils(int unitIdentifier, int startingAddress, int quantity)
        {
            var unitIdentifierConverted = ConvertUnitIdentifier(unitIdentifier);
            var startingAddressConverted = ConvertUshort(startingAddress);
            var quantityConverted = ConvertUshort(quantity);

            var frameData = new FrameData<byte>()
            {
                readQuantity = quantityConverted,
                readStartingAddress = startingAddressConverted
            };
            
            var buffer = TransceiveFrame(unitIdentifierConverted, ModbusFunctionCode.ReadCoils, FrameExtension.ExtendReadCoils, frameData).Slice(2);

            if (buffer.Length < (byte)Math.Ceiling((double)quantityConverted / 8))
                throw new ModbusException(ErrorMessage.ModbusClient_InvalidResponseMessageLength);

            return buffer;
        }

        /// <summary>
        /// Reads the specified number of discrete inputs as byte array. Each bit of the returned array represents a single discete input.
        /// </summary>
        /// <param name="unitIdentifier">The unit identifier is used to communicate via devices such as bridges, routers and gateways that use a single IP address to support multiple independent Modbus end units. Thus, the unit identifier is the address of a remote slave connected on a serial line or on other buses. Use the default values 0x00 or 0xFF when communicating to a Modbus server that is directly connected to a TCP/IP network.</param>
        /// <param name="startingAddress">The discrete input start address for the read operation.</param>
        /// <param name="quantity">The number of discrete inputs to read.</param>
        public Span<byte> ReadDiscreteInputs(int unitIdentifier, int startingAddress, int quantity)
        {
            var unitIdentifierConverted = ConvertUnitIdentifier(unitIdentifier);
            var startingAddressConverted = ConvertUshort(startingAddress);
            var quantityConverted = ConvertUshort(quantity);

            var frameData = new FrameData<byte>()
            {
                readQuantity = quantityConverted,
                readStartingAddress = startingAddressConverted
            };
            
            var buffer = TransceiveFrame(unitIdentifierConverted, ModbusFunctionCode.ReadDiscreteInputs, FrameExtension.ExtendReadDisceteInputs, frameData).Slice(2);

            if (buffer.Length < (byte)Math.Ceiling((double)quantityConverted / 8))
                throw new ModbusException(ErrorMessage.ModbusClient_InvalidResponseMessageLength);

            return buffer;
        }

        /// <summary>
        /// Reads the specified number of values of type <typeparamref name="T"/> from the input registers.
        /// </summary>
        /// <typeparam name="T">Determines the type of the returned data.</typeparam>
        /// <param name="unitIdentifier">The unit identifier is used to communicate via devices such as bridges, routers and gateways that use a single IP address to support multiple independent Modbus end units. Thus, the unit identifier is the address of a remote slave connected on a serial line or on other buses. Use the default values 0x00 or 0xFF when communicating to a Modbus server that is directly connected to a TCP/IP network.</param>
        /// <param name="startingAddress">The input register start address for the read operation.</param>
        /// <param name="count">The number of elements of type <typeparamref name="T"/> to read.</param>
        public Span<T> ReadInputRegisters<T>(int unitIdentifier, int startingAddress, int count) where T : unmanaged
        {
            var unitIdentifierConverted = ConvertUnitIdentifier(unitIdentifier);
            var startingAddressConverted = ConvertUshort(startingAddress);
            var countConverted = ConvertUshort(count);

            var dataset = MemoryMarshal.Cast<byte, T>(
                ReadInputRegisters(unitIdentifierConverted, startingAddressConverted, ConvertSize<T>(countConverted)));

            if (SwapBytes)
                ModbusUtils.SwitchEndianness(dataset);

            return dataset;
        }

        /// <summary>
        /// Low level API. Use the generic version of this method for easier access. Reads the specified number of values as byte array from the input registers.
        /// </summary>
        /// <param name="unitIdentifier">The unit identifier is used to communicate via devices such as bridges, routers and gateways that use a single IP address to support multiple independent Modbus end units. Thus, the unit identifier is the address of a remote slave connected on a serial line or on other buses. Use the default values 0x00 or 0xFF when communicating to a Modbus server that is directly connected to a TCP/IP network.</param>
        /// <param name="startingAddress">The input register start address for the read operation.</param>
        /// <param name="quantity">The number of input registers (16 bit per register) to read.</param>
        public Span<byte> ReadInputRegisters(byte unitIdentifier, ushort startingAddress, ushort quantity)
        {
            var frameData = new FrameData<byte>()
            {
                readQuantity = quantity,
                readStartingAddress = startingAddress
            };
            
            var buffer = TransceiveFrame(unitIdentifier, ModbusFunctionCode.ReadInputRegisters, FrameExtension.ExtendReadInput, frameData).Slice(2);

            if (buffer.Length < quantity * 2)
                throw new ModbusException(ErrorMessage.ModbusClient_InvalidResponseMessageLength);

            return buffer;
        }

        /// <summary>
        /// Writes the provided <paramref name="value"/> to the coil registers.
        /// </summary>
        /// <param name="unitIdentifier">The unit identifier is used to communicate via devices such as bridges, routers and gateways that use a single IP address to support multiple independent Modbus end units. Thus, the unit identifier is the address of a remote slave connected on a serial line or on other buses. Use the default values 0x00 or 0xFF when communicating to a Modbus server that is directly connected to a TCP/IP network.</param>
        /// <param name="registerAddress">The coil address for the write operation.</param>
        /// <param name="value">The value to write to the server.</param>
        public void WriteSingleCoil(int unitIdentifier, int registerAddress, bool value)
        {
            var unitIdentifierConverted = ConvertUnitIdentifier(unitIdentifier);
            var registerAddressConverted = ConvertUshort(registerAddress);

            var frameData = new FrameData<byte>()
            {
                readStartingAddress = registerAddressConverted,
                readQuantity = (ushort)(value ? 0xFF00 : 0x0000)
            };
            
            TransceiveFrame(unitIdentifierConverted, ModbusFunctionCode.WriteSingleCoil, FrameExtension.ExtendWriteSingleCoil, frameData);
        }

        /// <summary>
        /// Writes the provided <paramref name="value"/> to the holding registers.
        /// </summary>
        /// <param name="unitIdentifier">The unit identifier is used to communicate via devices such as bridges, routers and gateways that use a single IP address to support multiple independent Modbus end units. Thus, the unit identifier is the address of a remote slave connected on a serial line or on other buses. Use the default values 0x00 or 0xFF when communicating to a Modbus server that is directly connected to a TCP/IP network.</param>
        /// <param name="registerAddress">The holding register address for the write operation.</param>
        /// <param name="value">The value to write to the server.</param>
        public void WriteSingleRegister(int unitIdentifier, int registerAddress, short value)
        {
            var unitIdentifierConverted = ConvertUnitIdentifier(unitIdentifier);
            var registerAddressConverted = ConvertUshort(registerAddress);

            if (SwapBytes)
                value = ModbusUtils.SwitchEndianness(value);

            WriteSingleRegister(unitIdentifierConverted, registerAddressConverted, MemoryMarshal.Cast<short, byte>(new [] { value }).ToArray());
        }

        /// <summary>
        /// Writes the provided <paramref name="value"/> to the holding registers.
        /// </summary>
        /// <param name="unitIdentifier">The unit identifier is used to communicate via devices such as bridges, routers and gateways that use a single IP address to support multiple independent Modbus end units. Thus, the unit identifier is the address of a remote slave connected on a serial line or on other buses. Use the default values 0x00 or 0xFF when communicating to a Modbus server that is directly connected to a TCP/IP network.</param>
        /// <param name="registerAddress">The holding register address for the write operation.</param>
        /// <param name="value">The value to write to the server.</param>
        public void WriteSingleRegister(int unitIdentifier, int registerAddress, ushort value)
        {
            var unitIdentifierConverted = ConvertUnitIdentifier(unitIdentifier);
            var registerAddressConverted = ConvertUshort(registerAddress);

            if (SwapBytes)
                value = ModbusUtils.SwitchEndianness(value);

            WriteSingleRegister(unitIdentifierConverted, registerAddressConverted, MemoryMarshal.Cast<ushort, byte>(new[] { value }).ToArray());
        }

        /// <summary>
        /// Low level API. Use the overloads of this method for easier access. Writes the provided byte array to the holding register.
        /// </summary>
        /// <param name="unitIdentifier">The unit identifier is used to communicate via devices such as bridges, routers and gateways that use a single IP address to support multiple independent Modbus end units. Thus, the unit identifier is the address of a remote slave connected on a serial line or on other buses. Use the default values 0x00 or 0xFF when communicating to a Modbus server that is directly connected to a TCP/IP network.</param>
        /// <param name="registerAddress">The holding register address for the write operation.</param>
        /// <param name="value">The value to write to the server, which is passed as a 2-byte array.</param>
        public void WriteSingleRegister(byte unitIdentifier, ushort registerAddress, byte[] value)
        {
            if (value.Length != 2)
                throw new ArgumentOutOfRangeException(ErrorMessage.ModbusClient_ArrayLengthMustBeEqualToTwo);

            var frameData = new FrameData<byte>()
            {
                readStartingAddress = registerAddress,
                data = value
            };
            
            TransceiveFrame(unitIdentifier, ModbusFunctionCode.WriteSingleRegister, FrameExtension.ExtendWriteSingleRegister, frameData);
        }

        // class 2

        /// <summary>
        /// This methdod is not implemented.
        /// </summary>
        [Obsolete("This method is not implemented.")]
        public void WriteMultipleCoils()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// This methdod is not implemented.
        /// </summary>
        [Obsolete("This method is not implemented.")]
        public void ReadFileRecord()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// This methdod is not implemented.
        /// </summary>
        [Obsolete("This method is not implemented.")]
        public void WriteFileRecord()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// This methdod is not implemented.
        /// </summary>
        [Obsolete("This method is not implemented.")]
        public void MaskWriteRegister()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Reads the specified number of values of type <typeparamref name="TRead"/> from and writes the provided array of type <typeparamref name="TWrite"/> to the holding registers. The write operation is performed before the read.
        /// </summary>
        /// <typeparam name="TRead">Determines the type of the returned data.</typeparam>
        /// <typeparam name="TWrite">Determines the type of the provided data.</typeparam>
        /// <param name="unitIdentifier">The unit identifier is used to communicate via devices such as bridges, routers and gateways that use a single IP address to support multiple independent Modbus end units. Thus, the unit identifier is the address of a remote slave connected on a serial line or on other buses. Use the default values 0x00 or 0xFF when communicating to a Modbus server that is directly connected to a TCP/IP network.</param>
        /// <param name="readStartingAddress">The holding register start address for the read operation.</param>
        /// <param name="readCount">The number of elements of type <typeparamref name="TRead"/> to read.</param>
        /// <param name="writeStartingAddress">The holding register start address for the write operation.</param>
        /// <param name="dataset">The data of type <typeparamref name="TWrite"/> to write to the server.</param>
        public Span<TRead> ReadWriteMultipleRegisters<TRead, TWrite>(int unitIdentifier, int readStartingAddress, int readCount, int writeStartingAddress, TWrite[] dataset) where TRead : unmanaged
                                                                                                                                                                             where TWrite : unmanaged
        {
            var unitIdentifierConverted = ConvertUnitIdentifier(unitIdentifier);
            var readStartingAddressConverted = ConvertUshort(readStartingAddress);
            var readCountConverted = ConvertUshort(readCount);
            var writeStartingAddressConverted = ConvertUshort(writeStartingAddress);

            if (SwapBytes)
                ModbusUtils.SwitchEndianness(dataset.AsSpan());

            var readQuantity = ConvertSize<TRead>(readCountConverted);
            var byteData = MemoryMarshal.Cast<TWrite, byte>(dataset).ToArray();

            var dataset2 = MemoryMarshal.Cast<byte, TRead>(ReadWriteMultipleRegisters(unitIdentifierConverted, readStartingAddressConverted, readQuantity, writeStartingAddressConverted, byteData));

            if (SwapBytes)
                ModbusUtils.SwitchEndianness(dataset2);

            return dataset2;
        }

        /// <summary>
        /// Low level API. Use the generic version of this method for easier access. Reads the specified number of values as byte array from and writes the provided byte array to the holding registers. The write operation is performed before the read.
        /// </summary>
        /// <param name="unitIdentifier">The unit identifier is used to communicate via devices such as bridges, routers and gateways that use a single IP address to support multiple independent Modbus end units. Thus, the unit identifier is the address of a remote slave connected on a serial line or on other buses. Use the default values 0x00 or 0xFF when communicating to a Modbus server that is directly connected to a TCP/IP network.</param>
        /// <param name="readStartingAddress">The holding register start address for the read operation.</param>
        /// <param name="readQuantity">The number of holding registers (16 bit per register) to read.</param>
        /// <param name="writeStartingAddress">The holding register start address for the write operation.</param>
        /// <param name="dataset">The byte array to write to the server. A minimum of two bytes is required.</param>
        public Span<byte> ReadWriteMultipleRegisters(byte unitIdentifier, ushort readStartingAddress, ushort readQuantity, ushort writeStartingAddress, byte[] dataset)
        {
            if (dataset.Length < 2 || dataset.Length % 2 != 0)
                throw new ArgumentOutOfRangeException(ErrorMessage.ModbusClient_ArrayLengthMustBeGreaterThanTwoAndEven);

            var writeQuantity = dataset.Length / 2;

            var frameData = new FrameData<byte>()
            {
                readStartingAddress = readStartingAddress,
                readQuantity = readQuantity,
                writeStartingAddress = writeStartingAddress,
                writeQuantity = (ushort)writeQuantity,
                data = dataset,
                dataCount = dataset.Length
            };
            
            var buffer = TransceiveFrame(unitIdentifier, ModbusFunctionCode.ReadWriteMultipleRegisters, FrameExtension.ExtendReadWriteMultipleRegisters, frameData).Slice(2);

            if (buffer.Length < readQuantity * 2)
                throw new ModbusException(ErrorMessage.ModbusClient_InvalidResponseMessageLength);

            return buffer;
        }

        /// <summary>
        /// This methdod is not implemented.
        /// </summary>
        [Obsolete("This method is not implemented.")]
        public void ReadFifoQueue()
        {
            throw new NotImplementedException();
        }
    }
}
