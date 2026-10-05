using AlDawahPharma.Infrastructure.Auth;
using Xunit;
using Xunit.Abstractions;

namespace AlDawahPharma.Tests;

public class PasswordHasherTests
{
    private readonly ITestOutputHelper _output;

    public PasswordHasherTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public void GenerateAndVerifyHashes()
    {
        var hasher = new PasswordHasher();
        string adminHash = hasher.HashPassword("admin123");
        string staffHash = hasher.HashPassword("rahim123");

        _output.WriteLine($"admin123: {adminHash}");
        _output.WriteLine($"rahim123: {staffHash}");

        Assert.True(hasher.VerifyPassword("admin123", adminHash));
        Assert.True(hasher.VerifyPassword("rahim123", staffHash));
    }
}
