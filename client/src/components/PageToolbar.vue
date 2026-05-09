<script setup lang="ts">
import type { ClosedSprintItem } from '../types'

withDefaults(defineProps<{
  sprints?: ClosedSprintItem[]
  selectedSprintId?: number | null
  subTeams?: string[]
  selectedSubTeam?: string | null
  showSubTeamFilter?: boolean
  showAggregateOptions?: boolean
  sprintMode?: 'single' | 'multi'
  selectedLast?: number | null
}>(), {
  sprints: () => [],
  selectedSprintId: null,
  subTeams: () => [],
  selectedSubTeam: null,
  showSubTeamFilter: true,
  showAggregateOptions: false,
  sprintMode: 'single',
  selectedLast: null
})

const emit = defineEmits<{
  'update:selectedSprintId': [id: number]
  'update:selectedSubTeam': [subTeam: string | null]
  'update:sprintMode': [payload: { mode: 'single'; sprintId: number } | { mode: 'multi'; last: number | null }]
}>()

function onSprintChange(event: Event) {
  const value = (event.target as HTMLSelectElement).value
  if (value === 'last-3') {
    emit('update:sprintMode', { mode: 'multi', last: 3 })
  } else if (value === 'last-5') {
    emit('update:sprintMode', { mode: 'multi', last: 5 })
  } else if (value === 'all') {
    emit('update:sprintMode', { mode: 'multi', last: null })
  } else {
    const id = Number(value)
    emit('update:selectedSprintId', id)
    emit('update:sprintMode', { mode: 'single', sprintId: id })
  }
}

function onSubTeamChange(event: Event) {
  const value = (event.target as HTMLSelectElement).value
  emit('update:selectedSubTeam', value === '' ? null : value)
}

function currentSelectValue(props: {
  sprintMode?: 'single' | 'multi'
  selectedLast?: number | null
  selectedSprintId?: number | null
  sprints?: ClosedSprintItem[]
}): string {
  if (props.sprintMode === 'multi') {
    if (props.selectedLast === 3) return 'last-3'
    if (props.selectedLast === 5) return 'last-5'
    return 'all'
  }
  return String(props.selectedSprintId ?? props.sprints?.[0]?.id ?? '')
}
</script>

<template>
  <div class="flex items-center gap-3">
    <!-- Sprint selector -->
    <div class="relative flex items-center gap-2 h-9 px-3 rounded-md border border-border-default bg-surface-card text-text-secondary text-sm min-w-44">
      <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 20 20" fill="currentColor" class="w-4 h-4 shrink-0 text-text-muted">
        <path fill-rule="evenodd" d="M4 2a1 1 0 011 1v2.101a7.002 7.002 0 0111.601 2.566 1 1 0 11-1.885.666A5.002 5.002 0 005.999 7H9a1 1 0 010 2H4a1 1 0 01-1-1V3a1 1 0 011-1zm.008 9.057a1 1 0 011.276.61A5.002 5.002 0 0014.001 13H11a1 1 0 110-2h5a1 1 0 011 1v5a1 1 0 11-2 0v-2.101a7.002 7.002 0 01-11.601-2.566 1 1 0 01.61-1.276z" clip-rule="evenodd" />
      </svg>
      <select
        v-if="sprints.length > 0"
        :value="currentSelectValue({ sprintMode, selectedLast, selectedSprintId, sprints })"
        class="flex-1 bg-transparent outline-none cursor-pointer appearance-none truncate"
        @change="onSprintChange"
      >
        <option v-for="sprint in sprints" :key="sprint.id" :value="sprint.id">
          {{ sprint.name }}
        </option>
        <template v-if="showAggregateOptions">
          <option disabled value="">──────────</option>
          <option value="last-3">Last 3 Sprints</option>
          <option value="last-5">Last 5 Sprints</option>
          <option value="all">All Sprints</option>
        </template>
      </select>
      <span v-else class="flex-1 truncate text-text-muted">No sprints</span>
      <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 20 20" fill="currentColor" class="w-4 h-4 shrink-0 text-text-muted pointer-events-none">
        <path fill-rule="evenodd" d="M5.293 7.293a1 1 0 011.414 0L10 10.586l3.293-3.293a1 1 0 111.414 1.414l-4 4a1 1 0 01-1.414 0l-4-4a1 1 0 010-1.414z" clip-rule="evenodd" />
      </svg>
    </div>

    <!-- Sub-team filter -->
    <div v-if="showSubTeamFilter" class="relative flex items-center gap-2 h-9 px-3 rounded-md border border-border-default bg-surface-card text-text-secondary text-sm min-w-32">
      <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 20 20" fill="currentColor" class="w-4 h-4 shrink-0 text-text-muted">
        <path d="M9 6a3 3 0 11-6 0 3 3 0 016 0zM17 6a3 3 0 11-6 0 3 3 0 016 0zM12.93 17c.046-.327.07-.66.07-1a6.97 6.97 0 00-1.5-4.33A5 5 0 0119 16v1h-6.07zM6 11a5 5 0 015 5v1H1v-1a5 5 0 015-5z" />
      </svg>
      <select
        :value="selectedSubTeam ?? ''"
        class="flex-1 bg-transparent outline-none cursor-pointer appearance-none truncate"
        @change="onSubTeamChange"
      >
        <option value="">All</option>
        <option v-for="team in subTeams" :key="team" :value="team">
          {{ team }}
        </option>
      </select>
      <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 20 20" fill="currentColor" class="w-4 h-4 shrink-0 text-text-muted pointer-events-none">
        <path fill-rule="evenodd" d="M5.293 7.293a1 1 0 011.414 0L10 10.586l3.293-3.293a1 1 0 111.414 1.414l-4 4a1 1 0 01-1.414 0l-4-4a1 1 0 010-1.414z" clip-rule="evenodd" />
      </svg>
    </div>
  </div>
</template>
