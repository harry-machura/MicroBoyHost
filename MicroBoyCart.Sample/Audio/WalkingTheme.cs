using System;

namespace MicroBoyCart.Sample.Audio;

public sealed class WalkingTheme
{
    private readonly double[] melody = { 220.0, 246.0, 262.0, 294.0, 330.0, 294.0, 262.0, 246.0 };

    private double audioPhase;
    private double melodyTimer;
    private int melodyIndex;

    public void Reset()
    {
        audioPhase = 0;
        melodyTimer = 0;
        melodyIndex = 0;
    }

    public void MixAudio(Span<float> buffer, bool isMoving, int sampleRate, int channelCount)
    {
        if (channelCount <= 0)
        {
            buffer.Clear();
            return;
        }

        int samples = buffer.Length / channelCount;
        if (samples <= 0)
        {
            buffer.Clear();
            return;
        }

        double stepDuration = isMoving ? 0.18 : 0.32;
        double vibratoSpeed = isMoving ? 8.0 : 5.0;
        double vibratoDepth = isMoving ? 6.0 : 3.0;
        const double twoPi = Math.PI * 2.0;

        for (int i = 0; i < samples; i++)
        {
            melodyTimer += 1.0 / sampleRate;
            if (melodyTimer >= stepDuration)
            {
                melodyTimer -= stepDuration;
                melodyIndex = (melodyIndex + 1) % melody.Length;
            }

            double freq = melody[melodyIndex];
            double vibrato = Math.Sin((melodyTimer + i / (double)sampleRate) * twoPi * vibratoSpeed) * vibratoDepth;
            double phaseStep = twoPi * (freq + vibrato) / sampleRate;

            audioPhase += phaseStep;
            if (audioPhase >= twoPi)
            {
                audioPhase -= twoPi;
            }

            float sample = (float)(Math.Sin(audioPhase) * 0.18);
            int baseIndex = i * channelCount;
            for (int ch = 0; ch < channelCount; ch++)
            {
                buffer[baseIndex + ch] = sample;
            }
        }
    }
}
