using FluentResults;

namespace Caramel.Core.Tests;

public class ResultExtensionsTests
{
  [Fact]
  public void GetErrorMessagesReturnsEmptyStringWhenNoErrors()
  {
    // Arrange
    var result = Result.Ok();

    // Act
    var errorMessages = result.GetErrorMessages();

    // Assert
    Assert.Equal(string.Empty, errorMessages);
  }

  [Fact]
  public void GetErrorMessagesReturnsSingleErrorMessage()
  {
    // Arrange
    var result = Result.Fail("Error message");

    // Act
    var errorMessages = result.GetErrorMessages();

    // Assert
    Assert.Equal("Error message", errorMessages);
  }

  [Fact]
  public void GetErrorMessagesReturnsMultipleErrorMessagesWithDefaultSeparator()
  {
    // Arrange
    var result = Result.Fail("Error 1")
      .WithError("Error 2")
      .WithError("Error 3");

    // Act
    var errorMessages = result.GetErrorMessages();

    // Assert
    Assert.Equal("Error 1, Error 2, Error 3", errorMessages);
  }

  [Fact]
  public void GetErrorMessagesReturnsMultipleErrorMessagesWithCustomSeparator()
  {
    // Arrange
    var result = Result.Fail("Error 1")
      .WithError("Error 2")
      .WithError("Error 3");

    // Act
    var errorMessages = result.GetErrorMessages("; ");

    // Assert
    Assert.Equal("Error 1; Error 2; Error 3", errorMessages);
  }

  [Fact]
  public void GetErrorMessagesReturnsMultipleErrorMessagesWithNewlineSeparator()
  {
    // Arrange
    var result = Result.Fail("Error 1")
      .WithError("Error 2")
      .WithError("Error 3");

    // Act
    var errorMessages = result.GetErrorMessages("\n");

    // Assert
    Assert.Equal("Error 1\nError 2\nError 3", errorMessages);
  }

  [Fact]
  public void GetErrorMessagesWorksWithTypedResult()
  {
    // Arrange
    var result = Result.Fail<int>("Error 1")
      .WithError("Error 2");

    // Act
    var errorMessages = result.GetErrorMessages();

    // Assert
    Assert.Equal("Error 1, Error 2", errorMessages);
  }

  [Fact]
  public void GetErrorMessagesWorksWithEmptyTypedResult()
  {
    // Arrange
    var result = Result.Ok("Success");

    // Act
    var errorMessages = result.GetErrorMessages();

    // Assert
    Assert.Equal(string.Empty, errorMessages);
  }

  /// <summary>
  /// Pattern 1: ExecuteAsync Tests
  /// </summary>
  /// <returns></returns>
  [Fact]
  public async Task ExecuteAsyncWithValueSucceedsWhenOperationCompletesAsync()
  {
    // Arrange
    const int expectedValue = 42;
    async Task<int> operationAsync()
    {
      return expectedValue;
    }

    // Act
    var result = await ResultExtensions.ExecuteAsync(
      operationAsync,
      "Invalid operation",
      "Unexpected error");

    // Assert
    Assert.True(result.IsSuccess);
    Assert.Equal(expectedValue, result.Value);
  }

  [Fact]
  public async Task ExecuteAsyncWithValueFailsOnInvalidOperationExceptionAsync()
  {
    // Arrange
    static async Task<int> operationAsync()
    {
      throw new InvalidOperationException("Invalid state");
    }

    // Act
    var result = await ResultExtensions.ExecuteAsync(
      operationAsync,
      "Invalid operation",
      "Unexpected error");

    // Assert
    Assert.True(result.IsFailed);
    Assert.Contains("Invalid operation", result.GetErrorMessages());
  }

  [Fact]
  public async Task ExecuteAsyncWithValueFailsOnGeneralExceptionAsync()
  {
    // Arrange
    static async Task<int> operationAsync()
    {
      throw new Exception("General error");
    }

    // Act
    var result = await ResultExtensions.ExecuteAsync(
      operationAsync,
      "Invalid operation",
      "Unexpected error");

    // Assert
    Assert.True(result.IsFailed);
    Assert.Contains("Unexpected error", result.GetErrorMessages());
  }

  [Fact]
  public async Task ExecuteAsyncRethrowsOperationCanceledExceptionAsync()
  {
    // Arrange
    static async Task<int> operationAsync()
    {
      await Task.Delay(10);
      throw new OperationCanceledException();
    }

    // Act & Assert
    _ = await Assert.ThrowsAsync<OperationCanceledException>(
      () => ResultExtensions.ExecuteAsync(operationAsync, "Invalid", "Unexpected"));
  }

  [Fact]
  public async Task ExecuteAsyncWithoutValueSucceedsWhenOperationCompletesAsync()
  {
    // Arrange
    var operationCalled = false;
    async Task operationAsync()
    {
      operationCalled = true;
      await Task.Delay(0);
    }

    // Act
    var result = await ResultExtensions.ExecuteAsync(
      operationAsync,
      "Invalid operation",
      "Unexpected error");

    // Assert
    Assert.True(result.IsSuccess);
    Assert.True(operationCalled);
  }

  [Fact]
  public async Task ExecuteAsyncWithoutValueFailsOnExceptionAsync()
  {
    // Arrange
    static async Task operationAsync()
    {
      throw new Exception("Error occurred");
    }

    // Act
    var result = await ResultExtensions.ExecuteAsync(
      operationAsync,
      "Invalid operation",
      "Unexpected error");

    // Assert
    Assert.True(result.IsFailed);
  }

  /// <summary>
  /// Pattern 2: Bind Tests
  /// </summary>
  /// <returns></returns>
  [Fact]
  public async Task BindAsyncWithValueChainsSuccessfulResultsAsync()
  {
    // Arrange
    var initialTask = Task.FromResult(Result.Ok(10));

    // Act
    var result = await initialTask.BindAsync(value =>
      Task.FromResult(Result.Ok(value * 2)));

    // Assert
    Assert.True(result.IsSuccess);
    Assert.Equal(20, result.Value);
  }

  [Fact]
  public async Task BindAsyncWithValueReturnFailureFromSecondOperationAsync()
  {
    // Arrange
    var initialTask = Task.FromResult(Result.Ok(10));

    // Act
    var result = await initialTask.BindAsync(value =>
      Task.FromResult(Result.Fail<int>("Operation failed")));

    // Assert
    Assert.True(result.IsFailed);
    Assert.Equal("Operation failed", result.GetErrorMessages());
  }

  [Fact]
  public async Task BindAsyncWithValuePropagatesInitialFailureAsync()
  {
    // Arrange
    var initialTask = Task.FromResult(Result.Fail<int>("Initial failure"));

    // Act
    var result = await initialTask.BindAsync(value =>
      Task.FromResult(Result.Ok(value * 2)));

    // Assert
    Assert.True(result.IsFailed);
    Assert.Equal("Initial failure", result.GetErrorMessages());
  }

  [Fact]
  public async Task BindAsyncWithoutValueChainsResultsAsync()
  {
    // Arrange
    var initialTask = Task.FromResult(Result.Ok(10));
    var secondOperationCalled = false;

    // Act
    var result = await initialTask.BindAsync(value =>
    {
      secondOperationCalled = true;
      return Task.FromResult(Result.Ok());
    });

    // Assert
    Assert.True(result.IsSuccess);
    Assert.True(secondOperationCalled);
  }

  [Fact]
  public void BindSynchronousChainsSuccessfulResults()
  {
    // Arrange
    var initialResult = Result.Ok(10);

    // Act
    var result = initialResult.Bind(value => Result.Ok(value * 2));

    // Assert
    Assert.True(result.IsSuccess);
    Assert.Equal(20, result.Value);
  }

  [Fact]
  public void BindSynchronousPropagatesFailure()
  {
    // Arrange
    var initialResult = Result.Fail<int>("Initial failure");

    // Act
    var result = initialResult.Bind(value => Result.Ok(value * 2));

    // Assert
    Assert.True(result.IsFailed);
    Assert.Equal("Initial failure", result.GetErrorMessages());
  }

  /// <summary>
  /// Pattern 3: Map Tests
  /// </summary>
  [Fact]
  public void MapTransformsSuccessValue()
  {
    // Arrange
    var result = Result.Ok(10);

    // Act
    var mapped = result.Map(x => x * 2);

    // Assert
    Assert.True(mapped.IsSuccess);
    Assert.Equal(20, mapped.Value);
  }

  [Fact]
  public void MapPreservesFailure()
  {
    // Arrange
    var result = Result.Fail<int>("Error");

    // Act
    var mapped = result.Map(x => x * 2);

    // Assert
    Assert.True(mapped.IsFailed);
  }

  [Fact]
  public async Task MapAsyncTransformsValueAsynchronouslyAsync()
  {
    // Arrange
    var resultTask = Task.FromResult(Result.Ok(10));

    // Act
    var mapped = await resultTask.MapAsync(async x =>
    {
      await Task.Delay(0);
      return x * 2;
    });

    // Assert
    Assert.True(mapped.IsSuccess);
    Assert.Equal(20, mapped.Value);
  }

  [Fact]
  public async Task MapAsyncPreservesFailureAsync()
  {
    // Arrange
    var resultTask = Task.FromResult(Result.Fail<int>("Error"));

    // Act
    var mapped = await resultTask.MapAsync(async x =>
    {
      await Task.Delay(0);
      return x * 2;
    });

    // Assert
    Assert.True(mapped.IsFailed);
  }

  /// <summary>
  /// Pattern 4: ToResult Tests
  /// </summary>
  [Fact]
  public void ToResultConvertTypesOnSuccess()
  {
    // Arrange
    var result = Result.Ok(10);

    // Act
    var converted = result.ToResult("success");

    // Assert
    Assert.True(converted.IsSuccess);
    Assert.Equal("success", converted.Value);
  }

  [Fact]
  public void ToResultPreservesFailure()
  {
    // Arrange
    var result = Result.Fail<int>("Error");

    // Act
    var converted = result.ToResult("would not be used");

    // Assert
    Assert.True(converted.IsFailed);
    Assert.Equal("Error", result.GetErrorMessages());
  }

  [Fact]
  public void ToResultWithNewCreatesDefaultInstance()
  {
    // Arrange
    var result = Result.Ok(10);

    // Act
    var converted = result.ToResult<int, TestData>();

    // Assert
    Assert.True(converted.IsSuccess);
    Assert.NotNull(converted.Value);
  }

  /// <summary>
  /// Pattern 5: Then Tests
  /// </summary>
  /// <returns></returns>
  [Fact]
  public async Task ThenAsyncExecutesSideEffectAndReturnsOriginalResultAsync()
  {
    // Arrange
    var sideEffectCalled = false;
    var resultTask = Task.FromResult(Result.Ok(10));

    // Act
    var result = await resultTask.ThenAsync(async x =>
    {
      sideEffectCalled = true;
      await Task.Delay(0);
    });

    // Assert
    Assert.True(result.IsSuccess);
    Assert.Equal(10, result.Value);
    Assert.True(sideEffectCalled);
  }

  [Fact]
  public async Task ThenAsyncDoesNotExecuteSideEffectOnFailureAsync()
  {
    // Arrange
    var sideEffectCalled = false;
    var resultTask = Task.FromResult(Result.Fail<int>("Error"));

    // Act
    var result = await resultTask.ThenAsync(async x =>
    {
      sideEffectCalled = true;
      await Task.Delay(0);
    });

    // Assert
    Assert.True(result.IsFailed);
    Assert.False(sideEffectCalled);
  }

  [Fact]
  public void ThenSynchronousExecutesSideEffectAndReturnsOriginalResult()
  {
    // Arrange
    var sideEffectCalled = false;
    var result = Result.Ok(10);

    // Act
    var finalResult = result.Then(x => sideEffectCalled = true);

    // Assert
    Assert.True(finalResult.IsSuccess);
    Assert.Equal(10, finalResult.Value);
    Assert.True(sideEffectCalled);
  }

  [Fact]
  public async Task ThenAsyncWithoutValueExecutesSideEffectAsync()
  {
    // Arrange
    var sideEffectCalled = false;
    var resultTask = Task.FromResult(Result.Ok());

    // Act
    var result = await resultTask.ThenAsync(async () =>
    {
      sideEffectCalled = true;
      await Task.Delay(0);
    });

    // Assert
    Assert.True(result.IsSuccess);
    Assert.True(sideEffectCalled);
  }

  /// <summary>
  /// Pattern 6: Recover Tests
  /// </summary>
  /// <returns></returns>
  [Fact]
  public async Task RecoverAsyncReturnsOriginalSuccessResultAsync()
  {
    // Arrange
    var recoveryUsed = false;
    var resultTask = Task.FromResult(Result.Ok(10));

    // Act
    var result = await resultTask.RecoverAsync(async errors =>
    {
      recoveryUsed = true;
      await Task.Delay(0);
      return Result.Ok(99);
    });

    // Assert
    Assert.True(result.IsSuccess);
    Assert.Equal(10, result.Value);
    Assert.False(recoveryUsed);
  }

  [Fact]
  public async Task RecoverAsyncExecutesRecoveryOnFailureAsync()
  {
    // Arrange
    var recoveryUsed = false;
    var resultTask = Task.FromResult(Result.Fail<int>("Original error"));

    // Act
    var result = await resultTask.RecoverAsync(async errors =>
    {
      recoveryUsed = true;
      await Task.Delay(0);
      return Result.Ok(99);
    });

    // Assert
    Assert.True(result.IsSuccess);
    Assert.Equal(99, result.Value);
    Assert.True(recoveryUsed);
  }

  [Fact]
  public void RecoverSynchronousReturnsOriginalSuccessResult()
  {
    // Arrange
    var recoveryUsed = false;
    var result = Result.Ok(10);

    // Act
    var finalResult = result.Recover(errors =>
    {
      recoveryUsed = true;
      return Result.Ok(99);
    });

    // Assert
    Assert.True(finalResult.IsSuccess);
    Assert.Equal(10, finalResult.Value);
    Assert.False(recoveryUsed);
  }

  [Fact]
  public void RecoverSynchronousExecutesRecoveryOnFailure()
  {
    // Arrange
    var recoveryUsed = false;
    var result = Result.Fail<int>("Original error");

    // Act
    var finalResult = result.Recover(errors =>
    {
      recoveryUsed = true;
      return Result.Ok(99);
    });

    // Assert
    Assert.True(finalResult.IsSuccess);
    Assert.Equal(99, finalResult.Value);
    Assert.True(recoveryUsed);
  }

  /// <summary>
  /// Test data class
  /// </summary>
  private sealed class TestData
  {
  }
}
