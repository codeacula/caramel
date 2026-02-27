global using System.Net;
global using System.Net.Http;
global using System.Text;
global using System.Text.Json;
global using System.Threading;
global using System.Threading.Tasks;

global using Caramel.Core.API;
global using Caramel.Core.Conversations;
global using Caramel.Core.People;
global using Caramel.Domain.Common.Enums;
global using Caramel.Domain.People.ValueObjects;
global using Caramel.Domain.Twitch;
global using Caramel.Twitch.Auth;
global using Caramel.Twitch.Controllers;
global using Caramel.Twitch.Extensions;
global using Caramel.Twitch.Handlers;
global using Caramel.Twitch.Services;

global using FluentAssertions;

global using FluentResults;

global using Microsoft.AspNetCore.Mvc;
global using Microsoft.Extensions.Logging;

global using Moq;

global using Xunit;
