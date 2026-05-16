<script setup lang="ts">
import type { EpicProgressEntry } from '../../types'
import BaseCard from '../BaseCard.vue'
import EpicTicketTable from './EpicTicketTable.vue'

const props = defineProps<{
  epics: EpicProgressEntry[]
  expandedEpicKeys: Set<string>
  hasQaData: boolean
}>()

const emit = defineEmits<{
  'toggle-expand': [epicKey: string]
}>()

function spDisplay(entry: EpicProgressEntry): string {
  if (entry.adjustedTotalSp === null) return '—'
  const base = `${entry.doneSp.toFixed(1)} / ${entry.adjustedTotalSp.toFixed(1)} SP`
  if (entry.imputedSp !== null && entry.imputedSp > 0) {
    return `${base} (+~${entry.imputedSp.toFixed(1)} est.)`
  }
  return base
}

function velocityDisplay(entry: EpicProgressEntry): string {
  if (entry.velocity === null) return '—'
  return `${entry.velocity.toFixed(1)} SP/sprint`
}

function projectionDisplay(entry: EpicProgressEntry): string {
  if (entry.projectedSprintsRemaining === null) return 'Insufficient data'
  return `${entry.projectedSprintsRemaining.toFixed(1)} sprints`
}

function completionBarWidth(entry: EpicProgressEntry): string {
  if (entry.spCompletionPercentage !== null) {
    return `${Math.min(entry.spCompletionPercentage, 100)}%`
  }
  return `${Math.min(entry.ticketCompletionPercentage, 100)}%`
}

function imputedBarWidth(entry: EpicProgressEntry): string {
  if (entry.adjustedTotalSp === null || entry.adjustedTotalSp === 0 || entry.imputedSp === null || entry.imputedSp === 0) {
    return '0%'
  }
  const donePct = entry.spCompletionPercentage ?? 0
  const imputedPct = (entry.imputedSp / entry.adjustedTotalSp) * 100
  // show imputed segment only for remaining portion
  const remainingImputedPct = Math.max(0, Math.min(imputedPct, 100 - donePct))
  return `${remainingImputedPct.toFixed(1)}%`
}

function completionLabel(entry: EpicProgressEntry): string {
  if (entry.spCompletionPercentage !== null) {
    return `${entry.spCompletionPercentage.toFixed(1)}%`
  }
  return `${entry.ticketCompletionPercentage.toFixed(1)}%`
}

function ragClass(rag: string | null | undefined): string {
  if (rag === 'green') return 'text-status-success'
  if (rag === 'amber') return 'text-status-warning'
  if (rag === 'red') return 'text-status-danger'
  return 'text-text-primary'
}
</script>

<template>
  <div v-if="epics.length === 0" class="text-center py-12 text-text-muted text-sm">
    <slot name="empty" />
  </div>

  <BaseCard v-else class="overflow-hidden">
    <div class="overflow-x-auto">
      <table class="w-full text-sm">
        <thead>
          <tr class="text-text-muted text-left border-b border-border-default">
            <th class="pb-3 pr-4 font-medium w-6"></th>
            <th class="pb-3 pr-4 font-medium">Epic</th>
            <th class="pb-3 pr-4 font-medium min-w-40" title="SP completion with estimated portion shown separately.">Progress</th>
            <th class="pb-3 pr-4 font-medium text-right whitespace-nowrap" title="Completed story points versus total scope including estimates.">SP Done / Total</th>
            <th class="pb-3 pr-4 font-medium text-right whitespace-nowrap" title="Completed tickets versus total ticket count in the epic.">Tickets</th>
            <th class="pb-3 pr-4 font-medium text-right whitespace-nowrap" title="Average SP completed per sprint (last 3 sprints with progress).">Velocity</th>
            <th class="pb-3 pr-4 font-medium text-right whitespace-nowrap" title="Estimated sprints to completion based on current velocity.">Projected</th>
            <th class="pb-3 font-medium text-right whitespace-nowrap" title="Number of sprints this epic has had tickets in." :class="{ 'pr-4': hasQaData }">Sprints</th>
            <th v-if="hasQaData" class="pb-3 pr-4 font-medium text-right whitespace-nowrap" title="Percentage of feature tickets with at least one linked test execution.">Coverage %</th>
            <th v-if="hasQaData" class="pb-3 pr-4 font-medium text-right whitespace-nowrap" title="Percentage of PASS test runs across all non-cancelled test executions linked to feature tickets.">Pass Rate %</th>
            <th v-if="hasQaData" class="pb-3 font-medium text-right whitespace-nowrap" title="Unique bug tickets linked via Blocks from test executions on this epic's feature tickets.">Bugs Found</th>
          </tr>
        </thead>
        <tbody>
          <template v-for="epic in epics" :key="epic.epicKey">
            <!-- Epic row -->
            <tr
              class="border-b border-border-default hover:bg-surface-elevated/40 cursor-pointer transition-colors"
              :class="{ 'last:border-0': !expandedEpicKeys.has(epic.epicKey) }"
              @click="emit('toggle-expand', epic.epicKey)"
            >
              <!-- Expand chevron -->
              <td class="py-3 pr-2">
                <svg
                  xmlns="http://www.w3.org/2000/svg"
                  viewBox="0 0 20 20"
                  fill="currentColor"
                  class="w-4 h-4 text-text-muted transition-transform"
                  :class="{ 'rotate-90': expandedEpicKeys.has(epic.epicKey) }"
                >
                  <path fill-rule="evenodd" d="M7.293 14.707a1 1 0 010-1.414L10.586 10 7.293 6.707a1 1 0 011.414-1.414l4 4a1 1 0 010 1.414l-4 4a1 1 0 01-1.414 0z" clip-rule="evenodd" />
                </svg>
              </td>

              <!-- Epic name -->
              <td class="py-3 pr-4 text-text-primary font-medium">
                <div class="flex flex-col gap-0.5">
                  <span>{{ epic.epicName }}</span>
                  <span class="text-xs text-text-muted font-normal font-mono">{{ epic.epicKey }}</span>
                </div>
              </td>

              <!-- Progress bar (dual: SP completion + coverage) -->
              <td class="py-3 pr-4">
                <div class="flex items-center gap-2">
                  <div class="flex-1 flex flex-col gap-1">
                    <!-- SP completion bar (BR19: unchanged) -->
                    <div class="h-2 rounded-full bg-surface-elevated overflow-hidden flex">
                      <!-- Actual completion segment -->
                      <div
                        class="h-full bg-accent-default rounded-l-full transition-all"
                        :style="{ width: completionBarWidth(epic) }"
                      />
                      <!-- Imputed SP segment (distinct hatched look via opacity) -->
                      <div
                        v-if="epic.imputedSp !== null && epic.imputedSp > 0"
                        class="h-full bg-accent-default/30 transition-all"
                        :style="{ width: imputedBarWidth(epic) }"
                      />
                    </div>
                    <!-- Coverage bar (BR18: thinner, distinct color, hidden when null) -->
                    <div
                      v-if="hasQaData && epic.coverageRate !== null"
                      class="h-1 rounded-full bg-surface-elevated overflow-hidden"
                    >
                      <div
                        class="h-full bg-emerald-500 rounded-full transition-all"
                        :style="{ width: `${Math.min(epic.coverageRate, 100)}%` }"
                      />
                    </div>
                  </div>
                  <span class="text-xs text-text-secondary tabular-nums whitespace-nowrap w-12 text-right">
                    {{ completionLabel(epic) }}
                  </span>
                </div>
              </td>

              <!-- SP done / total -->
              <td class="py-3 pr-4 text-right tabular-nums text-text-primary whitespace-nowrap">
                {{ spDisplay(epic) }}
              </td>

              <!-- Tickets -->
              <td class="py-3 pr-4 text-right tabular-nums text-text-primary whitespace-nowrap">
                {{ epic.doneTickets }} / {{ epic.totalTickets }}
              </td>

              <!-- Velocity -->
              <td class="py-3 pr-4 text-right tabular-nums text-text-secondary whitespace-nowrap">
                {{ velocityDisplay(epic) }}
              </td>

              <!-- Projected remaining -->
              <td class="py-3 pr-4 text-right tabular-nums whitespace-nowrap">
                <span :class="epic.projectedSprintsRemaining === null ? 'text-text-muted' : 'text-text-secondary'">
                  {{ projectionDisplay(epic) }}
                </span>
                <span
                  v-if="epic.projectionConfidence === 'low'"
                  class="ml-1 text-xs text-amber-400 bg-amber-400/10 px-1.5 py-0.5 rounded"
                >
                  Low confidence
                </span>
              </td>

              <!-- Active sprint count -->
              <td class="py-3 text-right tabular-nums text-text-secondary" :class="{ 'pr-4': hasQaData }">
                {{ epic.activeSprintCount }}
              </td>

              <!-- Coverage % (QA) -->
              <td v-if="hasQaData" class="py-3 pr-4 text-right tabular-nums whitespace-nowrap">
                <span :class="epic.coverageRate !== null ? ragClass(epic.coverageRag) : 'text-text-muted'">
                  {{ epic.coverageRate !== null ? `${epic.coverageRate.toFixed(1)}%` : '—' }}
                </span>
              </td>

              <!-- Pass Rate % (QA) -->
              <td v-if="hasQaData" class="py-3 pr-4 text-right tabular-nums whitespace-nowrap">
                <span :class="epic.passRate !== null ? ragClass(epic.passRateRag) : 'text-text-muted'">
                  {{ epic.passRate !== null ? `${epic.passRate.toFixed(1)}%` : '—' }}
                </span>
              </td>

              <!-- Bugs Found (QA) -->
              <td v-if="hasQaData" class="py-3 text-right tabular-nums text-text-primary">
                {{ epic.featureTicketCount > 0 ? epic.bugsFound : '—' }}
              </td>
            </tr>

            <!-- Expanded ticket table -->
            <tr v-if="expandedEpicKeys.has(epic.epicKey)" :key="`${epic.epicKey}-tickets`">
              <td :colspan="hasQaData ? 11 : 8" class="px-4 pb-4 bg-surface-elevated/20 border-b border-border-default">
                <div class="pt-3">
                  <EpicTicketTable :tickets="epic.tickets" :has-qa-data="hasQaData" />
                </div>
              </td>
            </tr>
          </template>
        </tbody>
      </table>
    </div>
  </BaseCard>

  <!-- Empty states within the table area (rendered via slot in parent, but handled here when no epics) -->
</template>
