<script setup lang="ts">
import type { EpicProgressTicketEntry } from '../../types'

defineProps<{
  tickets: EpicProgressTicketEntry[]
}>()
</script>

<template>
  <div class="overflow-x-auto mt-3 border-t border-border-default pt-3">
    <table class="w-full text-sm">
      <thead>
        <tr class="text-text-muted text-left border-b border-border-default">
          <th class="pb-2 pr-4 font-medium">
            <span title="Individual ticket details within the epic.">Key</span>
          </th>
          <th class="pb-2 pr-4 font-medium">Summary</th>
          <th class="pb-2 pr-4 font-medium">Type</th>
          <th class="pb-2 pr-4 font-medium text-right">SP</th>
          <th class="pb-2 pr-4 font-medium">Status</th>
          <th class="pb-2 font-medium">Assignee</th>
        </tr>
      </thead>
      <tbody>
        <tr
          v-for="ticket in tickets"
          :key="ticket.ticketKey"
          :class="[
            'border-b border-border-default last:border-0',
            ticket.isDone ? 'opacity-60' : ''
          ]"
        >
          <td class="py-2 pr-4 text-text-secondary tabular-nums font-mono text-xs whitespace-nowrap">
            {{ ticket.ticketKey }}
          </td>
          <td class="py-2 pr-4 text-text-primary max-w-xs truncate">
            {{ ticket.summary }}
          </td>
          <td class="py-2 pr-4 text-text-secondary text-xs">
            {{ ticket.issueType }}
          </td>
          <td class="py-2 pr-4 text-right tabular-nums text-text-primary">
            {{ ticket.storyPoints !== null ? ticket.storyPoints : '—' }}
          </td>
          <td class="py-2 pr-4 text-xs">
            <span
              :class="[
                'inline-block rounded px-1.5 py-0.5',
                ticket.isDone
                  ? 'bg-status-success/10 text-status-success'
                  : 'bg-surface-elevated text-text-secondary'
              ]"
            >
              {{ ticket.currentStatus }}
            </span>
          </td>
          <td class="py-2 text-text-secondary text-xs">
            {{ ticket.assigneeDisplayName ?? 'Unassigned' }}
          </td>
        </tr>
      </tbody>
    </table>
  </div>
</template>
