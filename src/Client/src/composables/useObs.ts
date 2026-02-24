import OBSWebSocket from 'obs-websocket-js';
import { ref } from 'vue';

const obs = new OBSWebSocket();
const isConnected = ref(false);

export function useObs() {
  const connect = async (url = 'ws://localhost:4455', password = '') => {
    try {
      await obs.connect(url, password);
      isConnected.value = true;
      console.log('Connected to OBS');
    } catch (error) {
      console.error('Failed to connect to OBS', error);
      isConnected.value = false;
    }
  };

  const disconnect = async () => {
    await obs.disconnect();
    isConnected.value = false;
  };

  const switchToScene = async (sceneName: string) => {
    if (!isConnected.value) {
      console.warn('Not connected to OBS');
      return;
    }
    try {
      await obs.call('SetCurrentProgramScene', { sceneName });
      console.log(`Switched to scene: ${sceneName}`);
    } catch (error) {
      console.error(`Failed to switch to scene ${sceneName}`, error);
    }
  };

  return {
    obs,
    isConnected,
    connect,
    disconnect,
    switchToScene
  };
}
