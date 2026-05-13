namespace Endpoint.Site.Models.MindMap;

public sealed class MindMapExportPayload
{
    public List<MindMapNodePayload> Nodes { get; set; } = new();

    public List<MindMapLinkPayload> Links { get; set; } = new();
}

public sealed class MindMapNodePayload
{
    public string Id { get; set; } = "";

    public string? Text { get; set; }
}

public sealed class MindMapLinkPayload
{
    public string From { get; set; } = "";

    public string To { get; set; } = "";
}
