using System;
using System.IO;

namespace GameRes.Core
{
    public struct Adler32
    {
        public readonly int Value
        {
            get
            {
                return m_A2 << 16 | m_A1;
            }
        }

        public Adler32()
        {
            m_A1 = 1;
            m_A2 = 0;
        }

        public void Update(ReadOnlySpan<byte> _bytes)
        {
            for (int i = 0; i < _bytes.Length; i++)
            {
                m_A1 = (m_A1 + _bytes[i]) % 65521;
                m_A2 = (m_A2 + m_A1) % 65521;
            }
        }

        public int Make(Stream _stream)
        {
            BinaryReader binaryReader = new(_stream);
            return Make(binaryReader.ReadBytes((int)_stream.Length));
        }

        public int Make(ReadOnlySpan<byte> _bytes)
        {
            m_A1 = 1;
            m_A2 = 0;
            Update(_bytes);
            return Value;
        }

        private int m_A1;

        private int m_A2;
    }
}
