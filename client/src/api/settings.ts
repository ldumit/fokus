import type { AppSettings, BoardOption, DetectionResult, StatusOption } from '../types'
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
