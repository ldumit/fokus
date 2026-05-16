<script setup lang="ts">
import { computed, ref } from 'vue'
import type { QaWorkloadResponse, QaWorkloadEntry, QaWorkloadSingleEntry } from '../../types'
import BaseCard from '../BaseCard.vue'

const props = defineProps<{
  data: QaWorkloadResponse
  sprintMode: 'single' | 'multi'
}>()

const isMulti = computed(() => props.sprintMode === 'multi')
const multi = computed(() => props.data.multiSprint)
const single = computed(() => props.data.singleSprint)

// --- RAG helpers (pass rate thresholds: green >= 90, amber >= 70) ---
const PASS_RATE_GREEN = 90
const PASS_RATE_AMBER = 70

function passRateRagClass(rate: number): string {
  if (rate >= PASS_RATE_GREEN) return 'text-status-success'
  if (rate >= PASS_RATE_AMBER) return 'text-status-warning'
  return 'text-status-danger'
}

// --- Delta helpers ---
function deltaIcon(direction: string | null): string {
  if (direction === 'up') return '▲'
  if (direction === 'down') return '▼'
  return '—'
}

function deltaClass(polarity: string | null, direction: string | null): string {
  if (!direction || direction === 'flat') return 'text-text-secondary'
  if (polarity === 'positive') return 'text-status-success'
  if (polarity === 'negative') return 'text-status-danger'
  return 'text-text-secondary'
}

// --- Sorting ---
const sortColumn = ref<string | null>(null)
const sortDirection = ref<'asc' | 'desc'>('asc')

function toggleSort(column: string) {
  if (sortColumn.value === column) {
    sortDirection.value = sortDirection.value === 'asc' ? 'desc' : 'asc'
  } else {
    sortColumn.value = column
    sortDirection.value = 'asc'
  }
}

function sortIcon(column: string): string {
  if (sortColumn.value !== column) return ' ↕'
  return sortDirection.value === 'asc' ? ' ↑' : ' ↓'
}

function getVal(entry: QaWorkloadEntry | QaWorkloadSingleEntry, col: string): number {
  if (col === 'tesOwned') return entry.tesOwned
  if (col === 'runsCompleted') return entry.runsCompleted
  if (col === 'passCount') return entry.passCount
  if (col === 'failCount') return entry.failCount
  if (col === 'passRate') return entry.passRate
  if (col === 'storiesCovered') return entry.storiesCovered
  if (col === 'bugsFound') return entry.bugsFound
  return 0
}

const sortedMultiDevs = computed(() => {
  if (!multi.value) return []
  const devs = [...multi.value.developers]
  const col = sortColumn.value
  const dir = sortDirection.value
  if (!col) return devs
  return devs.sort((a, b) => {
    if (a.accountId === null) return 1
    if (b.accountId === null) return -1
    const diff = getVal(a, col) - getVal(b, col)
    return dir === 'asc' ? diff : -diff
  })
})

const sortedSingleDevs = computed(() => {
  if (!single.value) return []
  const devs = [...single.value.developers]
  const col = sortColumn.value
  const dir = sortDirection.value
  if (!col) return devs
  return devs.sort((a, b) => {
    if (a.accountId === null) return 1
    if (b.accountId === null) return -1
    const diff = getVal(a, col) - getVal(b, col)
    return dir === 'asc' ? diff : -diff
  })
})

// --- Distribution chart (stacked horizontal bar) ---
const distributionSeries = computed(() => {
  const devs = isMulti.value ? multi.value?.developers : single.value?.developers
  if (!devs || devs.length === 0) return []
  const sorted = [...devs].sort((a, b) => {
    if (a.accountId === null) return 1
    if (b.accountId === null) return -1
    return b.runsCompleted - a.runsCompleted
  })
  return [
    {
      name: 'Pass',
      data: sorted.map(d => d.passCount)
    },
    {
      name: 'Fail',
      data: sorted.map(d => d.failCount)
    }
  ]
})

const distributionCategories = computed(() => {
  const devs = isMulti.value ? multi.value?.developers : single.value?.developers
  if (!devs || devs.length === 0) return []
  return [...devs]
    .sort((a, b) => {
      if (a.accountId === null) return 1
      if (b.accountId === null) return -1
      return b.runsCompleted - a.runsCompleted
    })
    .map(d => d.displayName)
})

const distributionOptions = computed(() => ({
  chart: {
    type: 'bar',
    stacked: true,
    background: 'transparent',
    toolbar: { show: false },
    animations: { enabled: false }
  },
  plotOptions: {
    bar: {
      horizontal: true
    }
  },
  colors: ['#22c55e', '#ef4444'],
  xaxis: {
    labels: { style: { colors: '#9ca3af', fontSize: '12px' } },
    title: { text: 'Run Count', style: { color: '#9ca3af' } }
  },
  yaxis: {
    categories: distributionCategories.value,
    labels: { style: { colors: '#9ca3af', fontSize: '12px' } }
  },
  tooltip: {
    theme: 'dark'
  },
  legend: { labels: { colors: '#d1d5db' } },
  grid: { borderColor: '#374151' },
  theme: { mode: 'dark' }
}))

// --- Throughput trend chart (multi only) ---
const trendSeries = computed(() => {
  if (!multi.value) return []
  return multi.value.developers.map(dev => ({
    name: dev.displayName,
    data: multi.value!.sprints.map(s => {
      const bd = dev.sprintBreakdowns.find(b => b.sprintId === s.id)
      return bd?.runsCompleted ?? 0
    })
  }))
})

const trendOptions = computed(() => ({
  chart: {
    type: 'line',
    background: 'transparent',
    toolbar: { show: false },
    animations: { enabled: false }
  },
  stroke: { curve: 'smooth', width: 2 },
  xaxis: {
    categories: multi.value?.sprints.map(s => s.name) ?? [],
    labels: { style: { colors: '#9ca3af', fontSize: '12px' } }
  },
  yaxis: {
    labels: { style: { colors: '#9ca3af', fontSize: '12px' } },
    title: { text: 'Runs Completed', style: { color: '#9ca3af' } }
  },
  tooltip: { theme: 'dark' },
  legend: { labels: { colors: '#d1d5db' } },
  grid: { borderColor: '#374151' },
  theme: { mode: 'dark' }
}))

const hasDevelopers = computed(() => {
  if (isMulti.value) return (multi.value?.developers.length ?? 0) > 0
  return (single.value?.developers.length ?? 0) > 0
})
</script>

<template>
  <div class="flex flex-col gap-6">
    <!-- Empty: QA data exists but no developers -->
    <template v-if="!hasDevelopers">
      <BaseCard>
        <div class="text-sm text-text-muted text-center py-4">No test execution data for the selected sprint(s).</div>
      </BaseCard>
    </template>

    <template v-else>
      <!-- Team Metric Cards -->

      <!-- Multi-sprint team metrics (plain values) -->
      <div v-if="isMulti && multi" class="grid grid-cols-3 gap-4">
        <!-- Total TEs -->
        <BaseCard>
          <div
            class="text-xs text-text-muted mb-1 cursor-help"
            title="Number of test executions linked to sprint tickets. Counts unique TEs, not individual test runs."
          >
            Total TEs
          </div>
          <div class="text-2xl font-semibold text-text-primary tabular-nums">{{ multi.teamMetrics.totalTes }}</div>
        </BaseCard>

        <!-- Total Runs Completed -->
        <BaseCard>
          <div
            class="text-xs text-text-muted mb-1 cursor-help"
            title="Total individual test runs with a final result (pass or fail) across all testers this sprint."
          >
            Total Runs Completed
          </div>
          <div class="text-2xl font-semibold text-text-primary tabular-nums">{{ multi.teamMetrics.totalRunsCompleted }}</div>
        </BaseCard>

        <!-- Team Pass Rate -->
        <BaseCard>
          <div
            class="text-xs text-text-muted mb-1 cursor-help"
            title="Percentage of completed test runs that passed. Only counts runs with a final pass or fail result."
          >
            Team Pass Rate
          </div>
          <div :class="['text-2xl font-semibold tabular-nums', passRateRagClass(multi.teamMetrics.teamPassRate)]">
            {{ multi.teamMetrics.teamPassRate.toFixed(1) }}%
          </div>
        </BaseCard>
      </div>

      <!-- Single-sprint team MetricCards -->
      <div v-if="!isMulti && single" class="grid grid-cols-3 gap-4">
        <!-- Total TEs -->
        <BaseCard>
          <div
            class="text-xs text-text-muted mb-1 cursor-help"
            title="Number of test executions linked to sprint tickets. Counts unique TEs, not individual test runs."
          >
            {{ single.teamMetrics.totalTes.name }}
          </div>
          <div class="text-2xl font-semibold text-text-primary tabular-nums">{{ single.teamMetrics.totalTes.displayValue }}</div>
          <div v-if="single.teamMetrics.totalTes.delta !== null" class="text-xs text-text-secondary mt-1">
            {{ deltaIcon(single.teamMetrics.totalTes.deltaDirection) }}
            {{ Math.abs(single.teamMetrics.totalTes.delta) }}
          </div>
        </BaseCard>

        <!-- Total Runs Completed -->
        <BaseCard>
          <div
            class="text-xs text-text-muted mb-1 cursor-help"
            title="Total individual test runs with a final result (pass or fail) across all testers this sprint."
          >
            {{ single.teamMetrics.totalRunsCompleted.name }}
          </div>
          <div class="text-2xl font-semibold text-text-primary tabular-nums">{{ single.teamMetrics.totalRunsCompleted.displayValue }}</div>
          <div v-if="single.teamMetrics.totalRunsCompleted.delta !== null" class="text-xs text-text-secondary mt-1">
            {{ deltaIcon(single.teamMetrics.totalRunsCompleted.deltaDirection) }}
            {{ Math.abs(single.teamMetrics.totalRunsCompleted.delta) }}
          </div>
        </BaseCard>

        <!-- Team Pass Rate (RAG) -->
        <BaseCard>
          <div
            class="text-xs text-text-muted mb-1 cursor-help"
            title="Percentage of completed test runs that passed. Only counts runs with a final pass or fail result."
          >
            {{ single.teamMetrics.teamPassRate.name }}
          </div>
          <div :class="['text-2xl font-semibold tabular-nums', single.teamMetrics.teamPassRate.rag === 'green' ? 'text-status-success' : single.teamMetrics.teamPassRate.rag === 'amber' ? 'text-status-warning' : 'text-status-danger']">
            {{ single.teamMetrics.teamPassRate.displayValue }}
          </div>
          <div v-if="single.teamMetrics.teamPassRate.delta !== null" class="text-xs text-text-secondary mt-1">
            {{ deltaIcon(single.teamMetrics.teamPassRate.deltaDirection) }}
            {{ Math.abs(single.teamMetrics.teamPassRate.delta).toFixed(1) }}%
          </div>
        </BaseCard>
      </div>

      <!-- Workload Distribution Chart -->
      <BaseCard>
        <div
          class="text-sm font-medium text-text-primary mb-4 cursor-help"
          title="Shows how test execution volume is split across team members, broken down by pass and fail results."
        >
          Workload Distribution
        </div>
        <apexchart
          type="bar"
          height="300"
          :options="distributionOptions"
          :series="distributionSeries"
        />
      </BaseCard>

      <!-- Execution Throughput Trend Chart (multi only) -->
      <BaseCard v-if="isMulti && multi && multi.sprints.length > 0">
        <div
          class="text-sm font-medium text-text-primary mb-4 cursor-help"
          title="Each person's completed test runs per sprint over time. Helps spot changes in testing capacity."
        >
          Execution Throughput Trend
        </div>
        <apexchart
          type="line"
          height="300"
          :options="trendOptions"
          :series="trendSeries"
        />
      </BaseCard>

      <!-- Per-Person Table: Multi-sprint -->
      <BaseCard v-if="isMulti && multi">
        <div class="overflow-x-auto">
          <table class="w-full text-sm">
            <thead>
              <tr class="text-text-muted text-left border-b border-border-default">
                <th class="pb-2 pr-4 font-medium">Person</th>
                <th class="pb-2 pr-4 font-medium">Sub-Team</th>
                <th
                  class="pb-2 pr-4 font-medium text-right cursor-pointer select-none hover:text-text-primary"
                  :class="{ 'text-text-primary': sortColumn === 'tesOwned' }"
                  :title="'Test executions assigned to this person. Counts ownership regardless of who ran the individual tests.'"
                  @click="toggleSort('tesOwned')"
                >TEs Owned{{ sortIcon('tesOwned') }}</th>
                <th
                  class="pb-2 pr-4 font-medium text-right cursor-pointer select-none hover:text-text-primary"
                  :class="{ 'text-text-primary': sortColumn === 'runsCompleted' }"
                  :title="'Test runs this person executed with a final pass or fail result. Attributed by who ran the test.'"
                  @click="toggleSort('runsCompleted')"
                >Runs Completed{{ sortIcon('runsCompleted') }}</th>
                <th
                  class="pb-2 pr-4 font-medium text-right cursor-pointer select-none hover:text-text-primary"
                  :class="{ 'text-text-primary': sortColumn === 'passCount' }"
                  :title="'Number of test runs this person executed that passed.'"
                  @click="toggleSort('passCount')"
                >Pass{{ sortIcon('passCount') }}</th>
                <th
                  class="pb-2 pr-4 font-medium text-right cursor-pointer select-none hover:text-text-primary"
                  :class="{ 'text-text-primary': sortColumn === 'failCount' }"
                  :title="'Number of test runs this person executed that failed.'"
                  @click="toggleSort('failCount')"
                >Fail{{ sortIcon('failCount') }}</th>
                <th
                  class="pb-2 pr-4 font-medium text-right cursor-pointer select-none hover:text-text-primary"
                  :class="{ 'text-text-primary': sortColumn === 'passRate' }"
                  :title="'Percentage of this person\'s completed runs that passed. Color indicates quality against thresholds.'"
                  @click="toggleSort('passRate')"
                >Pass Rate{{ sortIcon('passRate') }}</th>
                <th
                  class="pb-2 pr-4 font-medium text-right cursor-pointer select-none hover:text-text-primary"
                  :class="{ 'text-text-primary': sortColumn === 'storiesCovered' }"
                  :title="'Unique sprint tickets linked to test executions where this person ran at least one test.'"
                  @click="toggleSort('storiesCovered')"
                >Stories Covered{{ sortIcon('storiesCovered') }}</th>
                <th
                  class="pb-2 font-medium text-right cursor-pointer select-none hover:text-text-primary"
                  :class="{ 'text-text-primary': sortColumn === 'bugsFound' }"
                  :title="'Bugs discovered through test executions this person owns. Credit goes to the TE owner.'"
                  @click="toggleSort('bugsFound')"
                >Bugs Found{{ sortIcon('bugsFound') }}</th>
              </tr>
            </thead>
            <tbody>
              <tr
                v-for="dev in sortedMultiDevs"
                :key="dev.accountId ?? '__unassigned__'"
                class="border-b border-border-default last:border-0"
              >
                <td class="py-2 pr-4">
                  <div class="flex items-center gap-2">
                    <template v-if="dev.accountId !== null">
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
                    </template>
                    <template v-else>
                      <div class="w-6 h-6 rounded-full bg-surface-elevated shrink-0" />
                      <span class="text-text-muted italic">{{ dev.displayName }}</span>
                    </template>
                    <!-- Workload alert badge -->
                    <span
                      v-if="dev.workloadAlert.isActive"
                      class="text-status-warning cursor-help"
                      :title="`Handles >50% of test executions for ${dev.workloadAlert.consecutiveSprintCount} consecutive sprints.`"
                    >
                      <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 20 20" fill="currentColor" class="w-4 h-4">
                        <path fill-rule="evenodd" d="M8.485 2.495c.673-1.167 2.357-1.167 3.03 0l6.28 10.875c.673 1.167-.17 2.625-1.516 2.625H3.72c-1.347 0-2.189-1.458-1.515-2.625L8.485 2.495zM10 5a.75.75 0 01.75.75v3.5a.75.75 0 01-1.5 0v-3.5A.75.75 0 0110 5zm0 9a1 1 0 100-2 1 1 0 000 2z" clip-rule="evenodd" />
                      </svg>
                    </span>
                  </div>
                </td>
                <td class="py-2 pr-4">
                  <span v-if="dev.subTeam" class="text-xs bg-surface-elevated rounded px-2 py-0.5 text-text-secondary">{{ dev.subTeam }}</span>
                  <span v-else class="text-text-muted">—</span>
                </td>
                <td class="py-2 pr-4 text-right tabular-nums text-text-primary">{{ dev.tesOwned }}</td>
                <td class="py-2 pr-4 text-right tabular-nums text-text-primary">{{ dev.runsCompleted }}</td>
                <td class="py-2 pr-4 text-right tabular-nums text-text-primary">{{ dev.passCount }}</td>
                <td class="py-2 pr-4 text-right tabular-nums text-text-primary">{{ dev.failCount }}</td>
                <td class="py-2 pr-4 text-right tabular-nums" :class="passRateRagClass(dev.passRate)">{{ dev.passRate.toFixed(1) }}%</td>
                <td class="py-2 pr-4 text-right tabular-nums text-text-primary">{{ dev.storiesCovered }}</td>
                <td class="py-2 text-right tabular-nums text-text-primary">{{ dev.bugsFound }}</td>
              </tr>
            </tbody>
          </table>
        </div>
      </BaseCard>

      <!-- Per-Person Table: Single-sprint -->
      <BaseCard v-if="!isMulti && single">
        <div class="overflow-x-auto">
          <table class="w-full text-sm">
            <thead>
              <tr class="text-text-muted text-left border-b border-border-default">
                <th class="pb-2 pr-4 font-medium">Person</th>
                <th class="pb-2 pr-4 font-medium">Sub-Team</th>
                <th
                  class="pb-2 pr-4 font-medium text-right cursor-pointer select-none hover:text-text-primary"
                  :class="{ 'text-text-primary': sortColumn === 'tesOwned' }"
                  :title="'Test executions assigned to this person. Counts ownership regardless of who ran the individual tests.'"
                  @click="toggleSort('tesOwned')"
                >TEs Owned{{ sortIcon('tesOwned') }}</th>
                <th
                  class="pb-2 pr-4 font-medium text-right cursor-pointer select-none hover:text-text-primary"
                  :class="{ 'text-text-primary': sortColumn === 'runsCompleted' }"
                  :title="'Test runs this person executed with a final pass or fail result. Attributed by who ran the test.'"
                  @click="toggleSort('runsCompleted')"
                >Runs Completed{{ sortIcon('runsCompleted') }}</th>
                <th
                  class="pb-2 pr-4 font-medium text-right cursor-pointer select-none hover:text-text-primary"
                  :class="{ 'text-text-primary': sortColumn === 'passCount' }"
                  :title="'Number of test runs this person executed that passed.'"
                  @click="toggleSort('passCount')"
                >Pass{{ sortIcon('passCount') }}</th>
                <th
                  class="pb-2 pr-4 font-medium text-right cursor-pointer select-none hover:text-text-primary"
                  :class="{ 'text-text-primary': sortColumn === 'failCount' }"
                  :title="'Number of test runs this person executed that failed.'"
                  @click="toggleSort('failCount')"
                >Fail{{ sortIcon('failCount') }}</th>
                <th
                  class="pb-2 pr-4 font-medium text-right cursor-pointer select-none hover:text-text-primary"
                  :class="{ 'text-text-primary': sortColumn === 'passRate' }"
                  :title="'Percentage of this person\'s completed runs that passed. Color indicates quality against thresholds.'"
                  @click="toggleSort('passRate')"
                >Pass Rate{{ sortIcon('passRate') }}</th>
                <th
                  class="pb-2 pr-4 font-medium text-right cursor-pointer select-none hover:text-text-primary"
                  :class="{ 'text-text-primary': sortColumn === 'storiesCovered' }"
                  :title="'Unique sprint tickets linked to test executions where this person ran at least one test.'"
                  @click="toggleSort('storiesCovered')"
                >Stories Covered{{ sortIcon('storiesCovered') }}</th>
                <th
                  class="pb-2 font-medium text-right cursor-pointer select-none hover:text-text-primary"
                  :class="{ 'text-text-primary': sortColumn === 'bugsFound' }"
                  :title="'Bugs discovered through test executions this person owns. Credit goes to the TE owner.'"
                  @click="toggleSort('bugsFound')"
                >Bugs Found{{ sortIcon('bugsFound') }}</th>
              </tr>
            </thead>
            <tbody>
              <tr
                v-for="dev in sortedSingleDevs"
                :key="dev.accountId ?? '__unassigned__'"
                class="border-b border-border-default last:border-0"
              >
                <td class="py-2 pr-4">
                  <div class="flex items-center gap-2">
                    <template v-if="dev.accountId !== null">
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
                    </template>
                    <template v-else>
                      <div class="w-6 h-6 rounded-full bg-surface-elevated shrink-0" />
                      <span class="text-text-muted italic">{{ dev.displayName }}</span>
                    </template>
                    <!-- Workload alert badge -->
                    <span
                      v-if="dev.workloadAlert.isActive"
                      class="text-status-warning cursor-help"
                      :title="`Handles >50% of test executions for ${dev.workloadAlert.consecutiveSprintCount} consecutive sprints.`"
                    >
                      <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 20 20" fill="currentColor" class="w-4 h-4">
                        <path fill-rule="evenodd" d="M8.485 2.495c.673-1.167 2.357-1.167 3.03 0l6.28 10.875c.673 1.167-.17 2.625-1.516 2.625H3.72c-1.347 0-2.189-1.458-1.515-2.625L8.485 2.495zM10 5a.75.75 0 01.75.75v3.5a.75.75 0 01-1.5 0v-3.5A.75.75 0 0110 5zm0 9a1 1 0 100-2 1 1 0 000 2z" clip-rule="evenodd" />
                      </svg>
                    </span>
                  </div>
                </td>
                <td class="py-2 pr-4">
                  <span v-if="dev.subTeam" class="text-xs bg-surface-elevated rounded px-2 py-0.5 text-text-secondary">{{ dev.subTeam }}</span>
                  <span v-else class="text-text-muted">—</span>
                </td>
                <!-- TEs Owned -->
                <td class="py-2 pr-4 text-right tabular-nums text-text-primary">
                  <span>{{ dev.tesOwned }}</span>
                  <span v-if="dev.tesOwnedDelta !== null" :class="['ml-1 text-xs', deltaClass(dev.tesOwnedPolarity, dev.tesOwnedDirection)]">
                    {{ deltaIcon(dev.tesOwnedDirection) }} {{ Math.abs(dev.tesOwnedDelta) }}
                  </span>
                </td>
                <!-- Runs Completed -->
                <td class="py-2 pr-4 text-right tabular-nums text-text-primary">
                  <span>{{ dev.runsCompleted }}</span>
                  <span v-if="dev.runsCompletedDelta !== null" :class="['ml-1 text-xs', deltaClass(dev.runsCompletedPolarity, dev.runsCompletedDirection)]">
                    {{ deltaIcon(dev.runsCompletedDirection) }} {{ Math.abs(dev.runsCompletedDelta) }}
                  </span>
                </td>
                <!-- Pass -->
                <td class="py-2 pr-4 text-right tabular-nums text-text-primary">
                  <span>{{ dev.passCount }}</span>
                  <span v-if="dev.passCountDelta !== null" :class="['ml-1 text-xs', deltaClass(dev.passCountPolarity, dev.passCountDirection)]">
                    {{ deltaIcon(dev.passCountDirection) }} {{ Math.abs(dev.passCountDelta) }}
                  </span>
                </td>
                <!-- Fail -->
                <td class="py-2 pr-4 text-right tabular-nums text-text-primary">
                  <span>{{ dev.failCount }}</span>
                  <span v-if="dev.failCountDelta !== null" :class="['ml-1 text-xs', deltaClass(dev.failCountPolarity, dev.failCountDirection)]">
                    {{ deltaIcon(dev.failCountDirection) }} {{ Math.abs(dev.failCountDelta) }}
                  </span>
                </td>
                <!-- Pass Rate -->
                <td class="py-2 pr-4 text-right tabular-nums" :class="passRateRagClass(dev.passRate)">
                  <span>{{ dev.passRate.toFixed(1) }}%</span>
                  <span v-if="dev.passRateDelta !== null" :class="['ml-1 text-xs', deltaClass(dev.passRatePolarity, dev.passRateDirection)]">
                    {{ deltaIcon(dev.passRateDirection) }} {{ Math.abs(dev.passRateDelta).toFixed(1) }}
                  </span>
                </td>
                <!-- Stories Covered -->
                <td class="py-2 pr-4 text-right tabular-nums text-text-primary">
                  <span>{{ dev.storiesCovered }}</span>
                  <span v-if="dev.storiesCoveredDelta !== null" :class="['ml-1 text-xs', deltaClass(dev.storiesCoveredPolarity, dev.storiesCoveredDirection)]">
                    {{ deltaIcon(dev.storiesCoveredDirection) }} {{ Math.abs(dev.storiesCoveredDelta) }}
                  </span>
                </td>
                <!-- Bugs Found -->
                <td class="py-2 text-right tabular-nums text-text-primary">
                  <span>{{ dev.bugsFound }}</span>
                  <span v-if="dev.bugsFoundDelta !== null" :class="['ml-1 text-xs', deltaClass(dev.bugsFoundPolarity, dev.bugsFoundDirection)]">
                    {{ deltaIcon(dev.bugsFoundDirection) }} {{ Math.abs(dev.bugsFoundDelta) }}
                  </span>
                </td>
              </tr>
            </tbody>
          </table>
        </div>
      </BaseCard>
    </template>
  </div>
</template>
