using UnityEngine;
using System;
using System.IO;

public static class WavUtility
{
    public static byte[] FromAudioClip(AudioClip clip)
    {
        float[] samples = new float[clip.samples * clip.channels];
        clip.GetData(samples, 0);
        short[] intData = new short[samples.Length];
        byte[] bytesData = new byte[samples.Length * 2];

        const float rescaleFactor = 32767;
        for (int i = 0; i < samples.Length; i++)
        {
            intData[i] = (short)(samples[i] * rescaleFactor);
            byte[] byteArr = BitConverter.GetBytes(intData[i]);
            byteArr.CopyTo(bytesData, i * 2);
        }

        byte[] header = GetWavHeader(clip, bytesData.Length);
        byte[] wav = new byte[header.Length + bytesData.Length];
        Buffer.BlockCopy(header, 0, wav, 0, header.Length);
        Buffer.BlockCopy(bytesData, 0, wav, header.Length, bytesData.Length);
        return wav;
    }

    private static byte[] GetWavHeader(AudioClip clip, int dataLength)
    {
        int hz = clip.frequency;
        int channels = clip.channels;
        int samples = clip.samples;

        MemoryStream stream = new MemoryStream(44);
        BinaryWriter writer = new BinaryWriter(stream);

        writer.Write(System.Text.Encoding.UTF8.GetBytes("RIFF"));
        writer.Write(dataLength + 36);
        writer.Write(System.Text.Encoding.UTF8.GetBytes("WAVE"));
        writer.Write(System.Text.Encoding.UTF8.GetBytes("fmt "));
        writer.Write(16);
        writer.Write((ushort)1);
        writer.Write((ushort)channels);
        writer.Write(hz);
        writer.Write(hz * channels * 2);
        writer.Write((ushort)(channels * 2));
        writer.Write((ushort)16);
        writer.Write(System.Text.Encoding.UTF8.GetBytes("data"));
        writer.Write(dataLength);

        return stream.ToArray();
    }
}
