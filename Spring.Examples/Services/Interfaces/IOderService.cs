namespace Spring.Examples.Services;

[Bean]
[Bind(typeof(OderService))]
public interface IOderService
{
    void GenerateOder(string userName, string id);
}