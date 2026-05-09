<script setup lang="ts">
import BaseCard from '../BaseCard.vue'
import type { ScopeChangeSummaryMetrics, ScopeChangeSingleSprintMetrics, ScopeMetricCard } from '../../types'

const props = defineProps<{
  mode: 'multi' | 'single'
  summaryMetrics?: ScopeChangeSummaryMetrics | null
  singleMetrics?: ScopeChangeSingleSprintMetrics | null
}>()

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

function singleCards(metrics: ScopeChangeSingleSprintMetrics): ScopeMetricCard[] {
  return [
    metrics.committedSpActive,
    metrics.committedSpTotal,
    metrics.addedSp,
    metrics.removedSp,
    metrics.netScopeChange,
    metrics.disruptionRate,
    metrics.bugCount
  ]
}
</script>

<template>
  <!-- Multi-sprint summary cards -->
  <div v-if="mode === 'multi' && summaryMetrics" class="grid grid-cols-3 gap-4">
    <BaseCard>
      <div class="text-xs text-text-muted mb-1">Avg Disruption Rate</div>
      <div class="text-2xl font-semibold text-text-primary tabular-nums">
        {{ summaryMetrics.averageDisruptionRate.toFixed(1) }}%
      </div>
    </BaseCard>
    <BaseCard>
      <div class="text-xs text-text-muted mb-1">Avg Net Scope Change</div>
      <div class="text-2xl font-semibold text-text-primary tabular-nums">
        {{ summaryMetrics.averageNetScopeChange >= 0 ? '+' : '' }}{{ summaryMetrics.averageNetScopeChange.toFixed(1) }} SP
      </div>
    </BaseCard>
    <BaseCard>
      <div class="text-xs text-text-muted mb-1">Total Bugs Added</div>
      <div class="text-2xl font-semibold text-text-primary tabular-nums">
        {{ summaryMetrics.totalBugsAdded }}
      </div>
    </BaseCard>
  </div>

  <!-- Single-sprint metric cards -->
  <div v-else-if="mode === 'single' && singleMetrics" class="grid grid-cols-4 gap-4">
    <BaseCard v-for="card in singleCards(singleMetrics)" :key="card.name">
      <div class="text-xs text-text-muted mb-1">{{ card.name }}</div>
      <div class="text-xl font-semibold text-text-primary tabular-nums">{{ card.displayValue }}</div>
      <div v-if="card.delta !== null" :class="['mt-1 text-xs', deltaClass(card.deltaPolarity, card.deltaDirection)]">
        {{ deltaIcon(card.deltaDirection) }} {{ Math.abs(card.delta).toFixed(1) }}
      </div>
    </BaseCard>
  </div>
</template>
