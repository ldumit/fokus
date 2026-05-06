import { createRouter, createWebHistory } from 'vue-router'

const router = createRouter({
  history: createWebHistory(),
  routes: [
    {
      path: '/',
      name: 'dashboard',
      component: () => import('./views/DashboardView.vue')
    },
    {
      path: '/developers',
      name: 'developers',
      component: () => import('./views/DevelopersView.vue')
    },
    {
      path: '/sprints',
      name: 'sprints',
      component: () => import('./views/SprintsView.vue')
    },
    {
      path: '/epics',
      name: 'epics',
      component: () => import('./views/EpicsView.vue')
    },
    {
      path: '/settings',
      name: 'settings',
      component: () => import('./views/SettingsView.vue')
    }
  ]
})

export default router
