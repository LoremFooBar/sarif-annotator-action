namespace LoremFooBar.SarifAnnotatorAction.Model;

public class Annotation
{
    public string Path { get; init; } = "";
    public int Line { get; init; }
    public string? Summary { get; init; }
    public string? Details { get; init; }
}
