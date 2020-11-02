using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.IO.Compression;

namespace CN.Teddy.Compression
{
    /// <summary>
    /// Compress Manager
    /// </summary>
    public class CompressionManager
    {
        public string CompressGZip(string str)
        {
            byte[] buffer = UTF8Encoding.Unicode.GetBytes(str);
            return Convert.ToBase64String(CompressGZip(buffer));
        }

        public byte[] CompressGZip(byte[] buffer)
        {
            if (buffer == null || buffer.Length == 0)
            {
                return buffer;
            }

            MemoryStream ms = new MemoryStream();
            using (GZipStream gzip = new GZipStream(ms, CompressionMode.Compress))
            {
                gzip.Write(buffer, 0, buffer.Length);
            }
            return ms.ToArray();
        }

        public string DecompressGZip(string str)
        {
            byte[] buffer = Convert.FromBase64String(str);
            return UTF8Encoding.Unicode.GetString(DecompressGZip(buffer));
        }

        public byte[] DecompressGZip(byte[] buffer)
        {
            if (buffer == null || buffer.Length == 0)
            {
                return buffer;
            }

            MemoryStream ms = new MemoryStream(buffer, 0, buffer.Length);
            MemoryStream msOut = new MemoryStream();
            byte[] writeData = new byte[4096];
            using (GZipStream gzip = new GZipStream(ms, CompressionMode.Decompress))
            {
                int n;
                while ((n = gzip.Read(writeData, 0, writeData.Length)) > 0)
                {
                    msOut.Write(writeData, 0, n);
                }
            }
            return msOut.ToArray();
        }

        /// <summary>
        /// 7Zip Compress the str.
        /// </summary>
        /// <param name="str">The STR.</param>
        /// <returns></returns>
        public string Compress7Zip(string str)
        {
            byte[] inbyt = UTF8Encoding.Unicode.GetBytes(str);
            byte[] b = SevenZip.Compression.LZMA.SevenZipHelper.Compress(inbyt);
            return Convert.ToBase64String(b);
        }

        public byte[] Compress7Zip(byte[] buffer)
        {
            if (buffer == null || buffer.Length == 0)
            {
                return buffer;
            }

            return SevenZip.Compression.LZMA.SevenZipHelper.Compress(buffer);
        }

        /// <summary>
        /// 7Zip Decompress the str.
        /// </summary>
        /// <param name="str">The STR.</param>
        /// <returns></returns>
        public string Decompress7Zip(string str)
        {
            byte[] inbyt = Convert.FromBase64String(str);
            byte[] b = SevenZip.Compression.LZMA.SevenZipHelper.Decompress(inbyt);
            return UTF8Encoding.Unicode.GetString(b);
        }

        public byte[] Decompress7Zip(byte[] buffer)
        {
            if (buffer == null || buffer.Length == 0)
            {
                return buffer;
            }

            return SevenZip.Compression.LZMA.SevenZipHelper.Decompress(buffer);
        }
    }
}
