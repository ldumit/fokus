import { defineStore } from 'pinia'
import { ref } from 'vue'
import type { ClosedSprintItem, ScopeChangeResponse } from '../types'
import { getScopeChange, getClosedSprints, getSubTeams } from '../api/analytics'

export const useSprintsStore = defineStore('sprints', () => {
  const closedSprints = ref<ClosedSprintItem[]>([])
  const subTeams = ref<string[]>([])
  const selectedSprintId = ref<number | null>(null)
  const selectedLast = ref<number | null>(5)
  const sprintMode = ref<'single' | 'multi'>('multi')
  const selectedSubTeam = ref<string | null>(null)
  const scopeChange = ref<ScopeChangeResponse | null>(null)
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

      await fetchScopeChange()
    } catch (e) {
      error.value = e instanceof Error ? e.message : 'Failed to initialize sprints page'
    } finally {
      initializing.value = false
    }
  }

  async function selectSprint(sprintId: number) {
    sprintMode.value = 'single'
    selectedSprintId.value = sprintId
    selectedLast.value = null
    await fetchScopeChange()
  }

  async function selectLastN(n: number | null) {
    sprintMode.value = 'multi'
    selectedLast.value = n
    selectedSprintId.value = null
    await fetchScopeChange()
  }

  async function selectSubTeam(subTeam: string | null) {
    selectedSubTeam.value = subTeam
    await fetchScopeChange()
  }

  async function fetchScopeChange() {
    loading.value = true
    error.value = null
    try {
      if (sprintMode.value === 'single') {
        scopeChange.value = await getScopeChange(
          selectedSprintId.value ?? undefined,
          undefined,
          selectedSubTeam.value ?? undefined
        )
      } else {
        // selectedLast === null means "All Sprints" — send 0 as sentinel
        const last = selectedLast.value === null ? 0 : selectedLast.value
        scopeChange.value = await getScopeChange(
          undefined,
          last,
          selectedSubTeam.value ?? undefined
        )
      }
    } catch (e) {
      error.value = e instanceof Error ? e.message : 'Failed to load scope change data'
    } finally {
      loading.value = false
    }
  }

  return {
    closedSprints,
    subTeams,
    selectedSprintId,
    selectedLast,
    sprintMode,
    selectedSubTeam,
    scopeChange,
    loading,
    initializing,
    error,
    initialize,
    selectSprint,
    selectLastN,
    selectSubTeam,
    fetchScopeChange
  }
})
