import type { AppSettings, BoardOption, DetectionResult, StatusOption, CycleTimeBoundariesResponse, SaveCycleTimeBoundariesResponse } from '../types'
import { apiFetch } from './client'

export function getSettings(): Promise<AppSettings> {
  return apiFetch<AppSettings>('/settings')
}

export function saveSettings(settings: AppSettings): Promise<{ success: boolean }> {
  return apiFetch<{ success: boolean }>('/settings', {
    method: 'PUT',
    body: JSON.stringify(settings)
  })
}

export function detectWorkflowStages(): Promise<DetectionResult> {
  return apiFetch<DetectionResult>('/settings/workflow-stages/detect')
}

export function getBoards(): Promise<{ boards: BoardOption[] }> {
  return apiFetch<{ boards: BoardOption[] }>('/boards')
}

export function getStatuses(): Promise<{ statuses: StatusOption[] }> {
  return apiFetch<{ statuses: StatusOption[] }>('/jira/statuses')
}

export function getExcludedStatuses(): Promise<string[]> {
  return apiFetch<string[]>('/settings/excluded-statuses')
}

export function saveExcludedStatuses(statuses: string[]): Promise<string[]> {
  return apiFetch<string[]>('/settings/excluded-statuses', {
    method: 'PUT',
    body: JSON.stringify({ statuses })
  })
}

export function getCycleTimeBoundaries(): Promise<CycleTimeBoundariesResponse> {
  return apiFetch<CycleTimeBoundariesResponse>('/settings/cycle-time-boundaries')
}

export function saveCycleTimeBoundaries(startStage: string, endStage: string): Promise<SaveCycleTimeBoundariesResponse> {
  return apiFetch<SaveCycleTimeBoundariesResponse>('/settings/cycle-time-boundaries', {
    method: 'PUT',
    body: JSON.stringify({ startStage, endStage })
  })
}
