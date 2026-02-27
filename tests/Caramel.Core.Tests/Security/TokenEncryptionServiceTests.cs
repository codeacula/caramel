namespace Caramel.Core.Tests.Security;

using Caramel.Core.Security;
using Microsoft.AspNetCore.DataProtection;

public class TokenEncryptionServiceTests
{
    private readonly Mock<IDataProtector> _mockProtector = new();
    private readonly TokenEncryptionService _service;

    public TokenEncryptionServiceTests()
    {
        var mockProvider = new Mock<IDataProtectionProvider>();
        mockProvider
            .Setup(p => p.CreateProtector(It.IsAny<string>()))
            .Returns(_mockProtector.Object);

        _service = new TokenEncryptionService(mockProvider.Object);
    }

    [Fact]
    public void Encrypt_WithValidPlaintext_ReturnsEncryptedValue()
    {
        // Arrange
        var plaintext = "test-access-token-123";
        var expectedCiphertext = "encrypted-value-xyz";
        _mockProtector.Setup(p => p.Protect(plaintext)).Returns(expectedCiphertext);

        // Act
        var result = _service.Encrypt(plaintext);

        // Assert
        result.Should().Be(expectedCiphertext);
        _mockProtector.Verify(p => p.Protect(plaintext), Times.Once);
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void Encrypt_WithEmptyOrNullPlaintext_ThrowsArgumentException(string plaintext)
    {
        // Act & Assert
        _service.Invoking(s => s.Encrypt(plaintext))
            .Should().Throw<ArgumentException>()
            .WithMessage("*Plaintext token cannot be null or empty*");
    }

    [Fact]
    public void Encrypt_WhenDataProtectionFails_ThrowsInvalidOperationException()
    {
        // Arrange
        var plaintext = "test-token";
        _mockProtector.Setup(p => p.Protect(plaintext))
            .Throws(new InvalidOperationException("Data Protection not configured"));

        // Act & Assert
        _service.Invoking(s => s.Encrypt(plaintext))
            .Should().Throw<InvalidOperationException>()
            .WithMessage("*Token encryption failed*");
    }

    [Fact]
    public void TryDecrypt_WithValidCiphertext_ReturnsDecryptedValue()
    {
        // Arrange
        var ciphertext = "encrypted-value-xyz";
        var expectedPlaintext = "test-access-token-123";
        _mockProtector.Setup(p => p.Unprotect(ciphertext)).Returns(expectedPlaintext);

        // Act
        var result = _service.TryDecrypt(ciphertext);

        // Assert
        result.Should().Be(expectedPlaintext);
        _mockProtector.Verify(p => p.Unprotect(ciphertext), Times.Once);
    }

    [Fact]
    public void TryDecrypt_WithCorruptedCiphertext_ReturnsNull()
    {
        // Arrange
        var ciphertext = "corrupted-value";
        _mockProtector.Setup(p => p.Unprotect(ciphertext))
            .Throws(new System.Security.Cryptography.CryptographicException("Invalid ciphertext"));

        // Act
        var result = _service.TryDecrypt(ciphertext);

        // Assert
        result.Should().BeNull();
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void TryDecrypt_WithEmptyOrNullCiphertext_ReturnsNull(string ciphertext)
    {
        // Act
        var result = _service.TryDecrypt(ciphertext);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void TryDecrypt_WhenDataProtectorThrows_ReturnsNull()
    {
        // Arrange
        var ciphertext = "some-value";
        _mockProtector.Setup(p => p.Unprotect(ciphertext))
            .Throws(new Exception("Unexpected error"));

        // Act
        var result = _service.TryDecrypt(ciphertext);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void Encrypt_AndTryDecrypt_RoundTrip_WithMockProtector()
    {
        // Arrange
        var plaintext = "test-broadcaster-token";
        var ciphertext = "encrypted-test-broadcaster-token";
        
        _mockProtector.Setup(p => p.Protect(plaintext)).Returns(ciphertext);
        _mockProtector.Setup(p => p.Unprotect(ciphertext)).Returns(plaintext);

        // Act
        var encrypted = _service.Encrypt(plaintext);
        var decrypted = _service.TryDecrypt(encrypted);

        // Assert
        encrypted.Should().Be(ciphertext);
        decrypted.Should().Be(plaintext);
    }
}
