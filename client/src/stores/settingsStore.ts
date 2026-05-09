import { defineStore } from 'pinia'
import { ref } from 'vue'
import type { AppSettings, DetectionResult } from '../types'
import { getSettings, saveSettings, detectWorkflowStages } from '../api/settings'

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
    bugRatioConsecutiveSprintCount: 2
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

  async function updateSettings(updated: AppSettings) {
    saving.value = true
    error.value = null
    try {
      await saveSettings(updated)
      settings.value = updated
    } catch (e) {
      error.value = e instanceof Error ? e.message : 'Failed to save settings'
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
    updateSettings,
    runDetectWorkflowStages,
    clearDetection,
    moveSidelinedToStages,
    removeSidelinedStatus
  }
})
