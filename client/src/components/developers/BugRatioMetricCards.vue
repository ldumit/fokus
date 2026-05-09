<script setup lang="ts">
import type { BugRatioMultiSprintResponse, BugRatioSingleSprintResponse } from '../../types'
import BaseCard from '../BaseCard.vue'

defineProps<{
  mode: 'multi' | 'single'
  multi?: BugRatioMultiSprintResponse
  single?: BugRatioSingleSprintResponse
}>()

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
</script>

<template>
  <!-- Multi-sprint: show totals without deltas -->
  <div v-if="mode === 'multi' && multi" class="grid grid-cols-3 gap-4">
    <BaseCard>
      <div class="text-xs text-text-muted mb-1">Team Bug Ratio</div>
      <div class="text-2xl font-bold text-text-primary tabular-nums">{{ multi.teamMetrics.bugRatioPercent.toFixed(1) }}%</div>
      <div class="text-xs text-text-muted mt-1">of completed SP</div>
    </BaseCard>
    <BaseCard>
      <div class="text-xs text-text-muted mb-1">Total Bug SP</div>
      <div class="text-2xl font-bold text-status-danger tabular-nums">{{ multi.teamMetrics.totalBugSp.toFixed(1) }}</div>
    </BaseCard>
    <BaseCard>
      <div class="text-xs text-text-muted mb-1">Total Non-Bug SP</div>
      <div class="text-2xl font-bold text-status-success tabular-nums">{{ multi.teamMetrics.totalNonBugSp.toFixed(1) }}</div>
    </BaseCard>
  </div>

  <!-- Single-sprint: metric cards with deltas -->
  <div v-else-if="mode === 'single' && single" class="grid grid-cols-3 gap-4">
    <BaseCard>
      <div class="text-xs text-text-muted mb-1">{{ single.teamMetrics.bugRatioPercent.name }}</div>
      <div class="text-2xl font-bold text-text-primary tabular-nums">{{ single.teamMetrics.bugRatioPercent.displayValue }}</div>
      <div v-if="single.teamMetrics.bugRatioPercent.delta !== null" :class="['text-xs mt-1', deltaClass(single.teamMetrics.bugRatioPercent.deltaPolarity, single.teamMetrics.bugRatioPercent.deltaDirection)]">
        {{ deltaIcon(single.teamMetrics.bugRatioPercent.deltaDirection) }} {{ Math.abs(single.teamMetrics.bugRatioPercent.delta).toFixed(1) }}% vs prior
      </div>
    </BaseCard>
    <BaseCard>
      <div class="text-xs text-text-muted mb-1">{{ single.teamMetrics.bugSp.name }}</div>
      <div class="text-2xl font-bold text-status-danger tabular-nums">{{ single.teamMetrics.bugSp.displayValue }}</div>
      <div v-if="single.teamMetrics.bugSp.delta !== null" :class="['text-xs mt-1', deltaClass(single.teamMetrics.bugSp.deltaPolarity, single.teamMetrics.bugSp.deltaDirection)]">
        {{ deltaIcon(single.teamMetrics.bugSp.deltaDirection) }} {{ Math.abs(single.teamMetrics.bugSp.delta).toFixed(1) }} vs prior
      </div>
    </BaseCard>
    <BaseCard>
      <div class="text-xs text-text-muted mb-1">{{ single.teamMetrics.nonBugSp.name }}</div>
      <div class="text-2xl font-bold text-status-success tabular-nums">{{ single.teamMetrics.nonBugSp.displayValue }}</div>
      <div v-if="single.teamMetrics.nonBugSp.delta !== null" :class="['text-xs mt-1', deltaClass(single.teamMetrics.nonBugSp.deltaPolarity, single.teamMetrics.nonBugSp.deltaDirection)]">
        {{ deltaIcon(single.teamMetrics.nonBugSp.deltaDirection) }} {{ Math.abs(single.teamMetrics.nonBugSp.delta).toFixed(1) }} vs prior
      </div>
    </BaseCard>
  </div>
</template>
