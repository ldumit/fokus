<script setup lang="ts">
import type { HealthScoreResult } from '../../types'

defineProps<{
  healthScore: HealthScoreResult
}>()

function ragClass(rag: string): string {
  switch (rag) {
    case 'green': return 'text-status-success'
    case 'amber': return 'text-status-warning'
    case 'red': return 'text-status-danger'
    default: return 'text-text-secondary'
  }
}

function ragBgClass(rag: string): string {
  switch (rag) {
    case 'green': return 'bg-status-success'
    case 'amber': return 'bg-status-warning'
    case 'red': return 'bg-status-danger'
    default: return 'bg-text-muted'
  }
}
</script>

<template>
  <div class="flex items-center gap-3">
    <div :class="['w-4 h-4 rounded-full shrink-0', ragBgClass(healthScore.compositeRag)]" />
    <div>
      <div :class="['text-3xl font-bold tabular-nums', ragClass(healthScore.compositeRag)]">
        {{ healthScore.compositeScore }}
      </div>
      <div class="text-xs text-text-muted uppercase tracking-wide mt-0.5">Health Score</div>
    </div>
    <div class="ml-4 flex gap-4 text-xs text-text-secondary">
      <div class="flex flex-col items-center gap-1">
        <span :class="ragClass(healthScore.completionRag)">{{ healthScore.completionSubScore }}</span>
        <span class="text-text-muted">Completion</span>
      </div>
      <div class="flex flex-col items-center gap-1">
        <span :class="ragClass(healthScore.disruptionRag)">{{ healthScore.disruptionSubScore }}</span>
        <span class="text-text-muted">Disruption</span>
      </div>
      <div class="flex flex-col items-center gap-1">
        <span :class="ragClass(healthScore.carryOverRag)">{{ healthScore.carryOverSubScore }}</span>
        <span class="text-text-muted">Carry-Over</span>
      </div>
    </div>
  </div>
</template>
