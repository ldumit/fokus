<script setup lang="ts">
import { ref, computed } from 'vue'
import type { DeveloperQualityEntry, DeveloperQualitySprintInfo, DeveloperQualitySprintBreakdown } from '../../types'
import BaseCard from '../BaseCard.vue'

const props = defineProps<{
  developers: DeveloperQualityEntry[]
  sprints: DeveloperQualitySprintInfo[]
  isSingleSprint: boolean
}>()

// --- Sorting ---
const sortColumn = ref<string>('coveragePercent')
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

// --- Multi-sprint averaging (BR22): exclude sprints where stories === 0 ---
function avgCoverage(dev: DeveloperQualityEntry): number {
  const vals = dev.sprintBreakdowns.filter(b => b.stories > 0).map(b => b.coveragePercent)
  if (vals.length === 0) return 0
  return vals.reduce((a, b) => a + b, 0) / vals.length
}

function avgPassRate(dev: DeveloperQualityEntry): number {
  const vals = dev.sprintBreakdowns.filter(b => b.stories > 0).map(b => b.passRatePercent)
  if (vals.length === 0) return 0
  return vals.reduce((a, b) => a + b, 0) / vals.length
}

function avgStories(dev: DeveloperQualityEntry): number {
  const vals = dev.sprintBreakdowns.map(b => b.stories)
  if (vals.length === 0) return 0
  return vals.reduce((a, b) => a + b, 0) / vals.length
}

function avgCovered(dev: DeveloperQualityEntry): number {
  const vals = dev.sprintBreakdowns.map(b => b.covered)
  if (vals.length === 0) return 0
  return vals.reduce((a, b) => a + b, 0) / vals.length
}

function avgUntested(dev: DeveloperQualityEntry): number {
  const vals = dev.sprintBreakdowns.map(b => b.untested)
  if (vals.length === 0) return 0
  return vals.reduce((a, b) => a + b, 0) / vals.length
}

function avgBugsFound(dev: DeveloperQualityEntry): number {
  const vals = dev.sprintBreakdowns.map(b => b.bugsFound)
  if (vals.length === 0) return 0
  return vals.reduce((a, b) => a + b, 0) / vals.length
}

// --- Single-sprint: get the single breakdown ---
function singleBreakdown(dev: DeveloperQualityEntry): DeveloperQualitySprintBreakdown | undefined {
  if (props.sprints.length === 0) return undefined
  return dev.sprintBreakdowns.find(b => b.sprintId === props.sprints[0].id)
}

// --- Sort value ---
function sortValue(dev: DeveloperQualityEntry): number {
  if (props.isSingleSprint) {
    const bd = singleBreakdown(dev)
    if (!bd) return 0
    if (sortColumn.value === 'coveragePercent') return bd.coveragePercent
    if (sortColumn.value === 'passRatePercent') return bd.passRatePercent
    if (sortColumn.value === 'stories') return bd.stories
    if (sortColumn.value === 'covered') return bd.covered
    if (sortColumn.value === 'untested') return bd.untested
    if (sortColumn.value === 'bugsFound') return bd.bugsFound
  } else {
    if (sortColumn.value === 'coveragePercent') return avgCoverage(dev)
    if (sortColumn.value === 'passRatePercent') return avgPassRate(dev)
    if (sortColumn.value === 'stories') return avgStories(dev)
    if (sortColumn.value === 'covered') return avgCovered(dev)
    if (sortColumn.value === 'untested') return avgUntested(dev)
    if (sortColumn.value === 'bugsFound') return avgBugsFound(dev)
  }
  return 0
}

const sortedDevelopers = computed(() => {
  const devs = [...props.developers]
  const dir = sortDirection.value
  return devs.sort((a, b) => {
    const diff = sortValue(a) - sortValue(b)
    return dir === 'asc' ? diff : -diff
  })
})

// --- RAG class ---
function ragClass(rag: string | null | undefined): string {
  if (rag === 'green') return 'text-status-success'
  if (rag === 'amber') return 'text-status-warning'
  if (rag === 'red') return 'text-status-danger'
  return 'text-text-primary'
}

// --- Delta display ---
function deltaIcon(direction: string | null | undefined): string {
  if (direction === 'up') return '▲'
  if (direction === 'down') return '▼'
  return '—'
}

function deltaClass(polarity: string | null | undefined, direction: string | null | undefined): string {
  if (direction === 'flat' || !direction) return 'text-text-secondary'
  if (polarity === 'positive') return 'text-status-success'
  if (polarity === 'negative') return 'text-status-danger'
  return 'text-text-secondary'
}

// --- Sparkline SVG (inline polyline from sparkline data) ---
function sparklinePath(points: { sprintName: string; value: number }[] | null | undefined): string {
  if (!points || points.length < 2) return ''
  const w = 48
  const h = 20
  const values = points.map(p => p.value)
  const min = Math.min(...values)
  const max = Math.max(...values)
  const range = max - min || 1
  const step = w / (points.length - 1)
  return points
    .map((p, i) => {
      const x = i * step
      const y = h - ((p.value - min) / range) * h
      return `${i === 0 ? 'M' : 'L'}${x.toFixed(1)},${y.toFixed(1)}`
    })
    .join(' ')
}
</script>

<template>
  <BaseCard>
    <div class="flex items-center gap-1 mb-4">
      <div class="text-sm font-medium text-text-primary">Developer Story Quality</div>
    </div>
    <div class="overflow-x-auto">
      <table class="w-full text-sm">
        <thead>
          <tr class="text-text-muted text-left border-b border-border-default">
            <th class="pb-2 pr-4 font-medium">Developer</th>
            <th class="pb-2 pr-4 font-medium">Sub-Team</th>
            <th
              class="pb-2 pr-4 font-medium text-right cursor-pointer select-none hover:text-text-primary"
              :class="{ 'text-text-primary': sortColumn === 'stories' }"
              title="Total feature stories assigned to this developer in the sprint scope."
              @click="toggleSort('stories')"
            >Stories{{ sortIcon('stories') }}</th>
            <th
              class="pb-2 pr-4 font-medium text-right cursor-pointer select-none hover:text-text-primary"
              :class="{ 'text-text-primary': sortColumn === 'covered' }"
              title="Stories with at least one linked test execution."
              @click="toggleSort('covered')"
            >Covered{{ sortIcon('covered') }}</th>
            <th
              class="pb-2 pr-4 font-medium text-right cursor-pointer select-none hover:text-text-primary"
              :class="{ 'text-text-primary': sortColumn === 'coveragePercent' }"
              title="Percentage of this developer's stories that have linked test executions."
              @click="toggleSort('coveragePercent')"
            >Coverage %{{ sortIcon('coveragePercent') }}</th>
            <th
              class="pb-2 pr-4 font-medium text-right cursor-pointer select-none hover:text-text-primary"
              :class="{ 'text-text-primary': sortColumn === 'passRatePercent' }"
              title="Percentage of test runs that passed across all test executions on this developer's stories."
              @click="toggleSort('passRatePercent')"
            >Pass Rate %{{ sortIcon('passRatePercent') }}</th>
            <th
              class="pb-2 pr-4 font-medium text-right cursor-pointer select-none hover:text-text-primary"
              :class="{ 'text-text-primary': sortColumn === 'untested' }"
              title="Stories with no linked test executions — the coverage gap for this developer."
              @click="toggleSort('untested')"
            >Untested{{ sortIcon('untested') }}</th>
            <th
              class="pb-2 font-medium text-right cursor-pointer select-none hover:text-text-primary"
              :class="{ 'text-text-primary': sortColumn === 'bugsFound' }"
              title="Bugs discovered during testing of this developer's stories. Delta is neutral — more is not inherently good or bad."
              @click="toggleSort('bugsFound')"
            >Bugs Found{{ sortIcon('bugsFound') }}</th>
          </tr>
        </thead>
        <tbody>
          <tr
            v-for="dev in sortedDevelopers"
            :key="dev.accountId"
            class="border-b border-border-default last:border-0"
          >
            <!-- Developer identity -->
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
                <!-- Below-median warning flag (single-sprint only) -->
                <span
                  v-if="isSingleSprint && dev.belowMedianStreak !== null && dev.belowMedianStreak !== undefined"
                  class="text-amber-400 text-xs cursor-help"
                  :title="`Coverage below team median for ${dev.belowMedianStreak} consecutive sprints. Review in retro.`"
                >&#9651;</span>
              </div>
            </td>

            <!-- Sub-team -->
            <td class="py-2 pr-4 text-text-secondary">
              <span v-if="dev.subTeam" class="text-xs bg-surface-elevated rounded px-2 py-0.5">{{ dev.subTeam }}</span>
              <span v-else class="text-text-muted">—</span>
            </td>

            <!-- Single-sprint mode -->
            <template v-if="isSingleSprint">
              <template v-if="singleBreakdown(dev)">
                <!-- Stories (no delta per spec) -->
                <td class="py-2 pr-4 text-right tabular-nums text-text-primary">
                  {{ singleBreakdown(dev)!.stories }}
                </td>

                <!-- Covered (no delta per spec) -->
                <td class="py-2 pr-4 text-right tabular-nums text-text-primary">
                  {{ singleBreakdown(dev)!.covered }}
                </td>

                <!-- Coverage % with RAG, delta, sparkline -->
                <td class="py-2 pr-4 text-right tabular-nums">
                  <div class="flex items-center justify-end gap-1">
                    <svg
                      v-if="singleBreakdown(dev)!.coverageSparkline && singleBreakdown(dev)!.coverageSparkline!.length >= 2"
                      width="48"
                      height="20"
                      class="shrink-0 opacity-60"
                    >
                      <path
                        :d="sparklinePath(singleBreakdown(dev)!.coverageSparkline)"
                        fill="none"
                        stroke="currentColor"
                        stroke-width="1.5"
                        class="text-accent-default"
                      />
                    </svg>
                    <span :class="ragClass(singleBreakdown(dev)!.coverageRag)">
                      {{ singleBreakdown(dev)!.coveragePercent.toFixed(1) }}%
                    </span>
                    <span
                      v-if="singleBreakdown(dev)!.coveragePercentDelta !== null"
                      :class="['text-xs', deltaClass(singleBreakdown(dev)!.coveragePercentDeltaPolarity, singleBreakdown(dev)!.coveragePercentDeltaDirection)]"
                    >
                      {{ deltaIcon(singleBreakdown(dev)!.coveragePercentDeltaDirection) }} {{ Math.abs(singleBreakdown(dev)!.coveragePercentDelta!).toFixed(1) }}
                    </span>
                  </div>
                </td>

                <!-- Pass Rate % with RAG, delta, sparkline -->
                <td class="py-2 pr-4 text-right tabular-nums">
                  <div class="flex items-center justify-end gap-1">
                    <svg
                      v-if="singleBreakdown(dev)!.passRateSparkline && singleBreakdown(dev)!.passRateSparkline!.length >= 2"
                      width="48"
                      height="20"
                      class="shrink-0 opacity-60"
                    >
                      <path
                        :d="sparklinePath(singleBreakdown(dev)!.passRateSparkline)"
                        fill="none"
                        stroke="currentColor"
                        stroke-width="1.5"
                        class="text-accent-default"
                      />
                    </svg>
                    <span :class="ragClass(singleBreakdown(dev)!.passRateRag)">
                      {{ singleBreakdown(dev)!.passRatePercent.toFixed(1) }}%
                    </span>
                    <span
                      v-if="singleBreakdown(dev)!.passRatePercentDelta !== null"
                      :class="['text-xs', deltaClass(singleBreakdown(dev)!.passRatePercentDeltaPolarity, singleBreakdown(dev)!.passRatePercentDeltaDirection)]"
                    >
                      {{ deltaIcon(singleBreakdown(dev)!.passRatePercentDeltaDirection) }} {{ Math.abs(singleBreakdown(dev)!.passRatePercentDelta!).toFixed(1) }}
                    </span>
                  </div>
                </td>

                <!-- Untested (no delta per spec) -->
                <td class="py-2 pr-4 text-right tabular-nums text-text-primary">
                  {{ singleBreakdown(dev)!.untested }}
                </td>

                <!-- Bugs Found with neutral delta -->
                <td class="py-2 text-right tabular-nums text-text-primary">
                  <span>{{ singleBreakdown(dev)!.bugsFound }}</span>
                  <span
                    v-if="singleBreakdown(dev)!.bugsFoundDelta !== null"
                    class="ml-1 text-xs text-text-secondary"
                  >
                    {{ deltaIcon(singleBreakdown(dev)!.bugsFoundDeltaDirection) }} {{ Math.abs(singleBreakdown(dev)!.bugsFoundDelta!) }}
                  </span>
                </td>
              </template>
              <template v-else>
                <td class="py-2 pr-4 text-right text-text-muted" colspan="6">—</td>
              </template>
            </template>

            <!-- Multi-sprint mode: averaged values, no deltas/sparklines/flags -->
            <template v-else>
              <td class="py-2 pr-4 text-right tabular-nums text-text-primary">{{ avgStories(dev).toFixed(1) }}</td>
              <td class="py-2 pr-4 text-right tabular-nums text-text-primary">{{ avgCovered(dev).toFixed(1) }}</td>
              <td class="py-2 pr-4 text-right tabular-nums text-text-primary">{{ avgCoverage(dev).toFixed(1) }}%</td>
              <td class="py-2 pr-4 text-right tabular-nums text-text-primary">{{ avgPassRate(dev).toFixed(1) }}%</td>
              <td class="py-2 pr-4 text-right tabular-nums text-text-primary">{{ avgUntested(dev).toFixed(1) }}</td>
              <td class="py-2 text-right tabular-nums text-text-primary">{{ avgBugsFound(dev).toFixed(1) }}</td>
            </template>
          </tr>
        </tbody>
      </table>
    </div>
  </BaseCard>
</template>
