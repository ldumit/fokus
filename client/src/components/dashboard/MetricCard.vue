<script setup lang="ts">
import type { MetricCard } from '../../types'
import { computed } from 'vue'

const props = defineProps<{
  metric: MetricCard
}>()

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
    <div class="text-xs text-text-muted uppercase tracking-wide">{{ metric.name }}</div>
    <div class="text-2xl font-bold text-text-primary tabular-nums">{{ metric.displayValue }}</div>
    <div class="flex items-center justify-between gap-2">
      <div v-if="metric.delta !== null" :class="['text-sm font-medium', deltaClass]">
        {{ deltaArrow }} {{ Math.abs(metric.delta) }}
      </div>
      <div v-else class="text-sm text-text-muted">—</div>
      <div v-if="metric.sparkline.length > 1" class="w-24 h-8">
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
