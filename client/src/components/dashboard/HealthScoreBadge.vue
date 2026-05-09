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
      <div class="flex items-center gap-1 mt-0.5">
        <div class="text-xs text-text-muted uppercase tracking-wide">Health Score</div>
        <span
          class="text-text-muted cursor-help"
          title="Weighted composite of completion, disruption, and carry-over rates. 0-100 scale with green/amber/red status."
        >
          <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 20 20" fill="currentColor" class="w-3.5 h-3.5">
            <path fill-rule="evenodd" d="M18 10a8 8 0 11-16 0 8 8 0 0116 0zm-7-4a1 1 0 11-2 0 1 1 0 012 0zM9 9a.75.75 0 000 1.5h.253a.25.25 0 01.244.304l-.459 2.066A1.75 1.75 0 0010.747 15H11a.75.75 0 000-1.5h-.253a.25.25 0 01-.244-.304l.459-2.066A1.75 1.75 0 009.253 9H9z" clip-rule="evenodd" />
          </svg>
        </span>
      </div>
    </div>
    <div class="ml-4 flex gap-4 text-xs text-text-secondary">
      <div class="flex flex-col items-center gap-1">
        <span :class="ragClass(healthScore.completionRag)">{{ healthScore.completionSubScore }}</span>
        <span
          class="text-text-muted cursor-help"
          title="Individual 0-100 scores for completion, disruption, and carry-over that feed the composite."
        >Completion</span>
      </div>
      <div class="flex flex-col items-center gap-1">
        <span :class="ragClass(healthScore.disruptionRag)">{{ healthScore.disruptionSubScore }}</span>
        <span
          class="text-text-muted cursor-help"
          title="Individual 0-100 scores for completion, disruption, and carry-over that feed the composite."
        >Disruption</span>
      </div>
      <div class="flex flex-col items-center gap-1">
        <span :class="ragClass(healthScore.carryOverRag)">{{ healthScore.carryOverSubScore }}</span>
        <span
          class="text-text-muted cursor-help"
          title="Individual 0-100 scores for completion, disruption, and carry-over that feed the composite."
        >Carry-Over</span>
      </div>
    </div>
  </div>
</template>
