import { defineStore } from 'pinia'
import { ref } from 'vue'
import type { ClosedSprintItem, DeveloperThroughputResponse, BugRatioResponse } from '../types'
import { getDeveloperThroughput, getBugRatio, getClosedSprints, getSubTeams } from '../api/analytics'
import { setDeveloperCapacity } from '../api/developers'

export const useDevelopersStore = defineStore('developers', () => {
  const closedSprints = ref<ClosedSprintItem[]>([])
  const subTeams = ref<string[]>([])
  const selectedSprintId = ref<number | null>(null)
  const selectedLast = ref<number | null>(null)
  const sprintMode = ref<'single' | 'multi'>('single')
  const selectedSubTeam = ref<string | null>(null)
  const throughput = ref<DeveloperThroughputResponse | null>(null)
  const loading = ref(false)
  const initializing = ref(false)
  const error = ref<string | null>(null)

  const activeTab = ref<'throughput' | 'bugRatio'>('throughput')
  const bugRatio = ref<BugRatioResponse | null>(null)
  const bugRatioLoading = ref(false)
  const bugRatioError = ref<string | null>(null)

  async function initialize() {
    initializing.value = true
    error.value = null
    try {
      const [sprints, teams] = await Promise.all([getClosedSprints(), getSubTeams()])
      closedSprints.value = sprints
      subTeams.value = teams

      if (sprints.length > 0 && selectedSprintId.value === null && sprintMode.value === 'single') {
        selectedSprintId.value = sprints[0].id
      }

      await fetchThroughput()
    } catch (e) {
      error.value = e instanceof Error ? e.message : 'Failed to initialize developers page'
    } finally {
      initializing.value = false
    }
  }

  async function selectSprint(sprintId: number) {
    sprintMode.value = 'single'
    selectedSprintId.value = sprintId
    selectedLast.value = null
    await fetchThroughput()
    if (activeTab.value === 'bugRatio') {
      await fetchBugRatio()
    }
  }

  async function selectLastN(n: number | null) {
    sprintMode.value = 'multi'
    selectedLast.value = n
    selectedSprintId.value = null
    await fetchThroughput()
    if (activeTab.value === 'bugRatio') {
      await fetchBugRatio()
    }
  }

  async function selectSubTeam(subTeam: string | null) {
    selectedSubTeam.value = subTeam
    await fetchThroughput()
    if (activeTab.value === 'bugRatio') {
      await fetchBugRatio()
    }
  }

  async function switchTab(tab: 'throughput' | 'bugRatio') {
    activeTab.value = tab
    if (tab === 'bugRatio') {
      await fetchBugRatio()
    }
  }

  async function fetchThroughput() {
    loading.value = true
    error.value = null
    try {
      if (sprintMode.value === 'single') {
        throughput.value = await getDeveloperThroughput(
          selectedSprintId.value ?? undefined,
          undefined,
          selectedSubTeam.value ?? undefined
        )
      } else {
        throughput.value = await getDeveloperThroughput(
          undefined,
          selectedLast.value ?? undefined,
          selectedSubTeam.value ?? undefined
        )
      }
    } catch (e) {
      error.value = e instanceof Error ? e.message : 'Failed to load developer throughput'
    } finally {
      loading.value = false
    }
  }

  async function fetchBugRatio() {
    bugRatioLoading.value = true
    bugRatioError.value = null
    try {
      if (sprintMode.value === 'single') {
        bugRatio.value = await getBugRatio(
          selectedSprintId.value ?? undefined,
          undefined,
          selectedSubTeam.value ?? undefined
        )
      } else {
        bugRatio.value = await getBugRatio(
          undefined,
          selectedLast.value ?? undefined,
          selectedSubTeam.value ?? undefined
        )
      }
    } catch (e) {
      bugRatioError.value = e instanceof Error ? e.message : 'Failed to load bug ratio data'
    } finally {
      bugRatioLoading.value = false
    }
  }

  async function updateCapacity(accountId: string, sprintId: number, capacityPercent: number) {
    if (throughput.value) {
      const devEntry = throughput.value.developers.find(d => d.accountId === accountId)
      const breakdown = devEntry?.sprintBreakdowns.find(b => b.sprintId === sprintId)
      const previousCapacity = breakdown?.capacityPercent ?? 100

      if (breakdown) {
        breakdown.capacityPercent = capacityPercent
      }

      try {
        await setDeveloperCapacity(accountId, sprintId, capacityPercent)
        await fetchThroughput()
      } catch (e) {
        if (breakdown) {
          breakdown.capacityPercent = previousCapacity
        }
        error.value = e instanceof Error ? e.message : 'Failed to update capacity'
      }
    } else {
      try {
        await setDeveloperCapacity(accountId, sprintId, capacityPercent)
        await fetchThroughput()
      } catch (e) {
        error.value = e instanceof Error ? e.message : 'Failed to update capacity'
      }
    }
  }

  return {
    closedSprints,
    subTeams,
    selectedSprintId,
    selectedLast,
    sprintMode,
    selectedSubTeam,
    throughput,
    loading,
    initializing,
    error,
    activeTab,
    bugRatio,
    bugRatioLoading,
    bugRatioError,
    initialize,
    selectSprint,
    selectLastN,
    selectSubTeam,
    switchTab,
    fetchThroughput,
    fetchBugRatio,
    updateCapacity
  }
})
