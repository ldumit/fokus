import { defineStore } from 'pinia'
import { ref } from 'vue'
import type { AppSettings } from '../types'
import { getSettings, saveSettings } from '../api/settings'

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
    }
  })

  const loading = ref(false)
  const saving = ref(false)
  const error = ref<string | null>(null)

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

  return { settings, loading, saving, error, fetchSettings, updateSettings }
})
