namespace WzComparerX.WzLib;

public sealed record WzImageSoundInspection(
    int Version,
    int Duration,
    int SoundDeclaration,
    string MajorType,
    string SubType,
    bool FixedSizeSamples,
    bool TemporalCompression,
    string FormatType,
    int? FormatExtraLength,
    long DataOffset,
    int DataLength)
{
    public override string ToString()
    {
        return $"version={Version}, duration={Duration}, soundDecl={SoundDeclaration}, dataLength={DataLength}, dataOffset={DataOffset}";
    }
}
