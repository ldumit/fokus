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
