namespace WzComparerX.WzLib;

public sealed record WzPackageHeader(
    WzPackageFormat Format,
    string Signature,
    string SourcePath,
    string Copyright,
    int HeaderSize,
    long DataSize,
    long FileSize,
    long DirectoryStartPosition,
    int? EncryptedVersion = null,
    bool IsEncryptedVersionMissing = false,
    uint? Hash1 = null,
    uint? Hash2 = null,
    bool IsModernPkg2Header = false)
{
    public bool IsValid => Format is not WzPackageFormat.Unknown;
}
