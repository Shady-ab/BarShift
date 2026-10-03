using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class GameAudio : MonoBehaviour
{
    private AudioSource source;
    private AudioClip clickClip;
    private AudioClip pourClip;
    private AudioClip successClip;
    private AudioClip failClip;
    private AudioClip shakeClip;
    private AudioClip coinClip;

    private void Awake()
    {
        source = GetComponent<AudioSource>();
        source.playOnAwake = false;
        source.spatialBlend = 0f;

        clickClip = Tone("click", 520f, 0.045f, 0.18f);
        pourClip = Tone("pour", 330f, 0.08f, 0.16f);
        successClip = TwoTone("success", 620f, 820f, 0.18f, 0.20f);
        failClip = TwoTone("fail", 260f, 190f, 0.22f, 0.18f);
        shakeClip = Noise("shake", 0.16f, 0.12f);
        coinClip = TwoTone("coin", 880f, 1100f, 0.13f, 0.18f);
    }

    public void Click() => Play(clickClip);
    public void Pour() => Play(pourClip);
    public void Shake() => Play(shakeClip);
    public void Coin() => Play(coinClip);

    public void Result(ReactionTier tier)
    {
        Play(tier == ReactionTier.Perfect || tier == ReactionTier.Good ? successClip : failClip);
    }

    private void Play(AudioClip clip)
    {
        if (clip != null && source != null)
            source.PlayOneShot(clip);
    }

    private static AudioClip Tone(string clipName, float frequency, float duration, float volume)
    {
        const int sampleRate = 44100;
        int sampleCount = Mathf.CeilToInt(duration * sampleRate);
        float[] samples = new float[sampleCount];
        for (int i = 0; i < sampleCount; i++)
        {
            float fade = 1f - (float)i / sampleCount;
            samples[i] = Mathf.Sin(2f * Mathf.PI * frequency * i / sampleRate) * volume * fade;
        }

        AudioClip clip = AudioClip.Create(clipName, sampleCount, 1, sampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }

    private static AudioClip TwoTone(string clipName, float first, float second, float duration, float volume)
    {
        const int sampleRate = 44100;
        int sampleCount = Mathf.CeilToInt(duration * sampleRate);
        float[] samples = new float[sampleCount];
        int split = sampleCount / 2;
        for (int i = 0; i < sampleCount; i++)
        {
            float frequency = i < split ? first : second;
            float fade = 1f - (float)i / sampleCount;
            samples[i] = Mathf.Sin(2f * Mathf.PI * frequency * i / sampleRate) * volume * fade;
        }

        AudioClip clip = AudioClip.Create(clipName, sampleCount, 1, sampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }

    private static AudioClip Noise(string clipName, float duration, float volume)
    {
        const int sampleRate = 44100;
        int sampleCount = Mathf.CeilToInt(duration * sampleRate);
        float[] samples = new float[sampleCount];
        uint seed = 123456789;
        for (int i = 0; i < sampleCount; i++)
        {
            seed = 1664525u * seed + 1013904223u;
            float random = ((seed >> 8) & 0xFFFFFF) / 16777215f * 2f - 1f;
            float fade = 1f - (float)i / sampleCount;
            samples[i] = random * volume * fade;
        }

        AudioClip clip = AudioClip.Create(clipName, sampleCount, 1, sampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }
}
