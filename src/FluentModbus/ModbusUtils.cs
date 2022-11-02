using System.Globalization;
using System.Net;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

#if NETSTANDARD2_1_OR_GREATER
using System.Diagnostics.CodeAnalysis;
#endif

namespace FluentModbus
{
    internal static class ModbusUtils
    {
#if NETSTANDARD2_0
        public static bool TryParseEndpoint(ReadOnlySpan<char> value, out IPEndPoint? result)
#endif
#if NETSTANDARD2_1_OR_GREATER
        public static bool TryParseEndpoint(ReadOnlySpan<char> value, [NotNullWhen(true)] out IPEndPoint? result)
#endif
        {
            var addressLength = value.Length;
            var lastColonPos = value.LastIndexOf(':');

            if (lastColonPos > 0)
            {
                if (value[lastColonPos - 1] == ']')
                    addressLength = lastColonPos;

                else if (value.Slice(0, lastColonPos).LastIndexOf(':') == -1)
                    addressLength = lastColonPos;
            }

            if (IPAddress.TryParse(value.Slice(0, addressLength).ToString(), out var address))
            {
                var port = 502U;

                if (addressLength == value.Length ||
                    (uint.TryParse(value.Slice(addressLength + 1).ToString(), NumberStyles.None, CultureInfo.InvariantCulture, out port) && port <= 65536))

                {
                    result = new IPEndPoint(address, (int)port);
                    return true;
                }
            }

            result = default;

            return false;
        }

        public static ushort CalculateCrc(Memory<byte> buffer)
        {
            var span = buffer.Span;
            ushort crc = 0xFFFF;

            foreach (var value in span)
            {
                crc ^= value;

                for (int i = 0; i < 8; i++)
                {
                    if ((crc & 0x0001) != 0)
                    {
                        crc >>= 1;
                        crc ^= 0xA001;
                    }
                    else
                    {
                        crc >>= 1;
                    }
                }
            }

            return crc;
        }

        public static bool DetectFrame(byte unitIdentifier, Memory<byte> frame)
        {
            /* Correct response frame (min. 6 bytes)
             * 00 Unit Identifier
             * 01 Function Code
             * 02 Byte count
             * 03 Minimum of 1 byte
             * 04 CRC Byte 1
             * 05 CRC Byte 2
             */

            /* Error response frame (5 bytes)
             * 00 Unit Identifier
             * 01 Function Code + 0x80
             * 02 Exception Code
             * 03 CRC Byte 1
             * 04 CRC Byte 2
             */

            var span = frame.Span;

            if (span.Length < 5)
                return false;

            if (unitIdentifier != 255) // 255 means "skip unit identifier check"
            {
                var newUnitIdentifier = span[0];

                if (newUnitIdentifier != unitIdentifier)
                    return false;
            }

            // Byte count check
            if (span[1] < 0x80 && span.Length < span[2] + 5)
                return false;

            // CRC check
            var crcBytes = span.Slice(span.Length - 2, 2);
            var actualCrc = unchecked((ushort)((crcBytes[1] << 8) + crcBytes[0]));
            var expectedCrc = ModbusUtils.CalculateCrc(frame.Slice(0, frame.Length - 2));

            if (actualCrc != expectedCrc)
                return false;

            return true;
        }
        
        public static short SwitchEndianness(short value)
        {
            return (short)(((value & 0xFF) << 8) | (value >> 8));
        }
        
        public static ushort SwitchEndianness(ushort value)
        {
            return (ushort)(((value & 0xFF) << 8) | (value >> 8));
        }
        
        public static T SwitchEndianness<T>(T value) where T : unmanaged
        {
            Span<T> data = stackalloc T[] { value };
            ModbusUtils.SwitchEndianness(data);

            return data[0];
        }

        public static T ConvertBetweenLittleEndianAndMidLittleEndian<T>(T value) where T : unmanaged
        {
            // from DCBA to CDAB

            if (Unsafe.SizeOf<T>() == 4)
            {
                Span<T> data = stackalloc T[] { value, default };

                var datasetBytes = MemoryMarshal.Cast<T, byte>(data);
                var offset = 4;

                datasetBytes[offset + 0] = datasetBytes[1];
                datasetBytes[offset + 1] = datasetBytes[0];
                datasetBytes[offset + 2] = datasetBytes[3];
                datasetBytes[offset + 3] = datasetBytes[2];

                return data[1];
            }
            else
            {
                throw new Exception($"Type {value.GetType().Name} cannot be represented as mid-little-endian.");
            }
        }

        public static void SwitchEndianness<T>(Memory<T> dataset) where T : unmanaged
        {
            ModbusUtils.SwitchEndianness(dataset.Span);
        }

        public static void SwitchEndianness<T>(Span<T> dataset) where T : unmanaged
        {
            var size = Marshal.SizeOf<T>();
            var datasetBytes = MemoryMarshal.Cast<T, byte>(dataset);

            for (int i = 0; i < datasetBytes.Length; i += size)
            {
                for (int j = 0; j < size / 2; j++)
                {
                    var i1 = i + j;
                    var i2 = i - j + size - 1;

                    byte tmp = datasetBytes[i1];
                    datasetBytes[i1] = datasetBytes[i2];
                    datasetBytes[i2] = tmp;
                }
            }
        }
    }
}