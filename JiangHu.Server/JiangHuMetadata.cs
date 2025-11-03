#nullable disable

using SPTarkov.Server.Core.Models.Spt.Mod;
using SemanticVersioning;
using System.Collections.Generic;

using SRange = SemanticVersioning.Range;

namespace JiangHu.Server;

public record JiangHuMetadata : AbstractModMetadata
{
    public override string ModGuid { get; init; } = "com.jianghu.mod";
    public override string Name { get; init; } = "Jiang Hu";
    public override string Author { get; init; } = "hiokree";

    public override SemanticVersioning.Version Version { get; init; } = new SemanticVersioning.Version(1, 0, 0);

    public override SRange SptVersion { get; init; } = new SRange("~4.0.0");

    public override List<string> Contributors { get; init; } = new List<string> { "hiokree" };
    public override Dictionary<string, SRange> ModDependencies { get; init; } = new Dictionary<string, SRange>();
    public override List<string> Incompatibilities { get; init; } = new List<string>();

    public override bool? IsBundleMod { get; init; } = false;

    public override string Url { get; init; } = "https://github.com/Hiokree/Jiang-Hu/tree/main";
    public override string License { get; init; } = "MIT";
}

#nullable restore
