// Auth types
export interface AuthUser {
  id: number
  email: string
  displayName: string
  avatarUrl: string | null
  role: 'Admin' | 'Manager'
}

export interface UserEntry {
  id: number
  email: string
  displayName: string
  avatarUrl: string | null
  role: string
  lastLoginAt: string | null
  isActive: boolean
}

export interface InvitationEntry {
  id: number
  email: string
  role: string
  status: string
  createdAt: string
  expiresAt: string
}

export interface CreateInvitationRequest {
  email: string
  role?: string
}

export interface CreateInvitationResponse {
  id: number
  email: string
  role: string
  inviteLink: string
  expiresAt: string
}

export interface InviteValidation {
  valid: boolean
  email?: string
  error?: string
}

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
  scopeDisruptionRate: MetricCard
  bugDisruptionRate: MetricCard
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
  featureSp: number
  bugSp: number
  featureTickets: number
  bugTickets: number
  capacityPercent: number
}

export interface ZombieTicket {
  ticketKey: string
  summary: string
  sprintCount: number
  assigneeName: string | null
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
  goal?: string
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
  bugRatioAlertThreshold: number
  bugRatioConsecutiveSprintCount: number
  syncBackSprintCount: number
  planningWindowDays: number
  defaultSpPerBug: number
}

export type SprintState = 'Active' | 'Closed' | 'Future'

export interface Developer {
  accountId: string
  displayName: string
  avatarUrl: string | null
  subTeam: string | null
  isActive: boolean
}

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
  bugSp: number
  totalScopeTickets: number
  completedTickets: number
  bugTickets: number
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

// Carry-over types
export interface CarryOverSprintInfo {
  id: number
  name: string
  startDate: string
  endDate: string
}

export interface CarryOverSummaryMetrics {
  averageCarryOverRate: number
  averageCarryOverSp: number
  totalZombieTickets: number
}

export interface CarryOverStatusDistributionEntry {
  stageName: string
  ticketCount: number
  spTotal: number
  percentage: number
}

export interface CarryOverPerSprintData {
  sprintId: number
  carryOverSp: number
  carryOverTicketCount: number
  carryOverRate: number
  totalScopeSp: number
  statusDistribution: CarryOverStatusDistributionEntry[]
}

export interface CarryOverIssueTypeEntry {
  issueType: string
  ticketCount: number
  spTotal: number
  percentage: number
}

export interface CarryOverZombieSummary {
  ticketKey: string
  summary: string
  issueType: string
  currentStatus: string
  storyPoints: number | null
  sprintCount: number
}

export interface CarryOverMultiSprintResponse {
  sprints: CarryOverSprintInfo[]
  summaryMetrics: CarryOverSummaryMetrics
  perSprintData: CarryOverPerSprintData[]
  issueTypeBreakdown: CarryOverIssueTypeEntry[]
  zombieTickets: CarryOverZombieSummary[]
}

export interface CarryOverSingleSprintMetrics {
  carryOverRate: ScopeMetricCard
  carryOverSp: ScopeMetricCard
  carryOverTicketCount: ScopeMetricCard
}

export interface CarryOverDestinationBucket {
  count: number
  sp: number
}

export interface CarryOverDestination {
  priorSprintId: number
  priorSprintName: string
  priorCarryOverCount: number
  priorCarryOverSp: number
  completed: CarryOverDestinationBucket
  carriedAgain: CarryOverDestinationBucket
  removed: CarryOverDestinationBucket
  dropped: CarryOverDestinationBucket
}

export interface CarryOverTicketEntry {
  ticketKey: string
  summary: string
  issueType: string
  storyPoints: number | null
  finalStatus: string
  workflowStage: string
  sprintCount: number
  isZombie: boolean
  isExcluded: boolean
}

export interface ZombieTrajectorySprintEntry {
  sprintId: number
  sprintName: string
  finalStatus: string
}

export interface ZombieTrajectoryEntry {
  ticketKey: string
  summary: string
  issueType: string
  storyPoints: number | null
  currentStatus: string
  sprintCount: number
  sprints: ZombieTrajectorySprintEntry[]
}

export interface CarryOverSingleSprintResponse {
  sprint: CarryOverSprintInfo
  metrics: CarryOverSingleSprintMetrics
  statusDistribution: CarryOverStatusDistributionEntry[]
  issueTypeBreakdown: CarryOverIssueTypeEntry[]
  carryOverDestination: CarryOverDestination | null
  tickets: CarryOverTicketEntry[]
  zombieTrajectories: ZombieTrajectoryEntry[]
}

export interface CarryOverResponse {
  mode: 'multi' | 'single'
  multiSprint: CarryOverMultiSprintResponse | null
  singleSprint: CarryOverSingleSprintResponse | null
}

// Bug Ratio types
export interface BugRatioSprintInfo {
  id: number
  name: string
  startDate: string
  endDate: string
}

export interface BugRatioTeamTrendEntry {
  sprintId: number
  bugRatioPercent: number
  bugSp: number
  nonBugSp: number
  completedSp: number
}

export interface BugRatioTeamMetrics {
  bugRatioPercent: number
  totalBugSp: number
  totalNonBugSp: number
  totalCompletedSp: number
  perSprintTrend: BugRatioTeamTrendEntry[]
}

export interface BugRatioIssueTypeEntry {
  issueType: string
  ticketCount: number
  spTotal: number
}

export interface BugRatioAlertStatus {
  isActive: boolean
  consecutiveSprintCount: number
  thresholdPercent: number
}

export interface BugRatioDeveloperSprintBreakdown {
  sprintId: number
  bugSp: number
  nonBugSp: number
  completedSp: number
  bugRatioPercent: number
  bugTicketCount: number
  nonBugTicketCount: number
}

export interface BugRatioDeveloperEntry {
  accountId: string
  displayName: string
  subTeam: string | null
  avatarUrl: string | null
  bugSp: number
  nonBugSp: number
  completedSp: number
  bugRatioPercent: number
  bugTicketCount: number
  nonBugTicketCount: number
  sprintBreakdowns: BugRatioDeveloperSprintBreakdown[]
  alert: BugRatioAlertStatus
}

export interface BugRatioMultiSprintResponse {
  sprints: BugRatioSprintInfo[]
  teamMetrics: BugRatioTeamMetrics
  issueTypeBreakdown: BugRatioIssueTypeEntry[]
  developers: BugRatioDeveloperEntry[]
}

export interface BugRatioMetricCard {
  name: string
  value: number
  displayValue: string
  delta: number | null
  deltaDirection: string | null
  deltaPolarity: string | null
}

export interface BugRatioTeamSingleMetrics {
  bugRatioPercent: BugRatioMetricCard
  bugSp: BugRatioMetricCard
  nonBugSp: BugRatioMetricCard
}

export interface BugRatioDeveloperDelta {
  bugSpDelta: number
  bugSpDeltaDirection: string
  bugSpDeltaPolarity: string
  nonBugSpDelta: number
  nonBugSpDeltaDirection: string
  nonBugSpDeltaPolarity: string
  bugRatioPercentDelta: number
  bugRatioPercentDeltaDirection: string
  bugRatioPercentDeltaPolarity: string
  bugTicketCountDelta: number
  bugTicketCountDeltaDirection: string
  bugTicketCountDeltaPolarity: string
  nonBugTicketCountDelta: number
  nonBugTicketCountDeltaDirection: string
  nonBugTicketCountDeltaPolarity: string
}

export interface BugRatioDeveloperSingleEntry {
  accountId: string
  displayName: string
  subTeam: string | null
  avatarUrl: string | null
  bugSp: number
  nonBugSp: number
  completedSp: number
  bugRatioPercent: number
  bugTicketCount: number
  nonBugTicketCount: number
  delta: BugRatioDeveloperDelta | null
  alert: BugRatioAlertStatus
}

export interface BugRatioSingleSprintResponse {
  sprint: BugRatioSprintInfo
  teamMetrics: BugRatioTeamSingleMetrics
  issueTypeBreakdown: BugRatioIssueTypeEntry[]
  developers: BugRatioDeveloperSingleEntry[]
}

export interface BugRatioResponse {
  mode: 'multi' | 'single'
  multiSprint: BugRatioMultiSprintResponse | null
  singleSprint: BugRatioSingleSprintResponse | null
}

// Epic Progress types
export interface EpicProgressSummaryMetrics {
  activeEpicCount: number
  completedEpicCount: number
  averageCompletionPercentage: number
}

export interface EpicProgressTicketEntry {
  ticketKey: string
  summary: string
  issueType: string
  storyPoints: number | null
  currentStatus: string
  assigneeDisplayName: string | null
  isDone: boolean
}

export interface EpicProgressEntry {
  epicKey: string
  epicName: string
  totalTickets: number
  doneTickets: number
  remainingTickets: number
  ticketCompletionPercentage: number
  totalSp: number
  doneSp: number
  remainingSp: number
  imputedSp: number | null
  adjustedTotalSp: number | null
  spCompletionPercentage: number | null
  unestimatedTicketCount: number
  velocity: number | null
  projectedSprintsRemaining: number | null
  projectionConfidence: string | null
  activeSprintCount: number
  isCompleted: boolean
  tickets: EpicProgressTicketEntry[]
}

export interface EpicProgressUnlinkedWork {
  ticketCount: number
  totalSp: number
}

export interface EpicProgressResponse {
  summaryMetrics: EpicProgressSummaryMetrics
  epics: EpicProgressEntry[]
  unlinkedWork: EpicProgressUnlinkedWork
}

// Cycle Time types
export interface CycleTimeSprintInfo {
  id: number
  name: string
  startDate: string
  endDate: string
}

export interface CycleTimeMetricCard {
  name: string
  value: number
  displayValue: string
  delta: number | null
  deltaDirection: string | null
  deltaPolarity: string | null
}

export interface StageBreakdownEntry {
  stageName: string
  durationDays: number
}

export interface CycleTimeScatterPoint {
  ticketKey: string
  ticketSummary: string
  issueType: string
  cycleTimeDays: number
  completionDate: string
  stageBreakdown: StageBreakdownEntry[]
  reworkCount: number
}

export interface StageFunnelEntry {
  stageName: string
  averageDurationDays: number
  percentage: number
}

export interface CycleTimeIssueTypeEntry {
  issueType: string
  ticketCount: number
  medianCycleTime: number
  p85CycleTime: number
}

export interface CycleTimeDeveloperEntry {
  displayName: string
  avatarUrl: string | null
  subTeam: string | null
  ticketsCompleted: number
  medianCycleTime: number
  p85CycleTime: number
  dominantStage: string
}

export interface CycleTimeOutlierEntry {
  ticketKey: string
  ticketSummary: string
  issueType: string
  cycleTimeDays: number
  stageBreakdown: StageBreakdownEntry[]
  reworkCount: number
}

export interface CycleTimeBoundaries {
  startStage: string
  endStage: string
}

export interface CycleTimeSingleSprintResponse {
  sprint: CycleTimeSprintInfo | null
  metricCards: CycleTimeMetricCard[] | null
  scatterPlot: CycleTimeScatterPoint[]
  stageFunnel: StageFunnelEntry[]
  issueTypeBreakdown: CycleTimeIssueTypeEntry[]
  developerBreakdown: CycleTimeDeveloperEntry[]
  outliers: CycleTimeOutlierEntry[]
  boundaries: CycleTimeBoundaries
}

export interface CycleTimeTrendEntry {
  sprintId: number
  sprintName: string
  p85CycleTime: number
  medianCycleTime: number
  ticketsCompleted: number
}

export interface CycleTimeSprintSummaryEntry {
  sprintId: number
  sprintName: string
  startDate: string
  ticketsCompleted: number
  medianCycleTime: number
  p85CycleTime: number
  outlierCount: number
}

export interface CycleTimeMultiSprintResponse {
  sprints: CycleTimeSprintInfo[]
  metricCards: CycleTimeMetricCard[] | null
  trend: CycleTimeTrendEntry[]
  stageFunnel: StageFunnelEntry[]
  sprintSummaries: CycleTimeSprintSummaryEntry[]
  boundaries: CycleTimeBoundaries
}

export interface CycleTimeResponse {
  mode: 'single' | 'multi'
  singleSprint: CycleTimeSingleSprintResponse | null
  multiSprint: CycleTimeMultiSprintResponse | null
}

export interface CycleTimeBoundariesResponse {
  startStage: string
  endStage: string
  availableStages: string[]
  workflowStageCount: number
}

export interface SaveCycleTimeBoundariesResponse {
  startStage: string
  endStage: string
}

// Team types
export interface TeamDeveloperDto {
  accountId: string
  displayName: string
  avatarUrl: string | null
  subTeam: string | null
  role: string
  defaultCapacityPercent: number
  isActive: boolean
}

export interface TeamRosterResponse {
  developers: TeamDeveloperDto[]
  subTeams: string[]
}

export interface UpdateTeamConfigRequest {
  role?: string | null
  defaultCapacityPercent?: number | null
  subTeam?: string | null
  subTeamProvided?: boolean
  isActive?: boolean | null
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

// Leaderboard types
export interface LeaderboardSprintInfo {
  id: number
  name: string
  startDate: string
  endDate: string
}

export interface LeaderboardDeveloperSprintBreakdown {
  sprintId: number
  featureSp: number
  bugSp: number
  totalSp: number
  featureTickets: number
  bugTickets: number
  capacityPercent: number
}

export interface LeaderboardDeveloperEntry {
  accountId: string
  displayName: string
  subTeam: string | null
  avatarUrl: string | null
  featureSp: number
  bugSp: number
  totalSp: number
  featureTickets: number
  bugTickets: number
  totalTickets: number
  capacityPercent: number
  sprintBreakdowns: LeaderboardDeveloperSprintBreakdown[]
}

export interface LeaderboardMultiSprintResponse {
  sprints: LeaderboardSprintInfo[]
  developers: LeaderboardDeveloperEntry[]
}

export interface LeaderboardDeveloperDelta {
  featureSpDelta: number
  featureSpDeltaDirection: string
  featureSpDeltaPolarity: string
  bugSpDelta: number
  bugSpDeltaDirection: string
  bugSpDeltaPolarity: string
  totalSpDelta: number
  totalSpDeltaDirection: string
  totalSpDeltaPolarity: string
  featureTicketsDelta: number
  featureTicketsDeltaDirection: string
  featureTicketsDeltaPolarity: string
  bugTicketsDelta: number
  bugTicketsDeltaDirection: string
  bugTicketsDeltaPolarity: string
}

export interface LeaderboardDeveloperSingleEntry {
  accountId: string
  displayName: string
  subTeam: string | null
  avatarUrl: string | null
  featureSp: number
  bugSp: number
  totalSp: number
  featureTickets: number
  bugTickets: number
  totalTickets: number
  capacityPercent: number
  delta: LeaderboardDeveloperDelta | null
}

export interface LeaderboardSingleSprintResponse {
  sprint: LeaderboardSprintInfo
  developers: LeaderboardDeveloperSingleEntry[]
}

export interface LeaderboardResponse {
  mode: 'multi' | 'single'
  multiSprint: LeaderboardMultiSprintResponse | null
  singleSprint: LeaderboardSingleSprintResponse | null
}
