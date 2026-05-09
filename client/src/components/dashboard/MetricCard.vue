<script setup lang="ts">
import type { MetricCard } from '../../types'
import { computed } from 'vue'

const props = defineProps<{
  metric: MetricCard
}>()

function metricTooltip(name: string): string {
  if (name === 'SP Completed') return 'Story points completed versus story points committed at sprint start.'
  if (name === 'Completion %') return 'Percentage of committed story points completed. Higher is better.'
  if (name === 'Scope Disruption Rate') return 'Non-bug work added mid-sprint as % of committed SP. Lower is better.'
  if (name === 'Bug Disruption Rate') return 'Bug work added mid-sprint as % of committed SP. Lower is better.'
  if (name === 'Carry-Over Rate') return 'Percentage of total sprint scope (committed + added) not completed. Lower is better.'
  return ''
}

const deltaArrow = computed(() => {
  switch (props.metric.deltaDirection) {
    case 'up': return '↑'
    case 'down': return '↓'
    default: return '—'
  }
})

const deltaClass = computed(() => {
  switch (props.metric.deltaPolarity) {
    case 'positive': return 'text-status-success'
    case 'negative': return 'text-status-danger'
    default: return 'text-text-secondary'
  }
})

const sparklineOptions = computed(() => ({
  chart: {
    type: 'line',
    sparkline: { enabled: true },
    animations: { enabled: false }
  },
  stroke: { curve: 'smooth', width: 2 },
  colors: ['#4f7cff'],
  tooltip: { enabled: false }
}))

const sparklineSeries = computed(() => [
  {
    name: props.metric.name,
    data: props.metric.sparkline.map(p => p.value)
  }
])
</script>

<template>
  <div class="bg-surface-card border border-border-default rounded-lg p-4 flex flex-col gap-3">
    <div
      class="text-xs text-text-muted uppercase tracking-wide cursor-help"
      :title="metricTooltip(metric.name)"
    >{{ metric.name }}</div>
    <div class="text-2xl font-bold text-text-primary tabular-nums">{{ metric.displayValue }}</div>
    <div class="flex items-center justify-between gap-2">
      <div
        v-if="metric.delta !== null"
        :class="['text-sm font-medium cursor-help', deltaClass]"
        title="Change versus the prior closed sprint. Arrow direction and color show whether the metric improved."
      >
        {{ deltaArrow }} {{ Math.abs(metric.delta) }}
      </div>
      <div v-else class="text-sm text-text-muted">—</div>
      <div
        v-if="metric.sparkline.length > 1"
        class="w-24 h-8"
        title="Trend of the last 4 sprints ending at the selected sprint."
      >
        <apexchart
          type="line"
          height="32"
          :options="sparklineOptions"
          :series="sparklineSeries"
        />
      </div>
    </div>
  </div>
</template>
