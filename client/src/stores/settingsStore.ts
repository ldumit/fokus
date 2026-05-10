import { defineStore } from 'pinia'
import { ref } from 'vue'
import type { AppSettings, DetectionResult, HealthThresholdConfig, HealthWeightConfig } from '../types'
import { getSettings, saveBoard, saveDoneStatuses, saveWorkflowStages, saveHealthConfig, saveBugRatioAlerts, saveSyncConfig, detectWorkflowStages } from '../api/settings'

export const useSettingsStore = defineStore('settings', () => {
  const settings = ref<AppSettings>({
    boardId: null,
    doneStatuses: ['Done', 'Closed'],
    workflowStages: [],
    healthThresholds: {
      completionGreen: 80,
      completionAmber: 60,
      disruptionGreen: 10,
      disruptionAmber: 25,
      carryOverGreen: 10,
      carryOverAmber: 25
    },
    healthWeights: {
      completion: 40,
      disruption: 30,
      carryOver: 30
    },
    bugRatioAlertThreshold: 50,
    bugRatioConsecutiveSprintCount: 2,
    syncBackSprintCount: 20,
    planningWindowDays: 2,
    defaultSpPerBug: 3
  })

  const loading = ref(false)
  const saving = ref(false)
  const error = ref<string | null>(null)

  const detectionResult = ref<DetectionResult | null>(null)
  const detecting = ref(false)

  async function fetchSettings() {
    loading.value = true
    error.value = null
    try {
      settings.value = await getSettings()
    } catch (e) {
      error.value = e instanceof Error ? e.message : 'Failed to load settings'
    } finally {
      loading.value = false
    }
  }

  async function saveBoardAction(boardId: number | null) {
    saving.value = true
    error.value = null
    try {
      await saveBoard(boardId)
      settings.value = { ...settings.value, boardId }
    } catch (e) {
      error.value = e instanceof Error ? e.message : 'Failed to save board'
    } finally {
      saving.value = false
    }
  }

  async function saveDoneStatusesAction(statuses: string[]) {
    saving.value = true
    error.value = null
    try {
      const result = await saveDoneStatuses(statuses)
      settings.value = { ...settings.value, doneStatuses: result }
    } catch (e) {
      error.value = e instanceof Error ? e.message : 'Failed to save done statuses'
    } finally {
      saving.value = false
    }
  }

  async function saveWorkflowStagesAction(stages: string[]) {
    saving.value = true
    error.value = null
    try {
      const result = await saveWorkflowStages(stages)
      settings.value = { ...settings.value, workflowStages: result }
    } catch (e) {
      error.value = e instanceof Error ? e.message : 'Failed to save workflow stages'
    } finally {
      saving.value = false
    }
  }

  async function saveHealthConfigAction(healthThresholds: HealthThresholdConfig, healthWeights: HealthWeightConfig) {
    saving.value = true
    error.value = null
    try {
      await saveHealthConfig(healthThresholds, healthWeights)
      settings.value = { ...settings.value, healthThresholds: { ...healthThresholds }, healthWeights: { ...healthWeights } }
    } catch (e) {
      error.value = e instanceof Error ? e.message : 'Failed to save health config'
    } finally {
      saving.value = false
    }
  }

  async function saveBugRatioAlertsAction(alertThreshold: number, consecutiveSprintCount: number, defaultSpPerBug: number) {
    saving.value = true
    error.value = null
    try {
      await saveBugRatioAlerts(alertThreshold, consecutiveSprintCount, defaultSpPerBug)
      settings.value = { ...settings.value, bugRatioAlertThreshold: alertThreshold, bugRatioConsecutiveSprintCount: consecutiveSprintCount, defaultSpPerBug }
    } catch (e) {
      error.value = e instanceof Error ? e.message : 'Failed to save bug ratio alerts'
    } finally {
      saving.value = false
    }
  }

  async function saveSyncConfigAction(syncBackSprintCount: number, planningWindowDays: number) {
    saving.value = true
    error.value = null
    try {
      await saveSyncConfig(syncBackSprintCount, planningWindowDays)
      settings.value = { ...settings.value, syncBackSprintCount, planningWindowDays }
    } catch (e) {
      error.value = e instanceof Error ? e.message : 'Failed to save sync config'
    } finally {
      saving.value = false
    }
  }

  async function runDetectWorkflowStages() {
    detecting.value = true
    error.value = null
    try {
      detectionResult.value = await detectWorkflowStages()
    } catch (e) {
      error.value = e instanceof Error ? e.message : 'Failed to detect workflow stages'
    } finally {
      detecting.value = false
    }
  }

  function clearDetection() {
    detectionResult.value = null
  }

  function moveSidelinedToStages(status: string) {
    if (detectionResult.value === null) return
    const idx = detectionResult.value.sidelined.indexOf(status)
    if (idx !== -1) {
      detectionResult.value.sidelined.splice(idx, 1)
    }
  }

  function removeSidelinedStatus(status: string) {
    if (detectionResult.value === null) return
    const idx = detectionResult.value.sidelined.indexOf(status)
    if (idx !== -1) {
      detectionResult.value.sidelined.splice(idx, 1)
    }
  }

  return {
    settings,
    loading,
    saving,
    error,
    detectionResult,
    detecting,
    fetchSettings,
    saveBoardAction,
    saveDoneStatusesAction,
    saveWorkflowStagesAction,
    saveHealthConfigAction,
    saveBugRatioAlertsAction,
    saveSyncConfigAction,
    runDetectWorkflowStages,
    clearDetection,
    moveSidelinedToStages,
    removeSidelinedStatus
  }
})
