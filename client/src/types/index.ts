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

// Scope change types
export interface ScopeChangeSprintInfo {
  id: number
  name: string
  startDate: string
  endDate: string
}

export interface ScopeChangeSummaryMetrics {
  averageDisruptionRate: number
  averageDisruptionRateDelta: number | null
  averageDisruptionRateDeltaDirection: string | null
  averageNetScopeChange: number
  totalBugsAdded: number
}

export interface ScopeChangePerSprintData {
  sprintId: number
  committedSpActive: number
  committedSpTotal: number
  addedSp: number
  removedSp: number
  netScopeChange: number
  completedSp: number
  disruptionRate: number
  bugCount: number
}

export interface ClassificationEntry {
  category: string
  ticketCount: number
  spTotal: number | null
  percentage: number
}

export interface ScopeChangeMultiSprintResponse {
  sprints: ScopeChangeSprintInfo[]
  summaryMetrics: ScopeChangeSummaryMetrics
  perSprintData: ScopeChangePerSprintData[]
  classificationBreakdown: ClassificationEntry[]
}

export interface ScopeMetricCard {
  name: string
  value: number
  displayValue: string
  delta: number | null
  deltaDirection: string | null
  deltaPolarity: string | null
}

export interface ScopeChangeSingleSprintMetrics {
  committedSpActive: ScopeMetricCard
  committedSpTotal: ScopeMetricCard
  addedSp: ScopeMetricCard
  removedSp: ScopeMetricCard
  netScopeChange: ScopeMetricCard
  disruptionRate: ScopeMetricCard
  bugCount: ScopeMetricCard
}

export interface BurnupDataPoint {
  dayNumber: number
  date: string
  totalScopeSp: number
  completedSp: number
  phase: string
}

export interface ScopeChangeEvent {
  date: string
  sprintDayNumber: number
  ticketKey: string
  ticketSummary: string
  storyPoints: number | null
  issueType: string
  action: string
  category: string | null
  isExcluded: boolean
}

export interface BugTimeInProgress {
  ticketKey: string
  ticketSummary: string
  timeInActiveDays: number
  currentStatus: string
}

export interface ScopeChangeSingleSprintResponse {
  sprint: ScopeChangeSprintInfo
  metrics: ScopeChangeSingleSprintMetrics
  burnupData: BurnupDataPoint[]
  classificationBreakdown: ClassificationEntry[]
  events: ScopeChangeEvent[]
  bugTimeInProgress: BugTimeInProgress[]
}

export interface ScopeChangeResponse {
  mode: 'multi' | 'single'
  multiSprint: ScopeChangeMultiSprintResponse | null
  singleSprint: ScopeChangeSingleSprintResponse | null
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
