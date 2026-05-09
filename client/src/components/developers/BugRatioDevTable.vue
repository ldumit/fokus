<script setup lang="ts">
import type { BugRatioMultiSprintResponse, BugRatioSingleSprintResponse } from '../../types'
import BaseCard from '../BaseCard.vue'

defineProps<{
  mode: 'multi' | 'single'
  multi?: BugRatioMultiSprintResponse
  single?: BugRatioSingleSprintResponse
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
</script>

<template>
  <!-- Multi-sprint developer table -->
  <BaseCard v-if="mode === 'multi' && multi">
    <div class="text-sm font-medium text-text-primary mb-4">Developer Bug Ratio</div>
    <div class="overflow-x-auto">
      <table class="w-full text-sm">
        <thead>
          <tr class="text-text-muted text-left border-b border-border-default">
            <th class="pb-2 pr-4 font-medium">Developer</th>
            <th class="pb-2 pr-4 font-medium">Sub-Team</th>
            <th class="pb-2 pr-4 font-medium text-right">Bug SP</th>
            <th class="pb-2 pr-4 font-medium text-right">Non-Bug SP</th>
            <th class="pb-2 pr-4 font-medium text-right">Bug Ratio %</th>
            <th class="pb-2 pr-4 font-medium text-right">Bug Tickets</th>
            <th class="pb-2 font-medium text-right">Non-Bug Tickets</th>
          </tr>
        </thead>
        <tbody>
          <tr
            v-for="dev in multi.developers"
            :key="dev.accountId"
            class="border-b border-border-default last:border-0"
          >
            <td class="py-2 pr-4">
              <div class="flex items-center gap-2">
                <img
                  v-if="dev.avatarUrl"
                  :src="dev.avatarUrl"
                  :alt="dev.displayName"
                  class="w-6 h-6 rounded-full shrink-0"
                />
                <div v-else class="w-6 h-6 rounded-full bg-surface-elevated shrink-0 flex items-center justify-center text-xs text-text-muted">
                  {{ dev.displayName.charAt(0).toUpperCase() }}
                </div>
                <span class="text-text-primary truncate">{{ dev.displayName }}</span>
                <!-- Alert badge -->
                <span
                  v-if="dev.alert.isActive"
                  class="text-amber-400 text-xs"
                  :title="`Bug ratio above ${dev.alert.thresholdPercent}% for ${dev.alert.consecutiveSprintCount} consecutive sprints`"
                >
                  &#9651;
                </span>
              </div>
            </td>
            <td class="py-2 pr-4 text-text-secondary">
              <span v-if="dev.subTeam" class="text-xs bg-surface-elevated rounded px-2 py-0.5">{{ dev.subTeam }}</span>
              <span v-else class="text-text-muted">—</span>
            </td>
            <td class="py-2 pr-4 text-right tabular-nums text-status-danger">{{ dev.bugSp.toFixed(1) }}</td>
            <td class="py-2 pr-4 text-right tabular-nums text-status-success">{{ dev.nonBugSp.toFixed(1) }}</td>
            <td class="py-2 pr-4 text-right tabular-nums text-text-primary">{{ dev.bugRatioPercent.toFixed(1) }}%</td>
            <td class="py-2 pr-4 text-right tabular-nums text-text-primary">{{ dev.bugTicketCount }}</td>
            <td class="py-2 text-right tabular-nums text-text-primary">{{ dev.nonBugTicketCount }}</td>
          </tr>
        </tbody>
      </table>
    </div>
  </BaseCard>

  <!-- Single-sprint developer table -->
  <BaseCard v-else-if="mode === 'single' && single">
    <div class="text-sm font-medium text-text-primary mb-4">Developer Bug Ratio — {{ single.sprint.name }}</div>
    <div class="overflow-x-auto">
      <table class="w-full text-sm">
        <thead>
          <tr class="text-text-muted text-left border-b border-border-default">
            <th class="pb-2 pr-4 font-medium">Developer</th>
            <th class="pb-2 pr-4 font-medium">Sub-Team</th>
            <th class="pb-2 pr-4 font-medium text-right">Bug SP</th>
            <th class="pb-2 pr-4 font-medium text-right">Non-Bug SP</th>
            <th class="pb-2 pr-4 font-medium text-right">Bug Ratio %</th>
            <th class="pb-2 pr-4 font-medium text-right">Bug Tickets</th>
            <th class="pb-2 font-medium text-right">Non-Bug Tickets</th>
          </tr>
        </thead>
        <tbody>
          <tr
            v-for="dev in single.developers"
            :key="dev.accountId"
            class="border-b border-border-default last:border-0"
          >
            <td class="py-2 pr-4">
              <div class="flex items-center gap-2">
                <img
                  v-if="dev.avatarUrl"
                  :src="dev.avatarUrl"
                  :alt="dev.displayName"
                  class="w-6 h-6 rounded-full shrink-0"
                />
                <div v-else class="w-6 h-6 rounded-full bg-surface-elevated shrink-0 flex items-center justify-center text-xs text-text-muted">
                  {{ dev.displayName.charAt(0).toUpperCase() }}
                </div>
                <span class="text-text-primary truncate">{{ dev.displayName }}</span>
                <span
                  v-if="dev.alert.isActive"
                  class="text-amber-400 text-xs"
                  :title="`Bug ratio above ${dev.alert.thresholdPercent}% for ${dev.alert.consecutiveSprintCount} consecutive sprints`"
                >
                  &#9651;
                </span>
              </div>
            </td>
            <td class="py-2 pr-4 text-text-secondary">
              <span v-if="dev.subTeam" class="text-xs bg-surface-elevated rounded px-2 py-0.5">{{ dev.subTeam }}</span>
              <span v-else class="text-text-muted">—</span>
            </td>
            <td class="py-2 pr-4 text-right tabular-nums text-status-danger">
              <span>{{ dev.bugSp.toFixed(1) }}</span>
              <span v-if="dev.delta" :class="['ml-1 text-xs', deltaClass(dev.delta.bugSpDeltaPolarity, dev.delta.bugSpDeltaDirection)]">
                {{ deltaIcon(dev.delta.bugSpDeltaDirection) }} {{ Math.abs(dev.delta.bugSpDelta).toFixed(1) }}
              </span>
            </td>
            <td class="py-2 pr-4 text-right tabular-nums text-status-success">
              <span>{{ dev.nonBugSp.toFixed(1) }}</span>
              <span v-if="dev.delta" :class="['ml-1 text-xs', deltaClass(dev.delta.nonBugSpDeltaPolarity, dev.delta.nonBugSpDeltaDirection)]">
                {{ deltaIcon(dev.delta.nonBugSpDeltaDirection) }} {{ Math.abs(dev.delta.nonBugSpDelta).toFixed(1) }}
              </span>
            </td>
            <td class="py-2 pr-4 text-right tabular-nums text-text-primary">
              <span>{{ dev.bugRatioPercent.toFixed(1) }}%</span>
              <span v-if="dev.delta" :class="['ml-1 text-xs', deltaClass(dev.delta.bugRatioPercentDeltaPolarity, dev.delta.bugRatioPercentDeltaDirection)]">
                {{ deltaIcon(dev.delta.bugRatioPercentDeltaDirection) }} {{ Math.abs(dev.delta.bugRatioPercentDelta).toFixed(1) }}
              </span>
            </td>
            <td class="py-2 pr-4 text-right tabular-nums text-text-primary">
              <span>{{ dev.bugTicketCount }}</span>
              <span v-if="dev.delta" :class="['ml-1 text-xs', deltaClass(dev.delta.bugTicketCountDeltaPolarity, dev.delta.bugTicketCountDeltaDirection)]">
                {{ deltaIcon(dev.delta.bugTicketCountDeltaDirection) }} {{ Math.abs(dev.delta.bugTicketCountDelta) }}
              </span>
            </td>
            <td class="py-2 text-right tabular-nums text-text-primary">
              <span>{{ dev.nonBugTicketCount }}</span>
              <span v-if="dev.delta" :class="['ml-1 text-xs', deltaClass(dev.delta.nonBugTicketCountDeltaPolarity, dev.delta.nonBugTicketCountDeltaDirection)]">
                {{ deltaIcon(dev.delta.nonBugTicketCountDeltaDirection) }} {{ Math.abs(dev.delta.nonBugTicketCountDelta) }}
              </span>
            </td>
          </tr>
        </tbody>
      </table>
    </div>
  </BaseCard>
</template>
