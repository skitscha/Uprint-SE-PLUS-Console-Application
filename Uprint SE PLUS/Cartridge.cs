// Ported from material.py and manager.py by David Slayton (2014); copyright below.

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
//      derived from this software without specific prior written permission.
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

using CartridgeWriterExtensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;

namespace CartridgeWriter
{

    // Each cartridge contains an EEPROM with encryted information about
    // the cartridge.  The Cartridge class contains methods to encrypt and
    // decrypt the information stored on the cartridge's EEPROM.

    // Typical structure on the EEPROM
    //        offset : len
    //        0x00   :   0x08    - Current Material Quantity
    //        0x08   ;   0x02    - Encrypted Current Material Quantity CRC
    //        0x0A   :   0x02    - Decrypted Current Material Quantity CRC
    //        0x02   :   0x01
    //        0x13   :   0x02    - Key Fragment CRC (0x14d0)
    //        0x15   :   0x02
    //        0x17   :   0x08    - Key Fragment
    //        0x1f   :   0x01
    //        0x20   :   0x08    - Initial use date
    //        0x28   :   0x08    - Manufacturing date
    //        0x30   :   0x08    - Serial number
    //        0x38   :   0x08    - Material type
    //        0x40   :   0x14    - Manufacturing lot
    //        0x54   :   0x02    - Eeprom version
    //        0x56   :   0x02
    //        0x58   :   0x08    - Initial material qty
    //        0x60   :   0x02    - Decrypted Info Block CRC
    //        0x62   :   0x02
    //        0x64   :   0x02    - Encrypted Info Block CRC
    //        0x66   :   0x02    -
    //        0x68   :   0x20    - Signature
    //        0x78   :   0x08    - Encrypted serial number
    // ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
    //        0x20   :   0x40    - Info Block
    //        0x00   :   0x08    - Current Material Quantity
    //        0x78   :   0x08    - Encrypted Serial Number


    // Field table
    // -----------
    // #    offset   length  name
    // 0     0x68    0x09    Signature
    // 1     0x68    0x10
    // 2     0x00    0x08    Current material qty
    // 3     0x08    0x02
    // 4     0x0A    0x02    Decrypted material qty CRC
    // 5     0x20    0x48    Info block
    // 6     0x66    0x02
    // 7     0x60    0x02
    // 8     0x20    0x08    Initial use date
    // 9     0x58    0x08    Iniial material qty
    // a     0x10    0x10
    // b     0x13    0x02    Key framgent crc
    // c     0x17    0x08    Key fragment
    // d     0x00    0x10
    // e     0x38    0x08    Material type
    // f     0x28    0x08    Manufacturing date
    // 10    0x40    0x14    Manufacturing lot
    // 11    0x30    0x08    Serial Number
    // 12    0x78    0x08    Encrypted Serial Number ?
    // 13    0x78    0x08    ^
    // 14    0x54    0x02    Eprom version?

    public class Cartridge
    {
        public byte[] _encrypted = null;
        private byte[] _decrypted = new byte[128];
        private Machine _machine = null;
        private byte[] _eepromUID = null;
        private byte[] _key = new byte[23];
        private byte[] _keyFragment = new byte[8];
        private byte[] _keyFragmentCRC = new byte[2];
        private byte[] __infolblock = new byte[2];
        private byte[] __infolblockCRC = new byte[2];
        private byte[] _plainContentCRC = new byte[2];
        private byte[] _currentMaterialQuantityCryptedCRC = new byte[2];
        private byte[] _currentMaterialQuantityCRC = new byte[2];
        private byte[] block1_xor_key = new byte[] { 0x07, 0x44, 0xDD, 0x84, 0xB5, 0x80, 0xA8, 0x9C };
        private byte[] block2_xor_key = new byte[] { 0xc0, 0xd3, 0x21, 0xa4, 0x41, 0x77, 0x22, 0xcb };
        private byte[] block3_xor_key = new byte[] { 0xaf, 0x04, 0xea, 0xd1, 0x5d, 0x52, 0x40, 0x3a };
        public Cartridge(byte[] encrypted, Machine machine, byte[] eepromUID)
        {
            _encrypted = encrypted;
            _machine = machine;
            _eepromUID = eepromUID;

            LoadCRC();
            BuildKey();
            Decrypt();
        }

        // Encrypted
        public byte[] Encrypted
        {
            get
            {
                Encrypt();
                return _encrypted;
            }
        }
        
        // EEPROMUID
        public byte[] EEPROMUID { get { return _eepromUID; } }

        

        // Serial number
        public double SerialNumber
        {
            get { return BitConverter.ToDouble(_decrypted, 0x30); }
            set { Buffer.BlockCopy(BitConverter.GetBytes(value), 0, _decrypted, 0x30, 8); }
        }

        // Material (see Material.cs)
        public Material Material
        {
            get { return Material.FromId(BitConverter.ToDouble(_decrypted, 0x38)); }
            set { Buffer.BlockCopy(BitConverter.GetBytes(value.Id), 0, _decrypted, 0x38, 8); }
        }

        // Manufacturing lot
        public string ManufacturingLot
        {
            get { return Encoding.ASCII.GetString(_decrypted, 0x40, 20).Split('\x00').First(); }
            set { Buffer.BlockCopy(Encoding.ASCII.GetBytes(value), 0, _decrypted, 0x40, value.Length); }
        }

        // Manufacturing date
        public DateTime ManfuacturingDate
        {
            get
            {
                int year = BitConverter.ToUInt16(_decrypted, 0x28) + 1900;
                int month = _decrypted[0x2a];
                int day = _decrypted[0x2b];
                int hour = _decrypted[0x2c];
                int minute = _decrypted[0x2d];
                int second = BitConverter.ToUInt16(_decrypted, 0x2e);
                return new DateTime(year, month, day, hour, minute, second);
            }
            set
            {
                Buffer.BlockCopy(BitConverter.GetBytes((ushort)(value.Year)), 0, _decrypted, 0x28, 2);
                Buffer.BlockCopy(BitConverter.GetBytes((ushort)(value.Month)), 0, _decrypted, 0x2a, 1);
                Buffer.BlockCopy(BitConverter.GetBytes((ushort)(value.Day)), 0, _decrypted, 0x2b, 1);
                Buffer.BlockCopy(BitConverter.GetBytes((ushort)(value.Hour)), 0, _decrypted, 0x2c, 1);
                Buffer.BlockCopy(BitConverter.GetBytes((ushort)(value.Minute)), 0, _decrypted, 0x2d, 1);
                Buffer.BlockCopy(BitConverter.GetBytes((ushort)(value.Second)), 0, _decrypted, 0x2e, 2);
            }
        }

        // Last use date
        public DateTime UseDate
        {
            get
            {
                int year = BitConverter.ToUInt16(_decrypted, 0x30) + 1900;
                int month = _decrypted[0x32];
                int day = _decrypted[0x33];
                int hour = _decrypted[0x34];
                int minute = _decrypted[0x35];
                int second = BitConverter.ToUInt16(_decrypted, 0x36);
                return new DateTime(year, month, day, hour, minute, second);
            }
            set
            {
                Buffer.BlockCopy(BitConverter.GetBytes((ushort)(value.Year)), 0, _decrypted, 0x30, 2);
                Buffer.BlockCopy(BitConverter.GetBytes((ushort)(value.Month)), 0, _decrypted, 0x32, 1);
                Buffer.BlockCopy(BitConverter.GetBytes((ushort)(value.Day)), 0, _decrypted, 0x33, 1);
                Buffer.BlockCopy(BitConverter.GetBytes((ushort)(value.Hour)), 0, _decrypted, 0x34, 1);
                Buffer.BlockCopy(BitConverter.GetBytes((ushort)(value.Minute)), 0, _decrypted, 0x35, 1);
                Buffer.BlockCopy(BitConverter.GetBytes((ushort)(value.Second)), 0, _decrypted, 0x36, 2);
            }
        }

        // Initial material quantity, in cubic inches
        public double InitialMaterialQuantity
        {
            get { return 16.3871*BitConverter.ToDouble(_decrypted, 0x58); }
            set { Buffer.BlockCopy(BitConverter.GetBytes(value), 0, _decrypted, 0x58, 8); }
        }

        // Remaining material quantity
        public double CurrentMaterialQuantity
        {
            get { return 16.3871*BitConverter.ToDouble(_decrypted, 0x00); }
            set { Buffer.BlockCopy(BitConverter.GetBytes(value), 0, _decrypted, 0x00, 8); }
        }

        // Key used to encrypt / decrypt
        public string KeyFragment { get { return _keyFragment.HexString(); } }

        // Version
        public ushort Version
        {
            get { return BitConverter.ToUInt16(_decrypted, 0x24); }
            set { Buffer.BlockCopy(BitConverter.GetBytes(value), 0, _decrypted, 0x24, 2); }
        }

        // Signature
        public string Signature
        {
            get { return Encoding.ASCII.GetString(_decrypted, 0x68, 9); }
            set { Buffer.BlockCopy(Encoding.ASCII.GetBytes(value), 0, _decrypted, 0x68, value.Length); }
        }

        //
        // Encrypt a packed cartridge into a crypted cartridge
        //
        private void Encrypt()
        {
            _decrypted.CopyTo(_encrypted, 0);

            //
            // Content
            //
            byte[] content = new byte[0x40];
            Buffer.BlockCopy(_decrypted, 0, content, 0, 0x40);
            // Checksum content
            Buffer.BlockCopy(Crc16_Checksum.Checksum(content), 0, _encrypted, 0x40, 2);
            // Encrypt content
            byte[] contentEncrypted = Desx_Crypto.Encrypt(_key, content);
            Buffer.BlockCopy(contentEncrypted, 0, _encrypted, 0, 0x40);
            // Checksum crypted content
            Buffer.BlockCopy(Crc16_Checksum.Checksum(contentEncrypted), 0, _encrypted, 0x46, 2);

            //
            // Current Material Quantity
            //
            byte[] currentMaterialQuantity = new byte[8];
            Buffer.BlockCopy(_decrypted, 0x58, currentMaterialQuantity, 0, 8);
            // Checksum current material quantity
            Buffer.BlockCopy(Crc16_Checksum.Checksum(currentMaterialQuantity), 0, _encrypted, 0x62, 2);
            // Encrypt current material quantity
            byte[] currentMaterialQuantityEncrypted = Desx_Crypto.Encrypt(_key, currentMaterialQuantity);
            Buffer.BlockCopy(currentMaterialQuantityEncrypted, 0, _encrypted, 0x58, 8);
            // Checksum crypted current material quantity
            Buffer.BlockCopy(Crc16_Checksum.Checksum(currentMaterialQuantityEncrypted), 0, _encrypted, 0x60, 2);
        }

        //
        // Decrypt a crypted cartridge into a packed cartridge
        //
        private void Decrypt()
        {
            _encrypted.CopyTo(_decrypted, 0);

            //
            // Info BLock
            //
            byte[] contentEncrypted = new byte[0x40];
            Buffer.BlockCopy(_encrypted, 0, contentEncrypted, 40, 0x20);

            // Info crypted content checksum
            if (!Crc16_Checksum.Checksum(contentEncrypted).SequenceEqual(__infolblockCRC))
                throw new Exception("invalid crypted content checksum");
            // Decrypt content
            byte[] content = Desx_Crypto.Decrypt(_key, contentEncrypted);
            Buffer.BlockCopy(content, 0, _decrypted, 0, 0x40);
            // Validating plaintext checksum
            if (!Crc16_Checksum.Checksum(content).SequenceEqual(_plainContentCRC))
                throw new Exception(
                    "invalid content checksum: should have " + _plainContentCRC.HexString() +
                    " but have " + Crc16_Checksum.Checksum(content).HexString());

            //
            // Current Material Quantity
            //
            byte[] currentMaterialQuantityEncrypted = new byte[8];
            Buffer.BlockCopy(_encrypted, 0x00, currentMaterialQuantityEncrypted, 0, 8);

            // Validate crypted current material quantity checksum
            if (!Crc16_Checksum.Checksum(currentMaterialQuantityEncrypted).SequenceEqual(_currentMaterialQuantityCryptedCRC))
                throw new Exception("invalid current material quantity checksum");
            // Decrypt current material quantity
            byte[] currentMaterialQuantityDecrypted = Desx_Crypto.Decrypt(_key, currentMaterialQuantityEncrypted);
            Buffer.BlockCopy(currentMaterialQuantityDecrypted, 0, _decrypted, 0x58, 8);
            // Validating current material quantity checksum
            if (!Crc16_Checksum.Checksum(currentMaterialQuantityDecrypted).SequenceEqual(_currentMaterialQuantityCRC))
                throw new Exception("invalid current material quantity checksum");
        }


        //
        // Build a key used to encrypt/decrypt a cartridge
        //
        private void BuildKey()
        {
            Buffer.BlockCopy(_encrypted, 0x17, _keyFragment, 0, 8);
            if (!Crc16_Checksum.Checksum(_keyFragment, 0x14d0).SequenceEqual(_keyFragmentCRC))
                throw new Exception("invalid keyfragment checksum: should have " + _keyFragmentCRC.HexString() + " but have "+ Crc16_Checksum.Checksum(_keyFragment).HexString());

            _keyFragment = shuffle_key_fragment(_keyFragment);

            byte[] machine_number = _machine.Number;

            _key[0] = (byte)(~_eepromUID[3] & 0xff);
            _key[1] = (byte)(~machine_number[3] & 0xff);
            _key[2] = (byte)(~machine_number[1] & 0xff);
            _key[3] = (byte)(~_keyFragment[0] & 0xff);
            _key[4] = (byte)(~_keyFragment[1] & 0xff);
            _key[5] = (byte)(~_keyFragment[2] & 0xff);
            _key[6] = (byte)(~_eepromUID[5] & 0xff);
            _key[7] = (byte)(~_keyFragment[3] & 0xff);
            _key[8] = (byte)(~machine_number[7] & 0xff);
            _key[9] = (byte)(~machine_number[5] & 0xff);
            _key[10] = (byte)(~machine_number[2] & 0xff);
            _key[11] = (byte)(~_eepromUID[1] & 0xff);
            _key[12] = (byte)(~_eepromUID[4] & 0xff);
            _key[13] = (byte)(~_keyFragment[4] & 0xff);
            _key[14] = (byte)(~_keyFragment[5] & 0xff);
            _key[15] = (byte)(~_eepromUID[6] & 0xff);
            _key[16] = (byte)(~_keyFragment[6] & 0xff);
            _key[17] = (byte)(~machine_number[0] & 0xff);
            _key[18] = (byte)(~machine_number[6] & 0xff);
            _key[19] = (byte)(~_eepromUID[0] & 0xff);
            _key[20] = (byte)(~machine_number[4] & 0xff);
            _key[21] = (byte)(~_keyFragment[7] & 0xff);
            _key[22] = (byte)(~_eepromUID[2] & 0xff);


            machine_number = null;
        }

        //
        // Build a key used to encrypt/decrypt a cartridge
        //
        private byte[] shuffle_key_fragment(byte[] keyfragment)
        {
            byte[] shuffled_key_fragment = new byte[8];
            shuffled_key_fragment[0] = keyfragment[5];
            shuffled_key_fragment[1] = keyfragment[6];
            shuffled_key_fragment[2] = keyfragment[1];
            shuffled_key_fragment[3] = keyfragment[0];
            shuffled_key_fragment[4] = keyfragment[3];
            shuffled_key_fragment[5] = keyfragment[4];
            shuffled_key_fragment[6] = keyfragment[7];
            shuffled_key_fragment[7] = keyfragment[2];
            return shuffled_key_fragment;

        }


        private void LoadCRC()
        {
            Buffer.BlockCopy(_encrypted, 0x13, _keyFragmentCRC, 0, 2);
            Buffer.BlockCopy(_encrypted, 0x66, __infolblockCRC, 0, 2);
            Buffer.BlockCopy(_encrypted, 0x60, _currentMaterialQuantityCryptedCRC, 0, 2);
            Buffer.BlockCopy(_encrypted, 0x62, _currentMaterialQuantityCRC, 0, 2);
        }
    }
}
