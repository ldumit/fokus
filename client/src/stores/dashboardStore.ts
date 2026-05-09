import { defineStore } from 'pinia'
import { ref } from 'vue'
import type { ClosedSprintItem, SprintSummaryResponse } from '../types'
import { getSprintSummary, getClosedSprints, getSubTeams } from '../api/analytics'

export const useDashboardStore = defineStore('dashboard', () => {
  const closedSprints = ref<ClosedSprintItem[]>([])
  const subTeams = ref<string[]>([])
  const selectedSprintId = ref<number | null>(null)
  const selectedSubTeam = ref<string | null>(null)
  const summary = ref<SprintSummaryResponse | null>(null)
  const loading = ref(false)
  const initializing = ref(false)
  const error = ref<string | null>(null)

  async function initialize() {
    initializing.value = true
    error.value = null
    try {
      const [sprints, teams] = await Promise.all([getClosedSprints(), getSubTeams()])
      closedSprints.value = sprints
      subTeams.value = teams

      if (sprints.length > 0 && selectedSprintId.value === null) {
        selectedSprintId.value = sprints[0].id
      }

      await fetchSummary()
    } catch (e) {
      error.value = e instanceof Error ? e.message : 'Failed to initialize dashboard'
    } finally {
      initializing.value = false
    }
  }

  async function selectSprint(sprintId: number) {
    selectedSprintId.value = sprintId
    await fetchSummary()
  }

  async function selectSubTeam(subTeam: string | null) {
    selectedSubTeam.value = subTeam
    await fetchSummary()
  }

  async function fetchSummary() {
    loading.value = true
    error.value = null
    try {
      summary.value = await getSprintSummary(
        selectedSprintId.value ?? undefined,
        selectedSubTeam.value ?? undefined
      )
    } catch (e) {
      error.value = e instanceof Error ? e.message : 'Failed to load sprint summary'
    } finally {
      loading.value = false
    }
  }

  return {
    closedSprints,
    subTeams,
    selectedSprintId,
    selectedSubTeam,
    summary,
    loading,
    initializing,
    error,
    initialize,
    selectSprint,
    selectSubTeam,
    fetchSummary
  }
})
