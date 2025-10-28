import { createRouter, createWebHistory } from 'vue-router'
import HomeView from '../views/HomeView.vue'
import StreamerControlPanelView from '@/views/StreamerControlPanelView.vue'
import StreamerHUDView from '@/views/StreamerHUDView.vue'
import StreamOverlayView from '@/views/StreamOverlayView.vue'

const router = createRouter({
  history: createWebHistory(import.meta.env.BASE_URL),
  routes: [
    {
      path: '/',
      name: 'home',
      component: HomeView,
    },
    {
      path: '/control',
      name: 'control',
      component: StreamerControlPanelView,
    },
    {
      path: '/hud',
      name: 'hud',
      component: StreamerHUDView,
    },
    {
      path: '/overlay',
      name: 'overlay',
      component: StreamOverlayView,
    },
  ],
})

export default router
