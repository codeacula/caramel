global using System.Collections.Concurrent;
global using System.Net.WebSockets;
global using System.Text.Json;

global using Caramel.Cache;
global using Caramel.Core.API;
global using Caramel.Core.Conversations;
global using Caramel.Core.Logging;
global using Caramel.Core.People;
global using Caramel.Core.Reminders.Requests;
global using Caramel.Core.ToDos.Requests;
global using Caramel.Domain.Common.Enums;
global using Caramel.Domain.People.ValueObjects;
global using Caramel.GRPC;
global using Caramel.Twitch.Handlers;

global using FluentResults;

global using Microsoft.Extensions.Configuration;
global using Microsoft.Extensions.DependencyInjection;
global using Microsoft.Extensions.Hosting;
global using Microsoft.Extensions.Logging;
global using Microsoft.Extensions.Logging.Console;

global using StackExchange.Redis;

global using TwitchLib.Api;
global using TwitchLib.Api.Core;
global using TwitchLib.Api.Helix;
global using TwitchLib.EventSub.Websockets;
global using TwitchLib.EventSub.Websockets.Core.EventArgs;

