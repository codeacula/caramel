using System.Globalization;

using Caramel.Core;
using Caramel.Core.People;
using Caramel.Core.ToDos;
using Caramel.Domain.People.Models;

using FluentResults;

namespace Caramel.AI;

public sealed class CaramelReminderMessageGenerator(
  ICaramelAIAgent caramelAIAgent,
  TimeProvider timeProvider,
  PersonConfig personConfig
) : IReminderMessageGenerator
{
  public async Task<Result<string>> GenerateReminderMessageAsync(
    Person person,
    IEnumerable<string> toDoDescriptions,
    CancellationToken cancellationToken = default)
  {
    try
    {
      var taskList = string.Join(", ", toDoDescriptions);

      // Get the user's timezone or use default
      var timeZoneId = person.TimeZoneId?.Value ?? personConfig.DefaultTimeZoneId;
      TimeZoneInfo timeZoneInfo;
      try
      {
        timeZoneInfo = TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
      }
      catch (TimeZoneNotFoundException)
      {
        return Result.Fail<string>($"Timezone '{timeZoneId}' is not recognized.");
      }
      catch (InvalidTimeZoneException)
      {
        return Result.Fail<string>($"Timezone '{timeZoneId}' is invalid.");
      }

      // Convert UTC time to user's timezone
      var utcNow = timeProvider.GetUtcDateTime();
      var offset = timeZoneInfo.GetUtcOffset(utcNow);
      var localTime = new DateTimeOffset(utcNow, TimeSpan.Zero).ToOffset(offset);

      var requestBuilder = caramelAIAgent
        .CreateReminderRequest(
          timeZoneId,
          localTime.ToString("yyyy-MM-ddTHH:mm:sszzz", CultureInfo.InvariantCulture),
          taskList
        );

      var result = await requestBuilder.ExecuteAsync(cancellationToken);

      if (!result.Success)
      {
        return Result.Fail<string>($"Failed to generate reminder message: {result.ErrorMessage}");
      }

      // Remove any surrounding quotes the LLM might add
      return Result.Ok(result.Content.Trim().Trim('"'));
    }
    catch (Exception ex)
    {
      return Result.Fail<string>($"Failed to generate reminder message: {ex.Message}");
    }
  }
}
