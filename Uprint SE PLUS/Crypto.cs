﻿// Ported from crypto.py by David Slayton (2014); copyright below.

// Copyright (c) 2013, Benjamin Vanheuverzwijn <bvanheu@gmail.com>
// All rights reserved.
//
// Redistribution and use in source and binary forms, with or without
// modification, are permitted provided that the following conditions are met:
//     * Redistributions of source code must retain the above copyright
//       notice, this list of conditions and the following disclaimer.
//     * Redistributions in binary form must reproduce the above copyright
//       notice, this list of conditions and the following disclaimer in the
//       documentation and/or other materials provided with the distribution.
//     * Neither the name of the <organization> nor the
//       names of its contributors may be used to endorse or promote products
//       derived from this software without specific prior written permission.
//
// THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS" AND
// ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED
// WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
// DISCLAIMED. IN NO EVENT SHALL <BENJAMIN VANHEUVERZWIJN> BE LIABLE FOR ANY
// DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES
// (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
// LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND
// ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
// (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
// SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.

using System;
using System.IO;
using System.Security.Cryptography;

namespace CartridgeWriter
{
    public class Desx_Crypto
    {
        private static byte[] clorox = new byte[256]
        {
            0xBD,0x56,0xEA,0xF2,0xA2,0xF1,0xAC,0x2A,0xB0,0x93,0xD1,
            0x9C,0x1B,0x33,0xFD,0xD0,0x30,0x04,0xB6,0xDC,0x7D,0xDF,
            0x32,0x4B,0xF7,0xCB,0x45,0x9B,0x31,0xBB,0x21,0x5A,0x41,
            0x9F,0xE1,0xD9,0x4A,0x4D,0x9E,0xDA,0xA0,0x68,0x2C,0xC3,
            0x27,0x5F,0x80,0x36,0x3E,0xEE,0xFB,0x95,0x1A,0xFE,0xCE,
            0xA8,0x34,0xA9,0x13,0xF0,0xA6,0x3F,0xD8,0x0C,0x78,0x24,
            0xAF,0x23,0x52,0xC1,0x67,0x17,0xF5,0x66,0x90,0xE7,0xE8,
            0x07,0xB8,0x60,0x48,0xE6,0x1E,0x53,0xF3,0x92,0xA4,0x72,
            0x8C,0x08,0x15,0x6E,0x86,0x00,0x84,0xFA,0xF4,0x7F,0x8A,
            0x42,0x19,0xF6,0xDB,0xCD,0x14,0x8D,0x50,0x12,0xBA,0x3C,
            0x06,0x4E,0xEC,0xB3,0x35,0x11,0xA1,0x88,0x8E,0x2B,0x94,
            0x99,0xB7,0x71,0x74,0xD3,0xE4,0xBF,0x3A,0xDE,0x96,0x0E,
            0xBC,0x0A,0xED,0x77,0xFC,0x37,0x6B,0x03,0x79,0x89,0x62,
            0xC6,0xD7,0xC0,0xD2,0x7C,0x6A,0x8B,0x22,0xA3,0x5B,0x05,
            0x5D,0x02,0x75,0xD5,0x61,0xE3,0x18,0x8F,0x55,0x51,0xAD,
            0x1F,0x0B,0x5E,0x85,0xE5,0xC2,0x57,0x63,0xCA,0x3D,0x6C,
            0xB4,0xC5,0xCC,0x70,0xB2,0x91,0x59,0x0D,0x47,0x20,0xC8,
            0x4F,0x58,0xE0,0x01,0xE2,0x16,0x38,0xC4,0x6F,0x3B,0x0F,
            0x65,0x46,0xBE,0x7E,0x2D,0x7B,0x82,0xF9,0x40,0xB5,0x1D,
            0x73,0xF8,0xEB,0x26,0xC7,0x87,0x97,0x25,0x54,0xB1,0x28,
            0xAA,0x98,0x9D,0xA5,0x64,0x6D,0x7A,0xD4,0x10,0x81,0x44,
            0xEF,0x49,0xD6,0xAE,0x2E,0xDD,0x76,0x5C,0x2F,0xA7,0x1C,
            0xC9,0x09,0x69,0x9A,0x83,0xCF,0x29,0x39,0xB9,0xE9,0x4C,
            0xFF,0x43,0xAB
        };

        public static byte[] Encrypt(byte[] key, byte[] plaintext)
        {
            if (key == null) throw new ArgumentNullException("key");
            if (key.Length != 16)
                throw new ArgumentOutOfRangeException("key length must be 16 bytes");
            if (plaintext == null) throw new ArgumentNullException("plaintext");
            if ((plaintext.Length % 8) != 0)
                throw new Exception("plaintext length must be a multiple of 8 bytes");

            byte[] input_whitening_key = new byte[8];
            Buffer.BlockCopy(key, 8, input_whitening_key, 0, 8);
            byte[] output_whitening_key = BuildOutputWhiteningKey(key);

            XOR input_whitener = new XOR(input_whitening_key);
            XOR output_whitener = new XOR(output_whitening_key);
            byte[] ciphertext = new byte[plaintext.Length];
            byte[] desKey = new byte[8];
            Buffer.BlockCopy(key, 0, desKey, 0, 8);

            for (int i = 0; i < (plaintext.Length / 8); i++)
            {
                byte[] plaintextBytes = new byte[8];
                Buffer.BlockCopy(plaintext, i * 8, plaintextBytes, 0, 8);
                byte[] whitenedPlaintextBytes = input_whitener.Crypt(plaintextBytes);
                byte[] encryptedBytes =
                    output_whitener.Crypt(EncryptBytes(desKey, new byte[8], whitenedPlaintextBytes));
                Buffer.BlockCopy(encryptedBytes, 0, ciphertext, i * 8, encryptedBytes.Length);
            }

            return ciphertext;
        }

        public static byte[] Decrypt(byte[] key, byte[] ciphertext)
        {
            if (key == null) throw new ArgumentNullException("key");
            if (key.Length != 16)
                throw new ArgumentOutOfRangeException("key length must be 16 bytes");
            if (ciphertext == null) throw new ArgumentNullException("ciphertext");
            if ((ciphertext.Length % 8) != 0)
                throw new Exception("ciphertext length must be a multiple of 8 bytes");

            byte[] input_whitening_key = new byte[8];
            Buffer.BlockCopy(key, 8, input_whitening_key, 0, 8);
            byte[] output_whitening_key = BuildOutputWhiteningKey(key);

            XOR input_whitener = new XOR(input_whitening_key);
            XOR output_whitener = new XOR(output_whitening_key);
            byte[] plaintext = new byte[ciphertext.Length];
            byte[] desKey = new byte[8];
            Buffer.BlockCopy(key, 0, desKey, 0, 8);

            for (int i = 0; i < (ciphertext.Length / 8); i++)
            {
                byte[] ciphertextBytes = new byte[8];
                Buffer.BlockCopy(ciphertext, i * 8, ciphertextBytes, 0, 8);
                byte[] whitenedCiphertextBytes = output_whitener.Crypt(ciphertextBytes);
                byte[] decryptedBytes =
                    input_whitener.Crypt(DecryptBytes(desKey, new byte[8], whitenedCiphertextBytes));
                Buffer.BlockCopy(decryptedBytes, 0, plaintext, i * 8, decryptedBytes.Length);
            }

            return plaintext;
        }

        private static byte[] BuildOutputWhiteningKey(byte[] key)
        {
            byte[] output_whitener = new byte[8];

            byte clorox_i = 0x00;
            for (int i = 0; i < 8; i++ )
            {
                clorox_i = (byte)(output_whitener[0] ^ output_whitener[1] & 0xff);
                for (int j = 0; j < 7; j++)
                    output_whitener[j] = output_whitener[j + 1];
                output_whitener[7] = (byte)(clorox[clorox_i] ^ key[i] & 0xff);
            }

            for (int i = 0; i < 8; i++)
            {
                clorox_i = (byte)(output_whitener[0] ^ output_whitener[1]);
                for (int j = 0; j < 7; j++)
                    output_whitener[j] = output_whitener[j + 1];
                output_whitener[7] = (byte)(clorox[clorox_i] ^ key[i+8]);
            }

            return output_whitener;
        }

        private static byte[] EncryptBytes(byte[] key, byte[] iv, byte[] bytes)
        {
            if (key == null) throw new ArgumentNullException("key");
            if (key.Length < 1) throw new ArgumentOutOfRangeException("key length must be at least 1 byte");
            if (iv == null) throw new ArgumentNullException("iv");
            if (bytes == null) throw new ArgumentNullException("bytes");
            if (bytes.Length < 1) throw new ArgumentOutOfRangeException("bytes length must be at least 1 byte");

            byte[] encrypted;

            using (DESCryptoServiceProvider des = new DESCryptoServiceProvider())
            {
                des.BlockSize = 64;
                des.Key = key;
                des.IV = iv;
                des.Mode = CipherMode.CBC;
                des.Padding = PaddingMode.None;

                ICryptoTransform encryptor = des.CreateEncryptor();

                using (MemoryStream ms = new MemoryStream())
                {
                    using (CryptoStream cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
                    {
                        cs.Write(bytes, 0, bytes.Length);
                        encrypted = ms.ToArray();
                    }
                }
            }

            return encrypted;
        }

        private static byte[] DecryptBytes(byte[] key, byte[] iv, byte[] bytes)
        {
            if (key == null) throw new ArgumentNullException("key");
            if (key.Length < 1) throw new ArgumentOutOfRangeException("key length must be at least 1 byte");
            if (iv == null) throw new ArgumentNullException("iv");
            if (bytes == null) throw new ArgumentNullException("bytes");
            if (bytes.Length < 1) throw new ArgumentOutOfRangeException("bytes length must be at least 1 byte");

            byte[] decrypted;

            using (DESCryptoServiceProvider des = new DESCryptoServiceProvider())
            {
                des.BlockSize = 64;
                des.Key = key;
                des.IV = iv;
                des.Mode = CipherMode.CBC;
                des.Padding = PaddingMode.None;

                ICryptoTransform decryptor = des.CreateDecryptor();

                using (MemoryStream ms = new MemoryStream())
                {
                    using (CryptoStream cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Write))
                    {
                        cs.Write(bytes, 0, bytes.Length);
                        decrypted = ms.ToArray();
                    }
                }
            }

            return decrypted;
        }
    }
}
