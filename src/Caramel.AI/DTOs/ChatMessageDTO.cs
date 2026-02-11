using Caramel.AI.Enums;

namespace Caramel.AI.DTOs;

public sealed record ChatMessageDTO(ChatRole Role, string Content, DateTime CreatedOn);
