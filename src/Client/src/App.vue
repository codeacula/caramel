<script setup lang="ts">
import TwitchChat from "./components/TwitchChat.vue";
</script>

<template>
  <div class="app">
    <TwitchChat />
  </div>
</template>

<style>
*,
*::before,
*::after {
  box-sizing: border-box;
  margin: 0;
  padding: 0;
}

html,
body {
  height: 100%;
  background: #0e0e10;
  color: #efeff1;
}

#app {
  height: 100%;
}
</style>

<style scoped>
.app {
  height: 100vh;
  display: flex;
  flex-direction: column;
  padding: 16px;
}
</style>
