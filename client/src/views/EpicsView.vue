<script setup lang="ts">
import PageLayout from '../components/PageLayout.vue'
import PageToolbar from '../components/PageToolbar.vue'
import EmptyState from '../components/EmptyState.vue'
</script>

<template>
  <PageLayout title="Epics">
    <template #toolbar>
      <PageToolbar />
    </template>

    <EmptyState
      title="No epic data yet"
      description="Sync a sprint to see epic progress here."
    >
      <template #icon>
        <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 20 20" fill="currentColor" class="w-12 h-12">
          <path d="M7 3a1 1 0 000 2h6a1 1 0 100-2H7zM4 7a1 1 0 011-1h10a1 1 0 110 2H5a1 1 0 01-1-1zM2 11a2 2 0 012-2h12a2 2 0 012 2v4a2 2 0 01-2 2H4a2 2 0 01-2-2v-4z" />
        </svg>
      </template>
    </EmptyState>
  </PageLayout>
</template>
