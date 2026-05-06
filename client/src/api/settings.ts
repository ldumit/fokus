import type { AppSettings } from '../types'
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
