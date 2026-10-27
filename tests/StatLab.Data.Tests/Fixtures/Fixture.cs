namespace StatLab.Data.Tests.Fixtures;

// Reads a JSON fixture from this project's Fixtures folder, copied next to the test DLL at build time.
internal static class Fixture
{
    public static string Read(string name)
    {
        return File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Fixtures", name));
    }
}
