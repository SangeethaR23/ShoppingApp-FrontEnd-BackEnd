using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using Moq;
using ShoppingWebApi.Services;

namespace ShoppingApp.Tests.Services;

public class JwtTokenServiceTests
{
    // ── Helpers ───────────────────────────────────────────────────────────────

    private const string ValidKey      = "super-secret-key-that-is-long-enough-32chars!";
    private const string ValidIssuer   = "ShoppingApp";
    private const string ValidAudience = "ShoppingAppUsers";
    private const int    ExpiryMinutes = 60;

    private static JwtTokenService BuildSut(
        string key      = ValidKey,
        string issuer   = ValidIssuer,
        string audience = ValidAudience,
        string expires  = "60")
    {
        var configMock = new Mock<IConfiguration>();
        configMock.Setup(c => c["Jwt:Key"]).Returns(key);
        configMock.Setup(c => c["Jwt:Issuer"]).Returns(issuer);
        configMock.Setup(c => c["Jwt:Audience"]).Returns(audience);
        configMock.Setup(c => c["Jwt:ExpiresMinutes"]).Returns(expires);
        return new JwtTokenService(configMock.Object);
    }

    private static ClaimsPrincipal ValidateToken(string token, string key, string issuer, string audience)
    {
        var handler = new JwtSecurityTokenHandler();
        var parameters = new TokenValidationParameters
        {
            ValidateIssuer           = true,
            ValidIssuer              = issuer,
            ValidateAudience         = true,
            ValidAudience            = audience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey         = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)),
            ValidateLifetime         = true,
            ClockSkew                = TimeSpan.Zero
        };
        return handler.ValidateToken(token, parameters, out _);
    }

    private static IEnumerable<Claim> UserClaims(int userId = 1, string role = "User") =>
    [
        new Claim(JwtRegisteredClaimNames.Sub, userId.ToString()),
        new Claim(JwtRegisteredClaimNames.Email, "user@test.com"),
        new Claim(ClaimTypes.Role, role),
        new Claim("userId", userId.ToString())
    ];

    // ── Token structure ───────────────────────────────────────────────────────

    [Fact]
    public void CreateAccessToken_ReturnsNonEmptyString()
    {
        var sut   = BuildSut();
        var token = sut.CreateAccessToken(UserClaims(), out _);

        Assert.False(string.IsNullOrWhiteSpace(token));
    }

    [Fact]
    public void CreateAccessToken_ReturnsValidJwtFormat()
    {
        var sut   = BuildSut();
        var token = sut.CreateAccessToken(UserClaims(), out _);

        // JWT has exactly 3 dot-separated parts
        Assert.Equal(3, token.Split('.').Length);
    }

    [Fact]
    public void CreateAccessToken_CanBeParsedByHandler()
    {
        var sut     = BuildSut();
        var token   = sut.CreateAccessToken(UserClaims(), out _);
        var handler = new JwtSecurityTokenHandler();

        Assert.True(handler.CanReadToken(token));
    }

    // ── Expiry ────────────────────────────────────────────────────────────────

    [Fact]
    public void CreateAccessToken_ExpiresAtUtc_IsInFuture()
    {
        var sut = BuildSut();
        sut.CreateAccessToken(UserClaims(), out var exp);

        Assert.True(exp > DateTime.UtcNow);
    }

    [Fact]
    public void CreateAccessToken_ExpiresAtUtc_IsApproximately60MinutesFromNow()
    {
        var sut   = BuildSut(expires: "60");
        var before = DateTime.UtcNow;
        sut.CreateAccessToken(UserClaims(), out var exp);

        var diff = exp - before;
        Assert.InRange(diff.TotalMinutes, 59, 61);
    }

    [Fact]
    public void CreateAccessToken_CustomExpiry_IsRespected()
    {
        var sut   = BuildSut(expires: "30");
        var before = DateTime.UtcNow;
        sut.CreateAccessToken(UserClaims(), out var exp);

        var diff = exp - before;
        Assert.InRange(diff.TotalMinutes, 29, 31);
    }

    [Fact]
    public void CreateAccessToken_InvalidExpiryConfig_DefaultsTo60Minutes()
    {
        var sut    = BuildSut(expires: "notanumber");
        var before = DateTime.UtcNow;
        sut.CreateAccessToken(UserClaims(), out var exp);

        var diff = exp - before;
        Assert.InRange(diff.TotalMinutes, 59, 61);
    }

    // ── Signature & validation ────────────────────────────────────────────────

    [Fact]
    public void CreateAccessToken_ValidatesWithCorrectKey()
    {
        var sut   = BuildSut();
        var token = sut.CreateAccessToken(UserClaims(), out _);

        var ex = Record.Exception(() => ValidateToken(token, ValidKey, ValidIssuer, ValidAudience));
        Assert.Null(ex);
    }

    [Fact]
    public void CreateAccessToken_FailsValidationWithWrongKey()
    {
        var sut   = BuildSut();
        var token = sut.CreateAccessToken(UserClaims(), out _);

        Assert.ThrowsAny<Exception>(() =>
            ValidateToken(token, "wrong-key-that-is-also-long-enough-32chars!", ValidIssuer, ValidAudience));
    }

    [Fact]
    public void CreateAccessToken_FailsValidationWithWrongIssuer()
    {
        var sut   = BuildSut();
        var token = sut.CreateAccessToken(UserClaims(), out _);

        Assert.ThrowsAny<Exception>(() =>
            ValidateToken(token, ValidKey, "WrongIssuer", ValidAudience));
    }

    [Fact]
    public void CreateAccessToken_FailsValidationWithWrongAudience()
    {
        var sut   = BuildSut();
        var token = sut.CreateAccessToken(UserClaims(), out _);

        Assert.ThrowsAny<Exception>(() =>
            ValidateToken(token, ValidKey, ValidIssuer, "WrongAudience"));
    }

    // ── Claims ────────────────────────────────────────────────────────────────

    [Fact]
    public void CreateAccessToken_ContainsSubClaim()
    {
        var sut     = BuildSut();
        var token   = sut.CreateAccessToken(UserClaims(userId: 5), out _);
        var jwt     = new JwtSecurityTokenHandler().ReadJwtToken(token);

        Assert.Contains(jwt.Claims, c => c.Type == JwtRegisteredClaimNames.Sub && c.Value == "5");
    }

    [Fact]
    public void CreateAccessToken_ContainsEmailClaim()
    {
        var sut   = BuildSut();
        var token = sut.CreateAccessToken(UserClaims(), out _);
        var jwt   = new JwtSecurityTokenHandler().ReadJwtToken(token);

        Assert.Contains(jwt.Claims, c => c.Type == JwtRegisteredClaimNames.Email && c.Value == "user@test.com");
    }

    [Fact]
    public void CreateAccessToken_ContainsRoleClaim()
    {
        var sut   = BuildSut();
        var token = sut.CreateAccessToken(UserClaims(role: "Admin"), out _);
        var jwt   = new JwtSecurityTokenHandler().ReadJwtToken(token);

        Assert.Contains(jwt.Claims, c => c.Value == "Admin");
    }

    [Fact]
    public void CreateAccessToken_ContainsUserIdClaim()
    {
        var sut   = BuildSut();
        var token = sut.CreateAccessToken(UserClaims(userId: 7), out _);
        var jwt   = new JwtSecurityTokenHandler().ReadJwtToken(token);

        Assert.Contains(jwt.Claims, c => c.Type == "userId" && c.Value == "7");
    }

    [Fact]
    public void CreateAccessToken_EmptyClaims_StillReturnsValidToken()
    {
        var sut   = BuildSut();
        var token = sut.CreateAccessToken([], out _);

        Assert.False(string.IsNullOrWhiteSpace(token));
        Assert.Equal(3, token.Split('.').Length);
    }

    [Fact]
    public void CreateAccessToken_MultipleClaims_AllPresent()
    {
        var sut    = BuildSut();
        var claims = new[]
        {
            new Claim("custom1", "value1"),
            new Claim("custom2", "value2"),
            new Claim("custom3", "value3")
        };
        var token = sut.CreateAccessToken(claims, out _);
        var jwt   = new JwtSecurityTokenHandler().ReadJwtToken(token);

        Assert.Contains(jwt.Claims, c => c.Type == "custom1" && c.Value == "value1");
        Assert.Contains(jwt.Claims, c => c.Type == "custom2" && c.Value == "value2");
        Assert.Contains(jwt.Claims, c => c.Type == "custom3" && c.Value == "value3");
    }

    // ── Issuer / Audience in token ────────────────────────────────────────────

    [Fact]
    public void CreateAccessToken_TokenHasCorrectIssuer()
    {
        var sut   = BuildSut();
        var token = sut.CreateAccessToken(UserClaims(), out _);
        var jwt   = new JwtSecurityTokenHandler().ReadJwtToken(token);

        Assert.Equal(ValidIssuer, jwt.Issuer);
    }

    [Fact]
    public void CreateAccessToken_TokenHasCorrectAudience()
    {
        var sut   = BuildSut();
        var token = sut.CreateAccessToken(UserClaims(), out _);
        var jwt   = new JwtSecurityTokenHandler().ReadJwtToken(token);

        Assert.Contains(ValidAudience, jwt.Audiences);
    }

    // ── Algorithm ─────────────────────────────────────────────────────────────

    [Fact]
    public void CreateAccessToken_UsesHmacSha256Algorithm()
    {
        var sut   = BuildSut();
        var token = sut.CreateAccessToken(UserClaims(), out _);
        var jwt   = new JwtSecurityTokenHandler().ReadJwtToken(token);

        Assert.Equal(SecurityAlgorithms.HmacSha256, jwt.Header.Alg);
    }

    // ── Two tokens are different ──────────────────────────────────────────────

    [Fact]
    public void CreateAccessToken_CalledTwice_ReturnsDifferentTokens()
    {
        var sut    = BuildSut();
        var token1 = sut.CreateAccessToken(UserClaims(), out _);
        // Small delay to ensure different iat
        Thread.Sleep(1100);
        var token2 = sut.CreateAccessToken(UserClaims(), out _);

        Assert.NotEqual(token1, token2);
    }
}
