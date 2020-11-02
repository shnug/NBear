using System;
using System.Data;
using System.IO;
using System.Text;
using System.Configuration;
using System.Security;
using System.Security.Cryptography;
using CN.Teddy.DesignByContract;

namespace CN.Teddy.Cryptography
{
    /// <summary>
    /// Common CryptographyManager
    /// </summary>
    public class CryptographyManager
    {
        private readonly string _defaultLegalIV = "E4ghj*Ghg7!rNIfb&95GUY86GfghUb#er57HBh(u%g6HJ($jhWk7&!hg4ui%$hjk";

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="CryptographyManager"/> class.
        /// </summary>
        public CryptographyManager()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CryptographyManager"/> class.
        /// </summary>
        /// <param name="legalIVKey">The legal IV key.</param>
        public CryptographyManager(string legalIVKey)
        {
            Check.Require(!string.IsNullOrEmpty(legalIVKey), "legalIVKey could not be null or empty.");

            _defaultLegalIV = legalIVKey;
        }

        #endregion

        #region Private Members

        /// <summary>
        /// The default encrypt key.
        /// </summary>
        public const string DEFAULT_KEY = "aslkjkljlsajsuaggasfklrjuisdhaie";

        private static byte[] GetLegalKey(SymmetricAlgorithm mobjCryptoService, string key)
        {
            string sTemp = key;
            mobjCryptoService.GenerateKey();
            byte[] bytTemp = mobjCryptoService.Key;
            int KeyLength = bytTemp.Length;
            if (sTemp.Length > KeyLength)
                sTemp = sTemp.Substring(0, KeyLength);
            else if (sTemp.Length < KeyLength)
                sTemp = sTemp.PadRight(KeyLength, ' ');
            return ASCIIEncoding.ASCII.GetBytes(sTemp);
        }

        private byte[] GetLegalIV(SymmetricAlgorithm mobjCryptoService)
        {
            string sTemp = _defaultLegalIV;
            mobjCryptoService.GenerateIV();
            byte[] bytTemp = mobjCryptoService.IV;
            int IVLength = bytTemp.Length;
            if (sTemp.Length > IVLength)
                sTemp = sTemp.Substring(0, IVLength);
            else if (sTemp.Length < IVLength)
                sTemp = sTemp.PadRight(IVLength, ' ');
            return ASCIIEncoding.ASCII.GetBytes(sTemp);
        }

        #endregion

        #region public Members

        /// <summary>
        /// Symmetrics encrpyt.
        /// </summary>
        /// <param name="str">The STR to encrpyt.</param>
        /// <param name="mobjCryptoService">A concrete symmetric algorithm.</param>
        /// <param name="key">The key.</param>
        /// <returns>The encrpyt str.</returns>
        public string SymmetricEncrpyt(string str, SymmetricAlgorithm mobjCryptoService, string key)
        {
            Check.Require(str != null, "str could not be null!");

            byte[] bytIn = UTF8Encoding.UTF8.GetBytes(str);
            return Convert.ToBase64String(SymmetricEncrpyt(bytIn, mobjCryptoService, key));
        }

        /// <summary>
        /// Symmetrics the encrpyt.
        /// </summary>
        /// <param name="buffer">The buffer.</param>
        /// <param name="mobjCryptoService">The mobj crypto service.</param>
        /// <param name="key">The key.</param>
        /// <returns></returns>
        public byte[] SymmetricEncrpyt(byte[] buffer, SymmetricAlgorithm mobjCryptoService, string key)
        {
            if (buffer == null || buffer.Length == 0)
            {
                return buffer;
            }

            Check.Require(mobjCryptoService != null, "mobjCryptoService could not be null!");

            MemoryStream ms = new MemoryStream();
            mobjCryptoService.Key = GetLegalKey(mobjCryptoService, key);
            mobjCryptoService.IV = GetLegalIV(mobjCryptoService);
            ICryptoTransform encrypto = mobjCryptoService.CreateEncryptor();
            CryptoStream cs = new CryptoStream(ms, encrypto, CryptoStreamMode.Write);
            cs.Write(buffer, 0, buffer.Length);
            cs.FlushFinalBlock();
            ms.Close();
            return ms.ToArray();
        }

        /// <summary>
        /// Symmetrics decrpyt.
        /// </summary>
        /// <param name="str">The STR to decrpyt.</param>
        /// <param name="mobjCryptoService">A concrete symmetric algorithm.</param>
        /// <param name="key">The key.</param>
        /// <returns>The decrpyted str.</returns>
        public string SymmetricDecrpyt(string str, SymmetricAlgorithm mobjCryptoService, string key)
        {
            Check.Require(str != null, "str could not be null!");

            byte[] bytIn = Convert.FromBase64String(str);
            return UTF8Encoding.Unicode.GetString(SymmetricDecrpyt(bytIn, mobjCryptoService, key));
        }

        /// <summary>
        /// Symmetrics the decrpyt.
        /// </summary>
        /// <param name="buffer">The buffer.</param>
        /// <param name="mobjCryptoService">The mobj crypto service.</param>
        /// <param name="key">The key.</param>
        /// <returns></returns>
        public byte[] SymmetricDecrpyt(byte[] buffer, SymmetricAlgorithm mobjCryptoService, string key)
        {
            if (buffer == null || buffer.Length == 0)
            {
                return buffer;
            }

            Check.Require(mobjCryptoService != null, "mobjCryptoService could not be null!");

            MemoryStream ms = new MemoryStream(buffer, 0, buffer.Length);
            MemoryStream msOut = new MemoryStream();
            mobjCryptoService.Key = GetLegalKey(mobjCryptoService, key);
            mobjCryptoService.IV = GetLegalIV(mobjCryptoService);
            ICryptoTransform encrypto = mobjCryptoService.CreateDecryptor();
            byte[] writeData = new byte[4096];
            using (CryptoStream cs = new CryptoStream(ms, encrypto, CryptoStreamMode.Read))
            {
                int n;
                while ((n = cs.Read(writeData, 0, writeData.Length)) > 0)
                {
                    msOut.Write(writeData, 0, n);
                }
            }
            return msOut.ToArray();
        }

        /// <summary>
        /// Computes the hash.
        /// </summary>
        /// <param name="str">The STR to compute hash value.</param>
        /// <returns>The hash value.</returns>
        public string ComputeHash(string str)
        {
            Check.Require(str != null, "Arguments error.", new ArgumentNullException("str"));

            byte[] bytIn = UTF8Encoding.UTF8.GetBytes(str);
            return Convert.ToBase64String(ComputeHash(bytIn));
        }

        /// <summary>
        /// Computes the hash.
        /// </summary>
        /// <param name="buffer">The buffer.</param>
        /// <returns></returns>
        public byte[] ComputeHash(byte[] buffer)
        {
            HashAlgorithm sha = new SHA1CryptoServiceProvider();
            return sha.ComputeHash(buffer);
        }

        #endregion
    }
}