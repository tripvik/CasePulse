using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartPendant.MAUIHybrid.Helpers
{
    public static class AudioHelper
    {
        public static byte[] ConvertUnsignedToSigned(byte[] unsignedData)
        {
            byte[] signedData = new byte[unsignedData.Length];
            for (int i = 0; i < unsignedData.Length; i++)
            {
                signedData[i] = (byte)(unsignedData[i] - 128);
            }
            return signedData;
        }

        // Method to convert unsigned 8 bit to signed 16 bit PCM
        public static byte[] ConvertUnsigned8BitToSigned16Bit(byte[] unsignedData)
        {
            byte[] signedData = new byte[unsignedData.Length * 2];
            for (int i = 0; i < unsignedData.Length; i++)
            {
                short signedValue = (short)(unsignedData[i] - 128);
                signedData[i * 2] = (byte)(signedValue & 0xFF);
                signedData[i * 2 + 1] = (byte)((signedValue >> 8) & 0xFF);
            }
            return signedData;
        }

    }
}
