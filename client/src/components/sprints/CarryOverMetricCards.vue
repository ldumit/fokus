<script setup lang="ts">
import BaseCard from '../BaseCard.vue'
import type { CarryOverSummaryMetrics, CarryOverSingleSprintMetrics, ScopeMetricCard } from '../../types'

const props = defineProps<{
  mode: 'multi' | 'single'
  summaryMetrics?: CarryOverSummaryMetrics | null
  singleMetrics?: CarryOverSingleSprintMetrics | null
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

function singleCards(metrics: CarryOverSingleSprintMetrics): ScopeMetricCard[] {
  return [
    metrics.carryOverRate,
    metrics.carryOverSp,
    metrics.carryOverTicketCount
  ]
}
</script>

<template>
  <!-- Multi-sprint summary cards -->
  <div v-if="mode === 'multi' && summaryMetrics" class="grid grid-cols-3 gap-4">
    <BaseCard>
      <div class="text-xs text-text-muted mb-1">Avg Carry-Over Rate</div>
      <div class="text-2xl font-semibold text-text-primary tabular-nums">
        {{ summaryMetrics.averageCarryOverRate.toFixed(1) }}%
      </div>
    </BaseCard>
    <BaseCard>
      <div class="text-xs text-text-muted mb-1">Avg Carry-Over SP</div>
      <div class="text-2xl font-semibold text-text-primary tabular-nums">
        {{ summaryMetrics.averageCarryOverSp.toFixed(1) }}
      </div>
    </BaseCard>
    <BaseCard>
      <div class="text-xs text-text-muted mb-1">Total Zombie Tickets</div>
      <div class="text-2xl font-semibold text-text-primary tabular-nums">
        {{ summaryMetrics.totalZombieTickets }}
      </div>
    </BaseCard>
  </div>

  <!-- Single-sprint metric cards with deltas -->
  <div v-else-if="mode === 'single' && singleMetrics" class="grid grid-cols-3 gap-4">
    <BaseCard v-for="card in singleCards(singleMetrics)" :key="card.name">
      <div class="text-xs text-text-muted mb-1">{{ card.name }}</div>
      <div class="text-xl font-semibold text-text-primary tabular-nums">{{ card.displayValue }}</div>
      <div v-if="card.delta !== null" :class="['mt-1 text-xs', deltaClass(card.deltaPolarity, card.deltaDirection)]">
        {{ deltaIcon(card.deltaDirection) }} {{ Math.abs(card.delta).toFixed(1) }}
      </div>
    </BaseCard>
  </div>
</template>
