import { defineStore, storeToRefs } from 'pinia'
import { ref, computed } from 'vue'
import type { EpicProgressResponse, EpicProgressEntry } from '../types'
import { getEpicProgress, getSubTeams } from '../api/analytics'
import { useSettingsStore } from './settingsStore'

export const useEpicsStore = defineStore('epics', () => {
  const settingsStore = useSettingsStore()
  const { settings } = storeToRefs(settingsStore)

  const subTeams = ref<string[]>([])
  const selectedSubTeam = ref<string | null>(null)
  const activeFilter = ref<'active' | 'completed'>('active')
  const epicProgress = ref<EpicProgressResponse | null>(null)
  const expandedEpicKeys = ref<Set<string>>(new Set())
  const loading = ref(false)
  const initializing = ref(false)
  const error = ref<string | null>(null)

  const filteredEpics = computed<EpicProgressEntry[]>(() => {
    if (!epicProgress.value) return []
    return activeFilter.value === 'active'
      ? epicProgress.value.epics.filter(e => !e.isCompleted)
      : epicProgress.value.epics.filter(e => e.isCompleted)
  })

  const hasEpics = computed<boolean>(() =>
    (epicProgress.value?.epics.length ?? 0) > 0
  )

  const isXrayEnabled = computed<boolean>(() =>
    settings.value.xrayEnabled
  )

  const averageTestCoverage = computed<number | null>(() =>
    epicProgress.value?.averageTestCoverage ?? null
  )

  async function initialize() {
    initializing.value = true
    error.value = null
    try {
      const [teams] = await Promise.all([
        getSubTeams(),
        settingsStore.fetchSettings(),
        fetchEpicProgress()
      ])
      subTeams.value = teams
    } catch (e) {
      error.value = e instanceof Error ? e.message : 'Failed to initialize epics'
    } finally {
      initializing.value = false
    }
  }

  async function fetchEpicProgress() {
    loading.value = true
    error.value = null
    try {
      epicProgress.value = await getEpicProgress(selectedSubTeam.value ?? undefined)
    } catch (e) {
      error.value = e instanceof Error ? e.message : 'Failed to load epic progress'
    } finally {
      loading.value = false
    }
  }

  async function selectSubTeam(subTeam: string | null) {
    selectedSubTeam.value = subTeam
    await fetchEpicProgress()
  }

  function setActiveFilter(filter: 'active' | 'completed') {
    activeFilter.value = filter
  }

  function toggleEpicExpanded(epicKey: string) {
    if (expandedEpicKeys.value.has(epicKey)) {
      expandedEpicKeys.value.delete(epicKey)
    } else {
      expandedEpicKeys.value.add(epicKey)
    }
  }

  function isEpicExpanded(epicKey: string): boolean {
    return expandedEpicKeys.value.has(epicKey)
  }

  return {
    subTeams,
    selectedSubTeam,
    activeFilter,
    epicProgress,
    expandedEpicKeys,
    loading,
    initializing,
    error,
    filteredEpics,
    hasEpics,
    isXrayEnabled,
    averageTestCoverage,
    initialize,
    fetchEpicProgress,
    selectSubTeam,
    setActiveFilter,
    toggleEpicExpanded,
    isEpicExpanded
  }
})
