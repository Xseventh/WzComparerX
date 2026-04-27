using WzComparerX.WzLib;

namespace WzComparerX.Core;

public sealed record ResourceDocument(Guid Id, string SourcePath, RawResourceNode Root);

