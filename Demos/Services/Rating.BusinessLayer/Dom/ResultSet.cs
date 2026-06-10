namespace Shaper.Core.Dom;

public class ResultSet<T> : ResultBase
    where T : Entity
{
    public IEnumerable<T>? Value { get; set; }
}
public class ResultBase
{
    public string? Error { get; set; }
    public bool Success => string.IsNullOrEmpty(Error);
}

public class Result<T> : ResultBase where T : Entity
{
    public T? Value { get; set; }
}