<script setup lang="ts">
import { RouterLink } from 'vue-router';

</script>

<template>
  <main>
    <ul>
      <li><RouterLink :to="{ name: 'control' }">Control</RouterLink></li>
      <li><RouterLink :to="{ name: 'hud' }">HUD</RouterLink></li>
      <li><RouterLink :to="{ name: 'overlay' }">Overlay</RouterLink></li>
    </ul>
  </main>
</template>
