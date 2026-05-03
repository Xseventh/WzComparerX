namespace WzComparerX.WzLib;

public sealed record WzListFileInspection(
    string SourcePath,
    WzStringEncryptionKind StringEncryptionKind,
    int RawEntryCount,
    IReadOnlyList<WzListFileEntryInspection> Entries);

public sealed record WzListFileEntryInspection(
    int Index,
    string Path,
    int CharacterCount,
    long LengthPosition,
    long DataPosition,
    long TerminatorPosition);
