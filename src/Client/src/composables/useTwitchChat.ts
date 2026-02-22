import { ref, onMounted, onUnmounted } from "vue";

export interface TwitchChatMessage {
  messageId: string;
  broadcasterLogin: string;
  broadcasterUserId: string;
  chatterLogin: string;
  chatterDisplayName: string;
  chatterUserId: string;
  messageText: string;
  color: string;
  timestamp: string;
}

const MAX_MESSAGES = 200;
const RECONNECT_DELAY_MS = 3000;
const MAX_MESSAGE_LENGTH = 500;

export type ConnectionStatus = "connecting" | "connected" | "disconnected" | "error";
export type SendStatus = "idle" | "sending" | "error";

export function useTwitchChat() {
  const messages = ref<TwitchChatMessage[]>([]);
  const status = ref<ConnectionStatus>("disconnected");
  const errorMessage = ref<string | null>(null);
  const sendStatus = ref<SendStatus>("idle");
  const sendError = ref<string | null>(null);

  let socket: WebSocket | null = null;
  let reconnectTimer: ReturnType<typeof setTimeout> | null = null;
  let stopped = false;

  function getWsUrl(): string {
    const protocol = window.location.protocol === "https:" ? "wss" : "ws";
    const host = window.location.host;
    return `${protocol}://${host}/ws/chat`;
  }

  function connect() {
    if (stopped) return;

    status.value = "connecting";
    errorMessage.value = null;

    try {
      socket = new WebSocket(getWsUrl());
    } catch (err) {
      status.value = "error";
      errorMessage.value = err instanceof Error ? err.message : "Failed to create WebSocket";
      scheduleReconnect();
      return;
    }

    socket.onopen = () => {
      status.value = "connected";
      errorMessage.value = null;
    };

    socket.onmessage = (event: MessageEvent<string>) => {
      try {
        const msg = JSON.parse(event.data) as TwitchChatMessage;
        messages.value.push(msg);
        if (messages.value.length > MAX_MESSAGES) {
          messages.value.splice(0, messages.value.length - MAX_MESSAGES);
        }
      } catch {
        // Malformed message – ignore
      }
    };

    socket.onerror = () => {
      status.value = "error";
      errorMessage.value = "WebSocket connection error";
    };

    socket.onclose = (event: CloseEvent) => {
      socket = null;
      if (stopped) {
        status.value = "disconnected";
        return;
      }
      status.value = event.wasClean ? "disconnected" : "error";
      if (!event.wasClean) {
        errorMessage.value = `Connection closed unexpectedly (code ${event.code})`;
      }
      scheduleReconnect();
    };
  }

  function scheduleReconnect() {
    if (stopped) return;
    reconnectTimer = setTimeout(() => {
      reconnectTimer = null;
      connect();
    }, RECONNECT_DELAY_MS);
  }

  function disconnect() {
    stopped = true;
    if (reconnectTimer !== null) {
      clearTimeout(reconnectTimer);
      reconnectTimer = null;
    }
    if (socket) {
      socket.close(1000, "Client disconnect");
      socket = null;
    }
    status.value = "disconnected";
  }

  function clearMessages() {
    messages.value = [];
  }

  async function sendMessage(text: string): Promise<boolean> {
    const trimmed = text.trim();

    if (!trimmed) {
      sendError.value = "Message cannot be empty.";
      sendStatus.value = "error";
      return false;
    }

    if (trimmed.length > MAX_MESSAGE_LENGTH) {
      sendError.value = `Message exceeds ${MAX_MESSAGE_LENGTH} characters.`;
      sendStatus.value = "error";
      return false;
    }

    sendStatus.value = "sending";
    sendError.value = null;

    try {
      const response = await fetch("/chat/send", {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ message: trimmed }),
      });

      if (response.ok) {
        sendStatus.value = "idle";
        return true;
      }

      // Try to extract a meaningful error from the response body
      let detail = `Server responded with ${response.status}.`;
      try {
        const body = await response.json();
        if (typeof body?.detail === "string") detail = body.detail;
        else if (typeof body?.title === "string") detail = body.title;
      } catch {
        // Ignore parse errors – keep the status-code message
      }

      sendError.value = detail;
      sendStatus.value = "error";
      return false;
    } catch (err) {
      sendError.value = err instanceof Error ? err.message : "Failed to send message.";
      sendStatus.value = "error";
      return false;
    }
  }

  function clearSendError() {
    sendError.value = null;
    sendStatus.value = "idle";
  }

  onMounted(() => {
    stopped = false;
    connect();
  });

  onUnmounted(() => {
    disconnect();
  });

  return {
    messages,
    status,
    errorMessage,
    sendStatus,
    sendError,
    clearMessages,
    clearSendError,
    disconnect,
    sendMessage,
    maxMessageLength: MAX_MESSAGE_LENGTH,
  };
}
