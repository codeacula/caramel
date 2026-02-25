global using System.Collections.Concurrent;
global using System.Text.Json;

global using Caramel.Cache;
global using Caramel.Core.API;
global using Caramel.Core.Conversations;
global using Caramel.Core.People;
global using Caramel.Core.Twitch;
global using Caramel.Domain.Common.Enums;
global using Caramel.Domain.People.ValueObjects;
global using Caramel.Domain.Twitch;
global using Caramel.GRPC;
global using Caramel.Twitch.Handlers;
global using Caramel.Twitch.Services;

global using MediatR;

global using Microsoft.Extensions.Configuration;
global using Microsoft.Extensions.DependencyInjection;
global using Microsoft.Extensions.Hosting;
global using Microsoft.Extensions.Logging;

global using StackExchange.Redis;

global using TwitchLib.EventSub.Websockets;
global using TwitchLib.EventSub.Websockets.Core.EventArgs;
