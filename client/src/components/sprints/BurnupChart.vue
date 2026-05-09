<script setup lang="ts">
import { computed } from 'vue'
import BaseCard from '../BaseCard.vue'
import type { BurnupDataPoint } from '../../types'

const props = defineProps<{
  burnupData: BurnupDataPoint[]
}>()

const xLabels = computed(() =>
  props.burnupData.map(d => {
    const date = new Date(d.date)
    return `Day ${d.dayNumber} (${date.toLocaleDateString('en-GB', { month: 'short', day: 'numeric' })})`
  })
)

const series = computed(() => [
  {
    name: 'Total Scope SP',
    type: 'line',
    data: props.burnupData.map(d => d.totalScopeSp)
  },
  {
    name: 'Completed SP',
    type: 'area',
    data: props.burnupData.map(d => d.completedSp)
  }
])

// Planning phase annotation: days 1-2
const planningEndIndex = computed(() =>
  props.burnupData.filter(d => d.phase === 'planning').length - 1
)

const chartOptions = computed(() => ({
  chart: {
    type: 'line',
    background: 'transparent',
    toolbar: { show: false },
    animations: { enabled: false }
  },
  stroke: { curve: 'smooth', width: [2, 1] },
  fill: {
    type: ['solid', 'gradient'],
    gradient: {
      shade: 'dark',
      type: 'vertical',
      opacityFrom: 0.4,
      opacityTo: 0.05
    }
  },
  colors: ['#f97316', '#22c55e'],
  xaxis: {
    categories: xLabels.value,
    labels: { style: { colors: '#9ca3af', fontSize: '11px' }, rotate: -30 }
  },
  yaxis: {
    labels: { style: { colors: '#9ca3af', fontSize: '12px' } },
    title: { text: 'Story Points', style: { color: '#9ca3af' } }
  },
  tooltip: { theme: 'dark' },
  legend: { labels: { colors: '#d1d5db' } },
  grid: { borderColor: '#374151' },
  theme: { mode: 'dark' },
  annotations: {
    xaxis: planningEndIndex.value >= 0 ? [
      {
        x: xLabels.value[planningEndIndex.value],
        strokeDashArray: 4,
        borderColor: '#6b7280',
        label: {
          text: 'Planning ends',
          style: { color: '#9ca3af', background: 'transparent', fontSize: '11px' }
        }
      }
    ] : []
  }
}))
</script>

<template>
  <BaseCard>
    <div class="text-sm font-medium text-text-primary mb-1">Scope Burnup</div>
    <p class="text-xs text-text-muted mb-4">Days 1-2 are the planning phase. Day 3+ is execution.</p>
    <apexchart
      type="line"
      height="280"
      :options="chartOptions"
      :series="series"
    />
  </BaseCard>
</template>
