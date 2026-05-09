<script setup lang="ts">
import { onMounted, computed, watch } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { useDevelopersStore } from '../stores/developersStore'
import PageLayout from '../components/PageLayout.vue'
import PageToolbar from '../components/PageToolbar.vue'
import BaseCard from '../components/BaseCard.vue'
import EmptyState from '../components/EmptyState.vue'
import type { DeveloperThroughputEntry, SprintBreakdown } from '../types'

const route = useRoute()
const router = useRouter()
const store = useDevelopersStore()

onMounted(async () => {
  // Seed state from URL before initialize
  const sprintParam = route.query.sprint
  const lastParam = route.query.last

  if (lastParam) {
    const n = lastParam === 'all' ? null : Number(lastParam)
    store.sprintMode = 'multi'
    store.selectedLast = isNaN(n as number) ? null : n
  } else if (sprintParam && !isNaN(Number(sprintParam))) {
    store.sprintMode = 'single'
    store.selectedSprintId = Number(sprintParam)
  }

  await store.initialize()
})

// URL sync
watch(
  () => [store.sprintMode, store.selectedSprintId, store.selectedLast] as const,
  ([mode, sprintId, last]) => {
    if (mode === 'single' && sprintId !== null) {
      router.replace({ query: { sprint: String(sprintId) } })
    } else if (mode === 'multi') {
      if (last === null) {
        router.replace({ query: { last: 'all' } })
      } else {
        router.replace({ query: { last: String(last) } })
      }
    }
  }
)

function onSprintModeChange(payload: { mode: 'single'; sprintId: number } | { mode: 'multi'; last: number | null }) {
  if (payload.mode === 'single') {
    store.selectSprint(payload.sprintId)
  } else {
    store.selectLastN(payload.last)
  }
}

function onSubTeamChange(subTeam: string | null) {
  store.selectSubTeam(subTeam)
}

// Whether we're in multi-sprint mode
const isMultiSprint = computed(() => store.sprintMode === 'multi')

// Single sprint data — the one target sprint breakdown per developer
const singleSprintId = computed(() => {
  if (!isMultiSprint.value && store.throughput?.sprints.length === 1) {
    return store.throughput.sprints[0].id
  }
  return null
})

// Note: getBreakdown was removed — row pre-computation via singleSprintRows replaces template narrowing

interface SingleSprintRow {
  dev: DeveloperThroughputEntry
  bd: SprintBreakdown | undefined
}

const singleSprintRows = computed<SingleSprintRow[]>(() => {
  if (!store.throughput || singleSprintId.value === null) return []
  return store.throughput.developers.map(dev => ({
    dev,
    bd: dev.sprintBreakdowns.find(b => b.sprintId === singleSprintId.value!)
  }))
})

// Delta icon helper
function deltaIcon(direction: string | null): string {
  if (direction === 'up') return '▲'
  if (direction === 'down') return '▼'
  return '—'
}

function deltaClass(polarity: string | null, direction: string | null): string {
  if (direction === 'flat' || !direction) return 'text-text-secondary'
  if (polarity === 'positive') return 'text-status-success'
  if (polarity === 'negative') return 'text-status-danger'
  return 'text-text-secondary'
}

// Multi-sprint averaged values
function avgSpAssigned(dev: DeveloperThroughputEntry): string {
  const vals = dev.sprintBreakdowns.map(b => b.spAssigned)
  if (vals.length === 0) return '0'
  return (vals.reduce((a, b) => a + b, 0) / vals.length).toFixed(1)
}

function avgSpCompleted(dev: DeveloperThroughputEntry): string {
  const vals = dev.sprintBreakdowns.map(b => b.spCompleted)
  if (vals.length === 0) return '0'
  return (vals.reduce((a, b) => a + b, 0) / vals.length).toFixed(1)
}

function avgCompletionPercent(dev: DeveloperThroughputEntry): string {
  const vals = dev.sprintBreakdowns.map(b => b.completionPercent)
  if (vals.length === 0) return '0'
  return (vals.reduce((a, b) => a + b, 0) / vals.length).toFixed(1)
}

function avgTicketsDone(dev: DeveloperThroughputEntry): string {
  const vals = dev.sprintBreakdowns.map(b => b.ticketsDone)
  if (vals.length === 0) return '0'
  return (vals.reduce((a, b) => a + b, 0) / vals.length).toFixed(1)
}

function avgTicketsCarriedOver(dev: DeveloperThroughputEntry): string {
  const vals = dev.sprintBreakdowns.map(b => b.ticketsCarriedOver)
  if (vals.length === 0) return '0'
  return (vals.reduce((a, b) => a + b, 0) / vals.length).toFixed(1)
}

// Most recent sprint capacity for multi-sprint mode
function latestCapacity(dev: DeveloperThroughputEntry): number {
  if (dev.sprintBreakdowns.length === 0) return 100
  return dev.sprintBreakdowns[dev.sprintBreakdowns.length - 1].capacityPercent
}

// Trend chart data
const chartSeries = computed(() => {
  if (!store.throughput) return []
  return store.throughput.developers.map(dev => ({
    name: dev.displayName,
    data: dev.sprintBreakdowns.map(b => b.rollingAverageSpCompleted ?? null)
  }))
})

const chartOptions = computed(() => ({
  chart: {
    type: 'line',
    background: 'transparent',
    toolbar: { show: false },
    animations: { enabled: false }
  },
  stroke: { curve: 'smooth', width: 2 },
  xaxis: {
    categories: store.throughput?.sprints.map(s => s.name) ?? [],
    labels: { style: { colors: '#9ca3af', fontSize: '12px' } }
  },
  yaxis: {
    labels: { style: { colors: '#9ca3af', fontSize: '12px' } },
    title: { text: 'SP Completed', style: { color: '#9ca3af' } }
  },
  tooltip: {
    theme: 'dark',
    y: { formatter: (val: number | null) => val !== null ? val.toFixed(1) : 'N/A' }
  },
  legend: { labels: { colors: '#d1d5db' } },
  grid: { borderColor: '#374151' },
  theme: { mode: 'dark' }
}))

function onCapacityChange(dev: DeveloperThroughputEntry, sprintId: number, event: Event) {
  const value = parseInt((event.target as HTMLInputElement).value, 10)
  if (!isNaN(value) && value >= 0 && value <= 100) {
    store.updateCapacity(dev.accountId, sprintId, value)
  }
}
</script>

<template>
  <PageLayout title="Developers">
    <template #toolbar>
      <PageToolbar
        :sprints="store.closedSprints"
        :selected-sprint-id="store.selectedSprintId"
        :sub-teams="store.subTeams"
        :selected-sub-team="store.selectedSubTeam"
        :show-sub-team-filter="true"
        :show-aggregate-options="true"
        :sprint-mode="store.sprintMode"
        :selected-last="store.selectedLast"
        @update:selected-sprint-id="(id) => store.selectSprint(id)"
        @update:selected-sub-team="onSubTeamChange"
        @update:sprint-mode="onSprintModeChange"
      />
    </template>

    <!-- Initializing skeleton -->
    <div v-if="store.initializing" class="flex items-center justify-center h-64">
      <div class="text-text-muted text-sm">Loading...</div>
    </div>

    <!-- No closed sprints -->
    <template v-else-if="store.closedSprints.length === 0">
      <EmptyState
        title="No developer data yet"
        description="Sync a sprint to see developer metrics here."
      >
        <template #icon>
          <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 20 20" fill="currentColor" class="w-12 h-12">
            <path d="M9 6a3 3 0 11-6 0 3 3 0 016 0zM17 6a3 3 0 11-6 0 3 3 0 016 0zM12.93 17c.046-.327.07-.66.07-1a6.97 6.97 0 00-1.5-4.33A5 5 0 0119 16v1h-6.07zM6 11a5 5 0 015 5v1H1v-1a5 5 0 015-5z" />
          </svg>
        </template>
      </EmptyState>
    </template>

    <!-- Throughput content -->
    <template v-else>
      <div class="flex flex-col gap-6">

        <!-- Loading indicator -->
        <div v-if="store.loading" class="text-xs text-text-muted">Updating...</div>

        <!-- Single-sprint throughput table -->
        <BaseCard v-if="!isMultiSprint && singleSprintId !== null && store.throughput">
          <div class="text-sm font-medium text-text-primary mb-4">
            {{ store.throughput.sprints[0]?.name }}
          </div>
          <div class="overflow-x-auto">
            <table class="w-full text-sm">
              <thead>
                <tr class="text-text-muted text-left border-b border-border-default">
                  <th class="pb-2 pr-4 font-medium">Developer</th>
                  <th class="pb-2 pr-4 font-medium">Sub-Team</th>
                  <th class="pb-2 pr-4 font-medium text-right">SP Assigned</th>
                  <th class="pb-2 pr-4 font-medium text-right">SP Completed</th>
                  <th class="pb-2 pr-4 font-medium text-right">Completion %</th>
                  <th class="pb-2 pr-4 font-medium text-right">Tickets Done</th>
                  <th class="pb-2 pr-4 font-medium text-right">Carried Over</th>
                  <th class="pb-2 font-medium text-right">Capacity %</th>
                </tr>
              </thead>
              <tbody>
                <tr
                  v-for="row in singleSprintRows"
                  :key="row.dev.accountId"
                  class="border-b border-border-default last:border-0"
                >
                  <td class="py-2 pr-4">
                    <div class="flex items-center gap-2">
                      <img
                        v-if="row.dev.avatarUrl"
                        :src="row.dev.avatarUrl"
                        :alt="row.dev.displayName"
                        class="w-6 h-6 rounded-full shrink-0"
                      />
                      <div v-else class="w-6 h-6 rounded-full bg-surface-elevated shrink-0 flex items-center justify-center text-xs text-text-muted">
                        {{ row.dev.displayName.charAt(0).toUpperCase() }}
                      </div>
                      <span class="text-text-primary truncate">{{ row.dev.displayName }}</span>
                    </div>
                  </td>
                  <td class="py-2 pr-4 text-text-secondary">
                    <span v-if="row.dev.subTeam" class="text-xs bg-surface-elevated rounded px-2 py-0.5">{{ row.dev.subTeam }}</span>
                    <span v-else class="text-text-muted">—</span>
                  </td>
                  <template v-if="row.bd">
                    <td class="py-2 pr-4 text-right tabular-nums text-text-primary">
                      <span>{{ row.bd.spAssigned }}</span>
                      <span v-if="row.bd.spAssignedDelta !== null" :class="['ml-1 text-xs', deltaClass(row.bd.spAssignedDeltaPolarity, row.bd.spAssignedDeltaDirection)]">
                        {{ deltaIcon(row.bd.spAssignedDeltaDirection) }} {{ Math.abs(row.bd.spAssignedDelta).toFixed(1) }}
                      </span>
                    </td>
                    <td class="py-2 pr-4 text-right tabular-nums text-text-primary">
                      <span>{{ row.bd.spCompleted }}</span>
                      <span v-if="row.bd.spCompletedDelta !== null" :class="['ml-1 text-xs', deltaClass(row.bd.spCompletedDeltaPolarity, row.bd.spCompletedDeltaDirection)]">
                        {{ deltaIcon(row.bd.spCompletedDeltaDirection) }} {{ Math.abs(row.bd.spCompletedDelta).toFixed(1) }}
                      </span>
                    </td>
                    <td class="py-2 pr-4 text-right tabular-nums text-text-primary">
                      <span>{{ row.bd.completionPercent }}%</span>
                      <span v-if="row.bd.completionPercentDelta !== null" :class="['ml-1 text-xs', deltaClass(row.bd.completionPercentDeltaPolarity, row.bd.completionPercentDeltaDirection)]">
                        {{ deltaIcon(row.bd.completionPercentDeltaDirection) }} {{ Math.abs(row.bd.completionPercentDelta).toFixed(1) }}
                      </span>
                    </td>
                    <td class="py-2 pr-4 text-right tabular-nums text-text-primary">
                      <span>{{ row.bd.ticketsDone }}</span>
                      <span v-if="row.bd.ticketsDoneDelta !== null" :class="['ml-1 text-xs', deltaClass(row.bd.ticketsDoneDeltaPolarity, row.bd.ticketsDoneDeltaDirection)]">
                        {{ deltaIcon(row.bd.ticketsDoneDeltaDirection) }} {{ Math.abs(row.bd.ticketsDoneDelta) }}
                      </span>
                    </td>
                    <td class="py-2 pr-4 text-right tabular-nums text-text-primary">
                      <span>{{ row.bd.ticketsCarriedOver }}</span>
                      <span v-if="row.bd.ticketsCarriedOverDelta !== null" :class="['ml-1 text-xs', deltaClass(row.bd.ticketsCarriedOverDeltaPolarity, row.bd.ticketsCarriedOverDeltaDirection)]">
                        {{ deltaIcon(row.bd.ticketsCarriedOverDeltaDirection) }} {{ Math.abs(row.bd.ticketsCarriedOverDelta) }}
                      </span>
                    </td>
                    <td class="py-2 text-right">
                      <input
                        type="number"
                        :value="row.bd.capacityPercent"
                        min="0"
                        max="100"
                        class="w-16 text-right bg-surface-elevated border border-border-default rounded px-1 py-0.5 text-text-primary tabular-nums outline-none focus:border-accent-default"
                        @change="onCapacityChange(row.dev, singleSprintId!, $event)"
                      />
                    </td>
                  </template>
                  <template v-else>
                    <td class="py-2 pr-4 text-right text-text-muted" colspan="6">—</td>
                  </template>
                </tr>
              </tbody>
            </table>
          </div>
        </BaseCard>

        <!-- Multi-sprint throughput table -->
        <BaseCard v-if="isMultiSprint && store.throughput">
          <div class="text-sm font-medium text-text-primary mb-4">
            <span v-if="store.selectedLast">Last {{ store.selectedLast }} Sprints</span>
            <span v-else>All Sprints</span>
            — Averaged Values
          </div>
          <div class="overflow-x-auto">
            <table class="w-full text-sm">
              <thead>
                <tr class="text-text-muted text-left border-b border-border-default">
                  <th class="pb-2 pr-4 font-medium">Developer</th>
                  <th class="pb-2 pr-4 font-medium">Sub-Team</th>
                  <th class="pb-2 pr-4 font-medium text-right">Avg SP Assigned</th>
                  <th class="pb-2 pr-4 font-medium text-right">Avg SP Completed</th>
                  <th class="pb-2 pr-4 font-medium text-right">Avg Completion %</th>
                  <th class="pb-2 pr-4 font-medium text-right">Avg Tickets Done</th>
                  <th class="pb-2 pr-4 font-medium text-right">Avg Carried Over</th>
                  <th class="pb-2 font-medium text-right">Capacity % (latest)</th>
                </tr>
              </thead>
              <tbody>
                <tr
                  v-for="dev in store.throughput.developers"
                  :key="dev.accountId"
                  class="border-b border-border-default last:border-0"
                >
                  <td class="py-2 pr-4">
                    <div class="flex items-center gap-2">
                      <img
                        v-if="dev.avatarUrl"
                        :src="dev.avatarUrl"
                        :alt="dev.displayName"
                        class="w-6 h-6 rounded-full shrink-0"
                      />
                      <div v-else class="w-6 h-6 rounded-full bg-surface-elevated shrink-0 flex items-center justify-center text-xs text-text-muted">
                        {{ dev.displayName.charAt(0).toUpperCase() }}
                      </div>
                      <span class="text-text-primary truncate">{{ dev.displayName }}</span>
                    </div>
                  </td>
                  <td class="py-2 pr-4 text-text-secondary">
                    <span v-if="dev.subTeam" class="text-xs bg-surface-elevated rounded px-2 py-0.5">{{ dev.subTeam }}</span>
                    <span v-else class="text-text-muted">—</span>
                  </td>
                  <td class="py-2 pr-4 text-right tabular-nums text-text-primary">{{ avgSpAssigned(dev) }}</td>
                  <td class="py-2 pr-4 text-right tabular-nums text-text-primary">{{ avgSpCompleted(dev) }}</td>
                  <td class="py-2 pr-4 text-right tabular-nums text-text-primary">{{ avgCompletionPercent(dev) }}%</td>
                  <td class="py-2 pr-4 text-right tabular-nums text-text-primary">{{ avgTicketsDone(dev) }}</td>
                  <td class="py-2 pr-4 text-right tabular-nums text-text-primary">{{ avgTicketsCarriedOver(dev) }}</td>
                  <td class="py-2 text-right tabular-nums text-text-secondary">{{ latestCapacity(dev) }}%</td>
                </tr>
              </tbody>
            </table>
          </div>
        </BaseCard>

        <!-- Trend chart (multi-sprint mode only) -->
        <BaseCard v-if="isMultiSprint && store.throughput && store.throughput.developers.length > 0">
          <div class="text-sm font-medium text-text-primary mb-4">SP Completed Trend (3-Sprint Rolling Avg)</div>
          <apexchart
            type="line"
            height="300"
            :options="chartOptions"
            :series="chartSeries"
          />
        </BaseCard>

      </div>
    </template>
  </PageLayout>
</template>
