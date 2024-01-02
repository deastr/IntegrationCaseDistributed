namespace Shared;

public record Item(long Id, string Content)
{
    public override string ToString() => $"{Id}:{Content}";
}