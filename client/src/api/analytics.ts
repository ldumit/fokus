import type { ClosedSprintItem, SprintSummaryResponse, DeveloperThroughputResponse, ScopeChangeResponse, CarryOverResponse, BugRatioResponse } from '../types'
import { apiFetch } from './client'

export function getSprintSummary(sprintId?: number, subTeam?: string): Promise<SprintSummaryResponse> {
  const params = new URLSearchParams()
  if (sprintId !== undefined) params.set('sprintId', String(sprintId))
  if (subTeam) params.set('subTeam', subTeam)
  const query = params.toString()
  return apiFetch<SprintSummaryResponse>(`/analytics/sprint-summary${query ? `?${query}` : ''}`)
}

export function getClosedSprints(): Promise<ClosedSprintItem[]> {
  return apiFetch<ClosedSprintItem[]>('/sprints/closed')
}

export function getSubTeams(): Promise<string[]> {
  return apiFetch<string[]>('/developers/sub-teams')
}

export function getDeveloperThroughput(sprintId?: number, last?: number, subTeam?: string): Promise<DeveloperThroughputResponse> {
  const params = new URLSearchParams()
  if (sprintId !== undefined) params.set('sprintId', String(sprintId))
  if (last !== undefined) params.set('last', String(last))
  if (subTeam) params.set('subTeam', subTeam)
  const query = params.toString()
  return apiFetch<DeveloperThroughputResponse>(`/analytics/developer-throughput${query ? `?${query}` : ''}`)
}

export function getScopeChange(sprintId?: number, last?: number, subTeam?: string): Promise<ScopeChangeResponse> {
  const params = new URLSearchParams()
  if (sprintId !== undefined) params.set('sprintId', String(sprintId))
  if (last !== undefined) params.set('last', String(last))
  if (subTeam) params.set('subTeam', subTeam)
  const query = params.toString()
  return apiFetch<ScopeChangeResponse>(`/analytics/scope-change${query ? `?${query}` : ''}`)
}

export function getCarryOver(sprintId?: number, last?: number, subTeam?: string): Promise<CarryOverResponse> {
  const params = new URLSearchParams()
  if (sprintId !== undefined) params.set('sprintId', String(sprintId))
  if (last !== undefined) params.set('last', String(last))
  if (subTeam) params.set('subTeam', subTeam)
  const query = params.toString()
  return apiFetch<CarryOverResponse>(`/analytics/carry-over${query ? `?${query}` : ''}`)
}

export function getBugRatio(sprintId?: number, last?: number, subTeam?: string): Promise<BugRatioResponse> {
  const params = new URLSearchParams()
  if (sprintId !== undefined) params.set('sprintId', String(sprintId))
  if (last !== undefined) params.set('last', String(last))
  if (subTeam) params.set('subTeam', subTeam)
  const query = params.toString()
  return apiFetch<BugRatioResponse>(`/analytics/bug-ratio${query ? `?${query}` : ''}`)
}
