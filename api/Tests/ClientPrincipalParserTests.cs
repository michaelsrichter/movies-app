using MoviesApp.Api.Functions;
using Xunit;

namespace MoviesApp.Api.Tests;

public class ClientPrincipalParserTests
{
    [Fact]
    public void Parse_NullHeader_ReturnsNotAdmin()
    {
        var (user, isAdmin) = ClientPrincipalParser.Parse(null);
        Assert.Null(user);
        Assert.False(isAdmin);
    }

    [Fact]
    public void Parse_AdminRole_ReturnsIsAdminTrue()
    {
        const string json =
            "{\"userId\":\"1\",\"userDetails\":\"mike\",\"identityProvider\":\"github\",\"userRoles\":[\"anonymous\",\"authenticated\",\"admin\"]}";
        var header = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(json));

        var (user, isAdmin) = ClientPrincipalParser.Parse(header);

        Assert.Equal("mike", user);
        Assert.True(isAdmin);
    }

    [Fact]
    public void Parse_NonAdmin_ReturnsIsAdminFalse()
    {
        const string json =
            "{\"userDetails\":\"guest\",\"userRoles\":[\"anonymous\",\"authenticated\"]}";
        var header = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(json));

        var (user, isAdmin) = ClientPrincipalParser.Parse(header);

        Assert.Equal("guest", user);
        Assert.False(isAdmin);
    }

    [Fact]
    public void Parse_InvalidBase64_ReturnsNotAdmin()
    {
        var (user, isAdmin) = ClientPrincipalParser.Parse("not-valid-base64!!!");
        Assert.Null(user);
        Assert.False(isAdmin);
    }
}
