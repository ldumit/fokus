// Sprint summary types
export interface SprintInfo {
  id: number
  name: string
  startDate: string
  endDate: string
  durationDays: number
  syncedAt: string
}

export interface HealthScoreResult {
  compositeScore: number
  compositeRag: string
  completionSubScore: number
  completionRag: string
  disruptionSubScore: number
  disruptionRag: string
  carryOverSubScore: number
  carryOverRag: string
}

export interface SparklinePoint {
  sprintName: string
  value: number
}

export interface MetricCard {
  name: string
  value: number
  displayValue: string
  delta: number | null
  deltaDirection: string | null
  deltaPolarity: string | null
  sparkline: SparklinePoint[]
}

export interface MetricsResult {
  spCompleted: MetricCard
  completionRate: MetricCard
  disruptionRate: MetricCard
  carryOverRate: MetricCard
}

export interface EpicProgress {
  epicName: string
  spCompletedThisSprint: number
  totalSp: number
  doneSp: number
  completionPercentage: number
}

export interface DeveloperSummary {
  displayName: string
  avatarUrl: string | null
  subTeam: string | null
  spCompleted: number
}

export interface ZombieTicket {
  ticketKey: string
  summary: string
  sprintCount: number
}

export interface MidSprintDisruption {
  totalSp: number
  ticketCount: number
}

export interface FlagsResult {
  zombieTickets: ZombieTicket[]
  midSprintDisruption: MidSprintDisruption | null
  zeroSpDevelopers: string[]
  hasAnyFlags: boolean
}

export interface SprintSummaryResponse {
  sprint: SprintInfo | null
  healthScore: HealthScoreResult | null
  metrics: MetricsResult | null
  topEpics: EpicProgress[]
  leaderboard: DeveloperSummary[]
  flags: FlagsResult
}

// Closed sprints list
export interface ClosedSprintItem {
  id: number
  name: string
  startDate: string
  endDate: string
  state: string
}

export interface HealthThresholdConfig {
  completionGreen: number
  completionAmber: number
  disruptionGreen: number
  disruptionAmber: number
  carryOverGreen: number
  carryOverAmber: number
}

export interface HealthWeightConfig {
  completion: number
  disruption: number
  carryOver: number
}

export interface AppSettings {
  boardId: number | null
  doneStatuses: string[]
  workflowStages: string[]
  healthThresholds: HealthThresholdConfig
  healthWeights: HealthWeightConfig
}

export type SprintState = 'Active' | 'Closed'

export interface DetectionConfidence {
  transitionCount: number
  ticketCount: number
  sprintCount: number
}

export interface DetectionResult {
  stages: string[]
  sidelined: string[]
  confidence: DetectionConfidence
}

export interface BoardOption {
  id: number
  name: string
  type: string
}

export interface StatusOption {
  name: string
  categoryKey: string
}

// Developer throughput types
export interface SprintSummaryItem {
  id: number
  name: string
  startDate: string
  endDate: string
}

export interface SprintBreakdown {
  sprintId: number
  spAssigned: number
  spCompleted: number
  completionPercent: number
  ticketsDone: number
  ticketsCarriedOver: number
  capacityPercent: number
  rollingAverageSpCompleted: number | null
  spAssignedDelta: number | null
  spCompletedDelta: number | null
  completionPercentDelta: number | null
  ticketsDoneDelta: number | null
  ticketsCarriedOverDelta: number | null
  spAssignedDeltaDirection: string | null
  spCompletedDeltaDirection: string | null
  completionPercentDeltaDirection: string | null
  ticketsDoneDeltaDirection: string | null
  ticketsCarriedOverDeltaDirection: string | null
  spAssignedDeltaPolarity: string | null
  spCompletedDeltaPolarity: string | null
  completionPercentDeltaPolarity: string | null
  ticketsDoneDeltaPolarity: string | null
  ticketsCarriedOverDeltaPolarity: string | null
}

export interface DeveloperThroughputEntry {
  accountId: string
  displayName: string
  subTeam: string | null
  avatarUrl: string | null
  sprintBreakdowns: SprintBreakdown[]
}

export interface DeveloperThroughputResponse {
  sprints: SprintSummaryItem[]
  developers: DeveloperThroughputEntry[]
}

// Capacity types
export interface CapacityEntry {
  sprintId: number
  capacityPercent: number
}

export interface SetCapacityRequest {
  sprintId: number
  capacityPercent: number
}

export interface SetCapacityResponse {
  developerAccountId: string
  sprintId: number
  capacityPercent: number
}

// Sync types
export interface JiraSprint {
  id: number
  name: string
  startDate: string | null
  state: string
}

export interface SyncSprintsResponse {
  sprintsAttempted: number
  sprintsSynced: number
  ticketsUpserted: number
  developersDiscovered: number
  failures: { sprintId: number; sprintName: string; error: string }[]
}

export interface SyncBacklogResponse {
  backlogSprintsSynced: number
  epicTicketsDiscovered: number
  sprintFailures: number
  epicFailures: string[]
}
