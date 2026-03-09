
using Caramel.Core.Security;

using Microsoft.AspNetCore.DataProtection;

namespace Caramel.Core.Tests.Security;

public class TokenEncryptionServiceTests
{
  private readonly Mock<IDataProtector> _mockProtector = new();
  private readonly TokenEncryptionService _service;

  public TokenEncryptionServiceTests()
  {
    var mockProvider = new Mock<IDataProtectionProvider>();
    _ = mockProvider
        .Setup(p => p.CreateProtector(It.IsAny<string>()))
        .Returns(_mockProtector.Object);

    _service = new TokenEncryptionService(mockProvider.Object);
  }

  [Fact]
  public void EncryptWithValidPlaintextReturnsEncryptedValue()
  {
    // Arrange
    const string plaintext = "test-access-token-123";
    const string expectedCiphertext = "encrypted-value-xyz";
    _ = _mockProtector.Setup(p => p.Protect(plaintext)).Returns(expectedCiphertext);

    // Act
    var result = _service.Encrypt(plaintext);

    // Assert
    _ = result.Should().Be(expectedCiphertext);
    _mockProtector.Verify(p => p.Protect(plaintext), Times.Once);
  }

  [Theory]
  [InlineData("")]
  [InlineData(null)]
  public void EncryptWithEmptyOrNullPlaintextThrowsArgumentException(string? plaintext)
  {
    // Act & Assert
    _ = _service.Invoking(s => s.Encrypt(plaintext))
        .Should().Throw<ArgumentException>()
        .WithMessage("*Plaintext token cannot be null or empty*");
  }

  [Fact]
  public void EncryptWhenDataProtectionFailsThrowsInvalidOperationException()
  {
    // Arrange
    const string plaintext = "test-token";
    _ = _mockProtector.Setup(p => p.Protect(plaintext))
        .Throws(new InvalidOperationException("Data Protection not configured"));

    // Act & Assert
    _ = _service.Invoking(s => s.Encrypt(plaintext))
        .Should().Throw<InvalidOperationException>()
        .WithMessage("*Token encryption failed*");
  }

  [Fact]
  public void TryDecryptWithValidCiphertextReturnsDecryptedValue()
  {
    // Arrange
    const string ciphertext = "encrypted-value-xyz";
    const string expectedPlaintext = "test-access-token-123";
    _ = _mockProtector.Setup(p => p.Unprotect(ciphertext)).Returns(expectedPlaintext);

    // Act
    var result = _service.TryDecrypt(ciphertext);

    // Assert
    _ = result.Should().Be(expectedPlaintext);
    _mockProtector.Verify(p => p.Unprotect(ciphertext), Times.Once);
  }

  [Fact]
  public void TryDecryptWithCorruptedCiphertextReturnsNull()
  {
    // Arrange
    const string ciphertext = "corrupted-value";
    _ = _mockProtector.Setup(p => p.Unprotect(ciphertext))
        .Throws(new System.Security.Cryptography.CryptographicException("Invalid ciphertext"));

    // Act
    var result = _service.TryDecrypt(ciphertext);

    // Assert
    _ = result.Should().BeNull();
  }

  [Theory]
  [InlineData("")]
  [InlineData(null)]
  public void TryDecryptWithEmptyOrNullCiphertextReturnsNull(string? ciphertext)
  {
    // Act
    var result = _service.TryDecrypt(ciphertext);

    // Assert
    _ = result.Should().BeNull();
  }

  [Fact]
  public void TryDecryptWhenDataProtectorThrowsReturnsNull()
  {
    // Arrange
    const string ciphertext = "some-value";
    _ = _mockProtector.Setup(p => p.Unprotect(ciphertext))
        .Throws(new Exception("Unexpected error"));

    // Act
    var result = _service.TryDecrypt(ciphertext);

    // Assert
    _ = result.Should().BeNull();
  }

  [Fact]
  public void EncryptAndTryDecryptRoundTripWithMockProtector()
  {
    // Arrange
    const string plaintext = "test-broadcaster-token";
    const string ciphertext = "encrypted-test-broadcaster-token";

    _ = _mockProtector.Setup(p => p.Protect(plaintext)).Returns(ciphertext);
    _ = _mockProtector.Setup(p => p.Unprotect(ciphertext)).Returns(plaintext);

    // Act
    var encrypted = _service.Encrypt(plaintext);
    var decrypted = _service.TryDecrypt(encrypted);

    // Assert
    _ = encrypted.Should().Be(ciphertext);
    _ = decrypted.Should().Be(plaintext);
  }
}
