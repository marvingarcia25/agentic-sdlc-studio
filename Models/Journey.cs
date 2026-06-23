namespace AgenticSdlcStudio.Models;

/// <summary>Kind of artifact, used to pick a label/colour for the viewer.</summary>
public enum ArtifactKind
{
    Markdown,
    CSharp,
    Diff,
    Yaml,
    Dockerfile,
    Json,
    Bash,
    Text
}

/// <summary>One line in a scripted agent/human transcript.</summary>
/// <param name="Author">Display name, e.g. "Planning agent" or "You".</param>
/// <param name="Kind">"agent", "human", or "system" — drives styling.</param>
public record TranscriptLine(string Author, string Kind, string Text);

/// <summary>A displayed work product (code, YAML, etc.). Display-only; never executed.</summary>
public record Artifact(string Title, string Filename, ArtifactKind Kind, string Content);

/// <summary>One lifecycle stage the feature travels through.</summary>
public record Stage(
    string Key,
    string Name,
    string Icon,
    string Tagline,
    IReadOnlyList<string> Agents,
    string Narrative,
    IReadOnlyList<TranscriptLine> Transcript,
    IReadOnlyList<Artifact> Artifacts,
    string ApprovalPrompt);
