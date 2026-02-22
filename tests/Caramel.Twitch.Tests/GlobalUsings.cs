global using System.Collections.Generic;
global using System.Linq;
global using System.Text;
global using System.Threading;
global using System.Threading.Tasks;

global using Caramel.Core.API;
global using Caramel.Core.Conversations;
global using Caramel.Core.People;
global using Caramel.Core.Reminders.Requests;
global using Caramel.Core.ToDos.Requests;
global using Caramel.Domain.Common.Enums;
global using Caramel.Domain.People.ValueObjects;
global using Caramel.Domain.ToDos.Models;
global using Caramel.Twitch;
global using Caramel.Twitch.Auth;
global using Caramel.Twitch.Extensions;
global using Caramel.Twitch.Handlers;

global using FluentResults;

global using Microsoft.Extensions.Logging;

global using Xunit;
global using Moq;
global using FluentAssertions;

