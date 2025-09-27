namespace MicroBoy;

[Flags]
public enum Buttons : byte
{
    None = 0, A = 1, B = 2, Start = 4, Select = 8, Up = 16, Down = 32, Left = 64, Right = 128
}

public readonly struct Input
{
    public readonly Buttons Buttons;
    public Input(Buttons b) => Buttons = b;
    public bool IsDown(Buttons b) => (Buttons & b) != 0;
}

public static class MicroBoySpec
{
    public const int W = 160;
    public const int H = 144;
}

public interface ICartridge
{
    string Title { get; }
    string Author { get; }
    void Init();
    void Update(Input input, double dt);
    void Render(Span<byte> frame);

    int AudioSampleRate => 44100;
    int AudioChannelCount => 2;
    void MixAudio(Span<float> buffer) => buffer.Clear();
}
