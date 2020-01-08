using NAudio.Wave;
using NLayer.NAudioSupport;
using UnityEngine;
using WaveFormRendererLib;

public class WaveformGenerator
{
    private int waveFormWidth = 512;
    private int waveFormHeight = 64;
    private int waveFormPeakCount = 32;
    private int spaceWidth = 9;
    private int peakWidth = 7;
    private float[] maximums = new float[500];
    private int currentFrame;

    private void GetWaveform(WaveStream reader, int width, int height, int peakCount, Action<Texture2D> callback)
    {
        int bytesPerSample = reader.WaveFormat.BitsPerSample / 8;
        var samples = reader.Length / bytesPerSample;
        var samplesPerPixel = (int) (samples / width);
        IPeakProvider peakProvider = new RmsPeakProvider(1);
        peakProvider.Init(reader.ToSampleProvider(), samplesPerPixel);
        int p = 0;
        var currentPeak = peakProvider.GetNextPeak();
        int peakNum = 0;
        Texture2D texture = new Texture2D(waveFormWidth, waveFormHeight);
        for (int x = 0; x < waveFormWidth; x++)
        for (int y = 0; y < waveFormHeight; y++)
            texture.SetPixel(x, y, Color.clear);
        float[] peakSizes = new float[peakCount];
        float peakSize = 0f;
        float maxPeakSize = 0f;
        while (p < width)
        {
            peakSize += Mathf.Abs(currentPeak.Max) + Mathf.Abs(currentPeak.Min);
            if (p % (peakWidth + spaceWidth) == 0)
            {
                peakSizes[peakNum] = peakSize;
                maxPeakSize = maxPeakSize < peakSize ? peakSize : maxPeakSize;
                peakSize = 0;
                peakNum++;
            }
            var nextPeak = peakProvider.GetNextPeak();
            currentPeak = nextPeak;
            p++;
        }
        peakNum = 0;
        p = 0;
        float k = waveFormHeight / (2 * maxPeakSize + peakWidth);
        while (p < width)
        {
            peakSize = peakSizes.Length <= peakNum ? 0f : peakSizes[peakNum];
            peakSize = peakSize * k;
            var pos = p % (peakWidth + spaceWidth);
            if (pos > spaceWidth)
            {
                int pX = pos - spaceWidth;
                pX = pX <= peakWidth / 2 ? pX : peakWidth - pX;
                var pS = peakSize + Mathf.Sqrt(3 / 4 * peakWidth - 2 * pX * pX + 2 * pX * peakWidth);
                for (int y = 0; y < waveFormHeight; y++)
                {
                    if (Mathf.Abs(y - waveFormHeight / 2) < pS)
                        texture.SetPixel(p, y, new Color(1, 1, 1, pS - Mathf.Abs(y - waveFormHeight / 2)));
                }
            }
            if ((p + spaceWidth) % (spaceWidth + peakWidth) == 0) peakNum++;
            p++;
        }
        texture.Apply();
        texture.filterMode = FilterMode.Bilinear;
        callback?.Invoke(texture);
    }
}