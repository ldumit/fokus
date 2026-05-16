import type { ClosedSprintItem, SprintSummaryResponse, DeveloperThroughputResponse, ScopeChangeResponse, CarryOverResponse, BugRatioResponse, EpicProgressResponse, CycleTimeResponse, LeaderboardResponse, QaMetricsResponse, UntestedTicketsResponse, FailingTicketsResponse } from '../types'
import { apiFetch } from './client'

export interface UpdateSprintRequest {
  name: string
  startDate: string
  endDate: string
  goal?: string
}

export interface UpdateSprintResponse {
  id: number
  name: string
  startDate: string
  endDate: string
  goal?: string
  state: string
}

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

export function getEpicProgress(subTeam?: string): Promise<EpicProgressResponse> {
  const params = new URLSearchParams()
  if (subTeam) params.set('subTeam', subTeam)
  const query = params.toString()
  return apiFetch<EpicProgressResponse>(`/analytics/epic-progress${query ? `?${query}` : ''}`)
}

export function updateSprint(sprintId: number, req: UpdateSprintRequest): Promise<UpdateSprintResponse> {
  return apiFetch<UpdateSprintResponse>(`/sprints/${sprintId}`, {
    method: 'PUT',
    body: JSON.stringify(req)
  })
}

export function getCycleTime(sprintId?: number, last?: number, subTeam?: string): Promise<CycleTimeResponse> {
  const params = new URLSearchParams()
  if (sprintId !== undefined) params.set('sprintId', String(sprintId))
  if (last !== undefined) params.set('last', String(last))
  if (subTeam) params.set('subTeam', subTeam)
  const query = params.toString()
  return apiFetch<CycleTimeResponse>(`/analytics/cycle-time${query ? `?${query}` : ''}`)
}

export function getLeaderboard(sprintId?: number, last?: number, subTeam?: string): Promise<LeaderboardResponse> {
  const params = new URLSearchParams()
  if (sprintId !== undefined) params.set('sprintId', String(sprintId))
  if (last !== undefined) params.set('last', String(last))
  if (subTeam) params.set('subTeam', subTeam)
  const query = params.toString()
  return apiFetch<LeaderboardResponse>(`/analytics/leaderboard${query ? `?${query}` : ''}`)
}

export function getQaMetrics(sprintId: number, subTeam?: string): Promise<QaMetricsResponse> {
  const params = new URLSearchParams()
  if (subTeam) params.set('subTeam', subTeam)
  const query = params.toString()
  return apiFetch<QaMetricsResponse>(`/sprints/${sprintId}/qa-metrics${query ? `?${query}` : ''}`)
}

export function getUntestedTickets(sprintId: number, subTeam?: string): Promise<UntestedTicketsResponse> {
  const params = new URLSearchParams()
  if (subTeam) params.set('subTeam', subTeam)
  const query = params.toString()
  return apiFetch<UntestedTicketsResponse>(`/sprints/${sprintId}/qa-metrics/untested${query ? `?${query}` : ''}`)
}

export function getFailingTickets(sprintId: number, subTeam?: string): Promise<FailingTicketsResponse> {
  const params = new URLSearchParams()
  if (subTeam) params.set('subTeam', subTeam)
  const query = params.toString()
  return apiFetch<FailingTicketsResponse>(`/sprints/${sprintId}/qa-metrics/failing${query ? `?${query}` : ''}`)
}
