import { defineStore } from 'pinia'
import { ref } from 'vue'
import type { SprintItem, SprintSummaryResponse, QaMetricsResponse, UntestedTicket, FailingTicket } from '../types'
import { getSprintSummary, getSprints, getSubTeams, getQaMetrics, getUntestedTickets, getFailingTickets } from '../api/analytics'

export const useDashboardStore = defineStore('dashboard', () => {
  const sprints = ref<SprintItem[]>([])
  const subTeams = ref<string[]>([])
  const selectedSprintId = ref<number | null>(null)
  const selectedSubTeam = ref<string | null>(null)
  const summary = ref<SprintSummaryResponse | null>(null)
  const loading = ref(false)
  const initializing = ref(false)
  const error = ref<string | null>(null)

  const qaMetrics = ref<QaMetricsResponse | null>(null)
  const untestedTickets = ref<UntestedTicket[]>([])
  const failingTickets = ref<FailingTicket[]>([])
  const qaLoading = ref(false)
  const qaMetricsError = ref<string | null>(null)
  const untestedError = ref<string | null>(null)
  const failingError = ref<string | null>(null)

  async function initialize() {
    initializing.value = true
    error.value = null
    try {
      const [sprintList, teams] = await Promise.all([getSprints(), getSubTeams()])
      sprints.value = sprintList
      subTeams.value = teams

      if (sprintList.length > 0 && selectedSprintId.value === null) {
        selectedSprintId.value = sprintList[0].id
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
    clearQaState()
    await fetchSummary()
  }

  async function selectSubTeam(subTeam: string | null) {
    selectedSubTeam.value = subTeam
    clearQaState()
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

  function clearQaState() {
    qaMetrics.value = null
    untestedTickets.value = []
    failingTickets.value = []
    qaMetricsError.value = null
    untestedError.value = null
    failingError.value = null
  }

  async function fetchQaMetrics() {
    if (selectedSprintId.value === null) return
    qaLoading.value = true
    qaMetricsError.value = null
    try {
      qaMetrics.value = await getQaMetrics(selectedSprintId.value, selectedSubTeam.value ?? undefined)
    } catch (e) {
      qaMetricsError.value = e instanceof Error ? e.message : 'Failed to load QA metrics'
    } finally {
      qaLoading.value = false
    }
  }

  async function fetchUntestedTickets() {
    if (selectedSprintId.value === null) return
    untestedError.value = null
    try {
      const result = await getUntestedTickets(selectedSprintId.value, selectedSubTeam.value ?? undefined)
      untestedTickets.value = result.tickets
    } catch (e) {
      untestedError.value = e instanceof Error ? e.message : 'Failed to load untested tickets'
    }
  }

  async function fetchFailingTickets() {
    if (selectedSprintId.value === null) return
    failingError.value = null
    try {
      const result = await getFailingTickets(selectedSprintId.value, selectedSubTeam.value ?? undefined)
      failingTickets.value = result.tickets
    } catch (e) {
      failingError.value = e instanceof Error ? e.message : 'Failed to load failing tickets'
    }
  }

  return {
    sprints,
    subTeams,
    selectedSprintId,
    selectedSubTeam,
    summary,
    loading,
    initializing,
    error,
    qaMetrics,
    untestedTickets,
    failingTickets,
    qaLoading,
    qaMetricsError,
    untestedError,
    failingError,
    initialize,
    selectSprint,
    selectSubTeam,
    fetchSummary,
    fetchQaMetrics,
    fetchUntestedTickets,
    fetchFailingTickets
  }
})
