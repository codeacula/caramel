using FluentResults;

namespace Caramel.Core;

/// <summary>
/// Extension methods for FluentResults Result types.
/// Provides helper methods for safely wrapping async operations, chaining results, and handling common error scenarios.
/// </summary>
public static class ResultExtensions
{
  /// <summary>
  /// Collects all error messages from a Result into a single string, separated by the specified separator.
  /// </summary>
  /// <param name="result">The result containing errors.</param>
  /// <param name="separator">The separator to use between error messages. Defaults to ", ".</param>
  /// <returns>A string containing all error messages, or an empty string if there are no errors.</returns>
  public static string GetErrorMessages(this ResultBase result, string separator = ", ")
  {
    return result.Errors.Count == 0
      ? string.Empty
      : string.Join(separator, result.Errors.Select(e => e.Message));
  }

  /// <summary>
  /// Pattern 1: Safely wraps an async operation in a try-catch block, returning a Result with proper error handling.
  /// Automatically handles OperationCanceledException by re-throwing it, and catches InvalidOperationException 
  /// and general Exception separately for different error messages.
  /// </summary>
  /// <param name="operation">The async operation to execute.</param>
  /// <param name="invalidOpErrorMessage">The error message to use for InvalidOperationException.</param>
  /// <param name="generalErrorMessage">The error message to use for general exceptions.</param>
  /// <returns>A Result containing the operation's result or an error.</returns>
  public static async Task<Result<T>> ExecuteAsync<T>(
    Func<Task<T>> operation,
    string invalidOpErrorMessage,
    string generalErrorMessage)
  {
    try
    {
      var result = await operation();
      return Result.Ok(result);
    }
    catch (OperationCanceledException)
    {
      throw;
    }
    catch (InvalidOperationException ex)
    {
      return Result.Fail<T>($"{invalidOpErrorMessage}: {ex.Message}");
    }
    catch (Exception ex)
    {
      return Result.Fail<T>($"{generalErrorMessage}: {ex.Message}");
    }
  }

  /// <summary>
  /// Pattern 1 variant: Safely wraps an async operation without returning a value.
  /// </summary>
  /// <param name="operation">The async operation to execute.</param>
  /// <param name="invalidOpErrorMessage">The error message to use for InvalidOperationException.</param>
  /// <param name="generalErrorMessage">The error message to use for general exceptions.</param>
  /// <returns>A Result indicating success or failure.</returns>
  public static async Task<Result> ExecuteAsync(
    Func<Task> operation,
    string invalidOpErrorMessage,
    string generalErrorMessage)
  {
    try
    {
      await operation();
      return Result.Ok();
    }
    catch (OperationCanceledException)
    {
      throw;
    }
    catch (InvalidOperationException ex)
    {
      return Result.Fail($"{invalidOpErrorMessage}: {ex.Message}");
    }
    catch (Exception ex)
    {
      return Result.Fail($"{generalErrorMessage}: {ex.Message}");
    }
  }

  /// <summary>
  /// Pattern 2: Chains Result operations using bind semantics. If the first result fails, 
  /// returns the failure. Otherwise, applies the next operation to the success value.
  /// </summary>
  /// <param name="result">The initial result.</param>
  /// <param name="nextOperation">The operation to apply if the result is successful.</param>
  /// <returns>The result of the next operation, or the original failure.</returns>
  public static async Task<Result<TNext>> BindAsync<T, TNext>(
    this Task<Result<T>> result,
    Func<T, Task<Result<TNext>>> nextOperation)
   {
     var awaitedResult = await result;

     if (awaitedResult.IsFailed)
    {
      return Result.Fail<TNext>(awaitedResult.Errors);
    }

    return await nextOperation(awaitedResult.Value);
  }

  /// <summary>
  /// Pattern 2 variant: Chains Result operations without returning a value from the next operation.
  /// </summary>
  /// <param name="result">The initial result.</param>
  /// <param name="nextOperation">The operation to apply if the result is successful.</param>
  /// <returns>The result of the next operation, or the original failure.</returns>
  public static async Task<Result> BindAsync<T>(
    this Task<Result<T>> result,
    Func<T, Task<Result>> nextOperation)
   {
     var awaitedResult = await result;

     if (awaitedResult.IsFailed)
    {
      return Result.Fail(awaitedResult.Errors);
    }

    return await nextOperation(awaitedResult.Value);
  }

  /// <summary>
  /// Pattern 2 variant: Synchronous bind for non-async operations.
  /// </summary>
  /// <param name="result">The initial result.</param>
  /// <param name="nextOperation">The operation to apply if the result is successful.</param>
  /// <returns>The result of the next operation, or the original failure.</returns>
  public static Result<TNext> Bind<T, TNext>(
    this Result<T> result,
    Func<T, Result<TNext>> nextOperation)
  {
    if (result.IsFailed)
    {
      return Result.Fail<TNext>(result.Errors);
    }

    return nextOperation(result.Value);
  }

  /// <summary>
  /// Pattern 3: Applies a function to the success value and returns a new Result.
  /// If the result is failed, returns the failure unchanged.
  /// </summary>
  /// <param name="result">The initial result.</param>
  /// <param name="mapping">The function to apply to the success value.</param>
  /// <returns>A new Result with the mapped value or the original failure.</returns>
  public static Result<TNext> Map<T, TNext>(
    this Result<T> result,
    Func<T, TNext> mapping)
  {
    if (result.IsFailed)
    {
      return Result.Fail<TNext>(result.Errors);
    }

    return Result.Ok(mapping(result.Value));
  }

  /// <summary>
  /// Pattern 3 variant: Async version of Map.
  /// </summary>
  /// <param name="result">The initial result.</param>
  /// <param name="mapping">The async function to apply to the success value.</param>
  /// <returns>A new Result with the mapped value or the original failure.</returns>
  public static async Task<Result<TNext>> MapAsync<T, TNext>(
    this Task<Result<T>> result,
    Func<T, Task<TNext>> mapping)
   {
     var awaitedResult = await result;

     if (awaitedResult.IsFailed)
    {
      return Result.Fail<TNext>(awaitedResult.Errors);
    }

    var mappedValue = await mapping(awaitedResult.Value);
    return Result.Ok(mappedValue);
  }

  /// <summary>
  /// Pattern 4: Converts a Result{T} to a Result{TNext} with a provided value when successful.
  /// Useful for changing the return type of a result while preserving the success/failure state.
  /// </summary>
  /// <param name="result">The initial result.</param>
  /// <param name="valueIfSuccess">The value to use if the result is successful.</param>
  /// <returns>A new Result with the provided value or the original failure.</returns>
  public static Result<TNext> ToResult<T, TNext>(
    this Result<T> result,
    TNext valueIfSuccess)
  {
    if (result.IsFailed)
    {
      return Result.Fail<TNext>(result.Errors);
    }

    return Result.Ok(valueIfSuccess);
  }

  /// <summary>
  /// Pattern 4 variant: Converts Result{T} to Result{TNext} when the result contains no useful value
  /// but we need to continue the chain.
  /// </summary>
  /// <param name="result">The initial result.</param>
  /// <returns>A new Result with default value or the original failure.</returns>
  public static Result<TNext> ToResult<T, TNext>(this Result<T> result)
    where TNext : new()
  {
    if (result.IsFailed)
    {
      return Result.Fail<TNext>(result.Errors);
    }

    return Result.Ok(new TNext());
  }

  /// <summary>
  /// Pattern 5: Executes a side effect if the result is successful, then returns the original result.
  /// Useful for logging, caching, or other side effects without transforming the result.
  /// </summary>
  /// <param name="result">The initial result.</param>
  /// <param name="sideEffect">The side effect operation to execute.</param>
  /// <returns>The original result unchanged.</returns>
  public static async Task<Result<T>> ThenAsync<T>(
    this Task<Result<T>> result,
    Func<T, Task> sideEffect)
  {
    var awaitedResult = await result;
    
    if (awaitedResult.IsSuccess)
    {
      await sideEffect(awaitedResult.Value);
    }

    return awaitedResult;
  }

  /// <summary>
  /// Pattern 5 variant: Synchronous version of Then.
  /// </summary>
  /// <param name="result">The initial result.</param>
  /// <param name="sideEffect">The side effect operation to execute.</param>
  /// <returns>The original result unchanged.</returns>
  public static Result<T> Then<T>(
    this Result<T> result,
    Action<T> sideEffect)
  {
    if (result.IsSuccess)
    {
      sideEffect(result.Value);
    }

    return result;
  }

  /// <summary>
  /// Pattern 5 variant: Side effect for Result without a value.
  /// </summary>
  /// <param name="result">The initial result.</param>
  /// <param name="sideEffect">The side effect operation to execute.</param>
  /// <returns>The original result unchanged.</returns>
  public static async Task<Result> ThenAsync(
    this Task<Result> result,
    Func<Task> sideEffect)
  {
    var awaitedResult = await result;
    
    if (awaitedResult.IsSuccess)
    {
      await sideEffect();
    }

    return awaitedResult;
  }

  /// <summary>
  /// Pattern 6: Handles failure scenarios with a recovery operation.
  /// If the result failed, executes the recovery function; otherwise returns the original result.
  /// </summary>
  /// <param name="result">The initial result.</param>
  /// <param name="recovery">The recovery operation to execute on failure.</param>
  /// <returns>The original result if successful, or the result of the recovery operation if failed.</returns>
  public static async Task<Result<T>> RecoverAsync<T>(
    this Task<Result<T>> result,
    Func<IReadOnlyList<IError>, Task<Result<T>>> recovery)
   {
     var awaitedResult = await result;

     if (awaitedResult.IsFailed)
    {
      return await recovery(awaitedResult.Errors);
    }

    return awaitedResult;
  }

  /// <summary>
  /// Pattern 6 variant: Synchronous version of Recover.
  /// </summary>
  /// <param name="result">The initial result.</param>
  /// <param name="recovery">The recovery operation to execute on failure.</param>
  /// <returns>The original result if successful, or the result of the recovery operation if failed.</returns>
  public static Result<T> Recover<T>(
    this Result<T> result,
    Func<IReadOnlyList<IError>, Result<T>> recovery)
  {
    if (result.IsFailed)
    {
      return recovery(result.Errors);
    }

    return result;
  }
}
