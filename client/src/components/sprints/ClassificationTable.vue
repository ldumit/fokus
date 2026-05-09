<script setup lang="ts">
import BaseCard from '../BaseCard.vue'
import type { ClassificationEntry } from '../../types'

defineProps<{
  entries: ClassificationEntry[]
}>()
</script>

<template>
  <BaseCard>
    <div class="text-sm font-medium text-text-primary mb-4">Classification Breakdown</div>
    <div v-if="entries.length === 0" class="text-sm text-text-muted">No mid-sprint additions.</div>
    <table v-else class="w-full text-sm">
      <thead>
        <tr class="text-text-muted text-left border-b border-border-default">
          <th class="pb-2 pr-4 font-medium">Category</th>
          <th class="pb-2 pr-4 font-medium text-right">Tickets</th>
          <th class="pb-2 pr-4 font-medium text-right">SP Total</th>
          <th class="pb-2 font-medium text-right">% of Additions</th>
        </tr>
      </thead>
      <tbody>
        <tr
          v-for="entry in entries"
          :key="entry.category"
          class="border-b border-border-default last:border-0"
        >
          <td class="py-2 pr-4 text-text-primary">{{ entry.category }}</td>
          <td class="py-2 pr-4 text-right tabular-nums text-text-primary">{{ entry.ticketCount }}</td>
          <td class="py-2 pr-4 text-right tabular-nums text-text-secondary">
            {{ entry.spTotal !== null ? entry.spTotal.toFixed(1) : '—' }}
          </td>
          <td class="py-2 text-right tabular-nums text-text-secondary">{{ entry.percentage.toFixed(1) }}%</td>
        </tr>
      </tbody>
    </table>
  </BaseCard>
</template>
