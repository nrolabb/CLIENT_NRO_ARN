using System;
using System.Security.Cryptography;
using System.Text;
using Game1.Assets.src.g;

public static class ClientVerifier
{

    private static readonly byte[] SECRET_KEY =
        Encoding.UTF8.GetBytes("NRO_SECRET_KEY_2024_CHANGE_ME");


    public static byte[] GenerateToken()
    {
        long timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

        byte[] tsBytes = new byte[8];
        long tmp = timestamp;
        for (int i = 7; i >= 0; i--)
        {
            tsBytes[i] = (byte)(tmp & 0xFF);
            tmp >>= 8;
        }
        using (var hmac = new HMACSHA256(SECRET_KEY))
        {
            byte[] hash = hmac.ComputeHash(tsBytes);

            byte[] token = new byte[40];
            Buffer.BlockCopy(tsBytes, 0, token, 0, 8);
            Buffer.BlockCopy(hash, 0, token, 8, 32);
            return token;
        }
    }
}