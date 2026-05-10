<script setup lang="ts">
import { onMounted, reactive, ref, computed } from 'vue'
import { useSettingsStore } from '../stores/settingsStore'
import { useAuthStore } from '../stores/authStore'
import { getBoards, getStatuses, getCycleTimeBoundaries, saveCycleTimeBoundaries, getExcludedStatuses, saveExcludedStatuses } from '../api/settings'
import { getJiraSprints, syncSprints, syncBacklog } from '../api/sync'
import { getUsers, getInvitations, createInvitation, updateUserRole, updateUserStatus, revokeInvitation } from '../api/auth'
import type { AppSettings, BoardOption, StatusOption, SyncSprintsResponse, SyncBacklogResponse, UserEntry, InvitationEntry, CreateInvitationResponse } from '../types'

const store = useSettingsStore()
const authStore = useAuthStore()

// Settings tabs
const activeSettingsTab = ref<'jira' | 'workflow' | 'health' | 'sync' | 'users'>('jira')

// Per-panel save state
const boardSaving = ref(false)
const boardSaved = ref(false)
const boardError = ref('')

const doneStatusesSaving = ref(false)
const doneStatusesSaved = ref(false)
const doneStatusesError = ref('')

const workflowStagesSaving = ref(false)
const workflowStagesSaved = ref(false)
const workflowStagesError = ref('')

const healthConfigSaving = ref(false)
const healthConfigSaved = ref(false)
const healthConfigError = ref('')

const bugRatioAlertsSaving = ref(false)
const bugRatioAlertsSaved = ref(false)
const bugRatioAlertsError = ref('')

const syncConfigSaving = ref(false)
const syncConfigSaved = ref(false)
const syncConfigError = ref('')

async function saveBoardPanel() {
  boardSaved.value = false
  boardError.value = ''
  boardSaving.value = true
  try {
    await store.saveBoardAction(form.boardId)
    if (!store.error) {
      boardSaved.value = true
      setTimeout(() => boardSaved.value = false, 3000)
    } else {
      boardError.value = store.error
    }
  } catch (e: any) {
    boardError.value = e.message || 'Failed to save board.'
  } finally {
    boardSaving.value = false
  }
}

async function saveDoneStatusesPanel() {
  doneStatusesSaved.value = false
  doneStatusesError.value = ''
  doneStatusesSaving.value = true
  try {
    await store.saveDoneStatusesAction([...form.doneStatuses])
    if (!store.error) {
      doneStatusesSaved.value = true
      setTimeout(() => doneStatusesSaved.value = false, 3000)
    } else {
      doneStatusesError.value = store.error
    }
  } catch (e: any) {
    doneStatusesError.value = e.message || 'Failed to save done statuses.'
  } finally {
    doneStatusesSaving.value = false
  }
}

async function saveWorkflowStagesPanel() {
  workflowStagesSaved.value = false
  workflowStagesError.value = ''
  workflowStagesSaving.value = true
  try {
    await store.saveWorkflowStagesAction([...form.workflowStages])
    if (!store.error) {
      store.clearDetection()
      workflowStagesSaved.value = true
      setTimeout(() => workflowStagesSaved.value = false, 3000)
    } else {
      workflowStagesError.value = store.error
    }
  } catch (e: any) {
    workflowStagesError.value = e.message || 'Failed to save workflow stages.'
  } finally {
    workflowStagesSaving.value = false
  }
}

async function saveHealthConfigPanel() {
  healthConfigSaved.value = false
  healthConfigError.value = ''
  healthConfigSaving.value = true
  try {
    await store.saveHealthConfigAction({ ...form.healthThresholds }, { ...form.healthWeights })
    if (!store.error) {
      healthConfigSaved.value = true
      setTimeout(() => healthConfigSaved.value = false, 3000)
    } else {
      healthConfigError.value = store.error
    }
  } catch (e: any) {
    healthConfigError.value = e.message || 'Failed to save health config.'
  } finally {
    healthConfigSaving.value = false
  }
}

async function saveBugRatioAlertsPanel() {
  bugRatioAlertsSaved.value = false
  bugRatioAlertsError.value = ''
  bugRatioAlertsSaving.value = true
  try {
    await store.saveBugRatioAlertsAction(form.bugRatioAlertThreshold, form.bugRatioConsecutiveSprintCount, form.defaultSpPerBug)
    if (!store.error) {
      bugRatioAlertsSaved.value = true
      setTimeout(() => bugRatioAlertsSaved.value = false, 3000)
    } else {
      bugRatioAlertsError.value = store.error
    }
  } catch (e: any) {
    bugRatioAlertsError.value = e.message || 'Failed to save bug ratio alerts.'
  } finally {
    bugRatioAlertsSaving.value = false
  }
}

async function saveSyncConfigPanel() {
  syncConfigSaved.value = false
  syncConfigError.value = ''
  syncConfigSaving.value = true
  try {
    await store.saveSyncConfigAction(form.syncBackSprintCount, form.planningWindowDays)
    if (!store.error) {
      syncConfigSaved.value = true
      setTimeout(() => syncConfigSaved.value = false, 3000)
    } else {
      syncConfigError.value = store.error
    }
  } catch (e: any) {
    syncConfigError.value = e.message || 'Failed to save sync config.'
  } finally {
    syncConfigSaving.value = false
  }
}

// User management state (Admin only)
const users = ref<UserEntry[]>([])
const invitations = ref<InvitationEntry[]>([])
const usersLoading = ref(false)
const inviteEmail = ref('')
const inviteRole = ref('Manager')
const inviting = ref(false)
const inviteResult = ref<CreateInvitationResponse | null>(null)
const inviteError = ref('')
const userMgmtError = ref('')

async function loadUserManagement() {
  usersLoading.value = true
  try {
    const [usersResult, invitationsResult] = await Promise.allSettled([getUsers(), getInvitations()])
    if (usersResult.status === 'fulfilled') users.value = usersResult.value
    if (invitationsResult.status === 'fulfilled') invitations.value = invitationsResult.value
  } finally {
    usersLoading.value = false
  }
}

async function sendInvite() {
  inviteError.value = ''
  inviteResult.value = null
  inviting.value = true
  try {
    inviteResult.value = await createInvitation({ email: inviteEmail.value.trim(), role: inviteRole.value })
    inviteEmail.value = ''
    await loadUserManagement()
  } catch (e: any) {
    inviteError.value = e.message || 'Failed to send invitation.'
  } finally {
    inviting.value = false
  }
}

async function changeUserRole(userId: number, role: string) {
  userMgmtError.value = ''
  try {
    const updated = await updateUserRole(userId, role)
    const idx = users.value.findIndex(u => u.id === userId)
    if (idx !== -1) users.value[idx] = updated
  } catch (e: any) {
    userMgmtError.value = e.message || 'Failed to update role.'
  }
}

async function toggleUserStatus(userId: number, isActive: boolean) {
  userMgmtError.value = ''
  try {
    const updated = await updateUserStatus(userId, isActive)
    const idx = users.value.findIndex(u => u.id === userId)
    if (idx !== -1) users.value[idx] = updated
  } catch (e: any) {
    userMgmtError.value = e.message || 'Failed to update status.'
  }
}

async function revokeInvite(inviteId: number) {
  userMgmtError.value = ''
  try {
    await revokeInvitation(inviteId)
    await loadUserManagement()
  } catch (e: any) {
    userMgmtError.value = e.message || 'Failed to revoke invitation.'
  }
}

function copyInviteLink(link: string) {
  navigator.clipboard.writeText(link)
}
const newStatus = ref('')
const newStage = ref('')

// Board dropdown state
const boards = ref<BoardOption[]>([])
const boardsLoading = ref(false)
const boardsError = ref(false)

// Status dropdown state
const statuses = ref<StatusOption[]>([])
const statusesLoading = ref(false)
const statusesError = ref(false)
const selectedStatus = ref('')

const form = reactive<AppSettings>({
  boardId: null,
  doneStatuses: [],
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
  },
  bugRatioAlertThreshold: 50,
  bugRatioConsecutiveSprintCount: 2,
  syncBackSprintCount: 20,
  planningWindowDays: 2,
  defaultSpPerBug: 3
})

// Excluded statuses state (GAP-1)
const excludedStatuses = ref<string[]>([])
const selectedExcludedStatus = ref('')
const excludedSaving = ref(false)
const excludedSaved = ref(false)
const excludedError = ref('')

const availableExcludedStatuses = computed<StatusOption[]>(() => {
  return statuses.value.filter(s => !excludedStatuses.value.includes(s.name))
})

function addExcludedStatus() {
  if (statuses.value.length > 0 && !statusesError.value) {
    const val = selectedExcludedStatus.value.trim()
    if (val && !excludedStatuses.value.includes(val)) {
      excludedStatuses.value.push(val)
      const next = availableExcludedStatuses.value.find(s => s.name !== val)
      selectedExcludedStatus.value = next?.name ?? ''
    }
  }
}

function removeExcludedStatus(index: number) {
  excludedStatuses.value.splice(index, 1)
}

async function saveExcluded() {
  excludedSaved.value = false
  excludedError.value = ''
  excludedSaving.value = true
  try {
    const result = await saveExcludedStatuses(excludedStatuses.value)
    excludedStatuses.value = result
    excludedSaved.value = true
    setTimeout(() => excludedSaved.value = false, 3000)
  } catch (e: any) {
    excludedError.value = e.message || 'Failed to save excluded statuses.'
  } finally {
    excludedSaving.value = false
  }
}

// Sprint range sync state (GAP-2)
const sprintsForRange = ref<{ id: number; name: string; startDate: string | null; state: string }[]>([])
const sprintsForRangeLoading = ref(false)
const sprintsForRangeError = ref('')
const fromSprintId = ref<number | null>(null)
const toSprintId = ref<number | null>(null)
const syncingRange = ref(false)
const syncRangeResult = ref<SyncSprintsResponse | null>(null)
const syncRangeError = ref('')

async function loadSprintsForRange() {
  if (!form.boardId) {
    sprintsForRangeError.value = 'Select a board first.'
    return
  }
  sprintsForRangeLoading.value = true
  sprintsForRangeError.value = ''
  try {
    const { sprints } = await getJiraSprints(form.boardId)
    sprintsForRange.value = [...sprints].sort((a, b) => {
      if (!a.startDate) return 1
      if (!b.startDate) return -1
      return a.startDate.localeCompare(b.startDate)
    })
    if (sprintsForRange.value.length > 0) {
      fromSprintId.value = sprintsForRange.value[0].id
      toSprintId.value = sprintsForRange.value[sprintsForRange.value.length - 1].id
    }
  } catch (e: any) {
    sprintsForRangeError.value = e.message || 'Failed to load sprints.'
  } finally {
    sprintsForRangeLoading.value = false
  }
}

async function syncRange() {
  if (!fromSprintId.value || !toSprintId.value) {
    syncRangeError.value = 'Select from and to sprints.'
    return
  }
  syncingRange.value = true
  syncRangeError.value = ''
  syncRangeResult.value = null
  try {
    syncRangeResult.value = await syncSprints(fromSprintId.value, toSprintId.value)
  } catch (e: any) {
    syncRangeError.value = e.message || 'Sync range failed.'
  } finally {
    syncingRange.value = false
  }
}


function syncFromStore() {
  const s = store.settings
  form.boardId = s.boardId
  form.doneStatuses = [...s.doneStatuses]
  form.workflowStages = [...s.workflowStages]
  form.healthThresholds = { ...s.healthThresholds }
  form.healthWeights = { ...s.healthWeights }
  form.bugRatioAlertThreshold = s.bugRatioAlertThreshold
  form.bugRatioConsecutiveSprintCount = s.bugRatioConsecutiveSprintCount
  form.syncBackSprintCount = s.syncBackSprintCount
  form.planningWindowDays = s.planningWindowDays
  form.defaultSpPerBug = s.defaultSpPerBug
}

// Statuses available to add (not already in doneStatuses), done category first
const availableStatuses = computed<StatusOption[]>(() => {
  const notAdded = statuses.value.filter(s => !form.doneStatuses.includes(s.name))
  const done = notAdded.filter(s => s.categoryKey === 'done')
  const rest = notAdded.filter(s => s.categoryKey !== 'done')
  return [...done, ...rest]
})

onMounted(async () => {
  // All fire in parallel — form is not blocked on dropdown data
  boardsLoading.value = true
  statusesLoading.value = true
  boundariesLoading.value = true

  const [, boardsResult, statusesResult, boundariesResult, excludedResult] = await Promise.allSettled([
    store.fetchSettings().then(() => {
      syncFromStore()
      if (form.workflowStages.length === 0) {
        return store.runDetectWorkflowStages().then(() => {
          if (store.detectionResult && store.detectionResult.stages.length > 0) {
            form.workflowStages = [...store.detectionResult.stages]
          }
        })
      }
    }),
    getBoards(),
    getStatuses(),
    getCycleTimeBoundaries(),
    getExcludedStatuses()
  ])

  boardsLoading.value = false
  statusesLoading.value = false
  boundariesLoading.value = false

  if (boardsResult.status === 'fulfilled') {
    boards.value = boardsResult.value.boards
  } else {
    boardsError.value = true
  }

  if (statusesResult.status === 'fulfilled') {
    statuses.value = statusesResult.value.statuses
    // Set default selection to first available
    if (availableStatuses.value.length > 0) {
      selectedStatus.value = availableStatuses.value[0].name
    }
    if (availableExcludedStatuses.value.length > 0) {
      selectedExcludedStatus.value = availableExcludedStatuses.value[0].name
    }
  } else {
    statusesError.value = true
  }

  if (boundariesResult.status === 'fulfilled') {
    boundaryStartStage.value = boundariesResult.value.startStage
    boundaryEndStage.value = boundariesResult.value.endStage
    boundaryAvailableStages.value = boundariesResult.value.availableStages
  }

  if (excludedResult.status === 'fulfilled') {
    excludedStatuses.value = excludedResult.value
  }

  if (authStore.isAdmin) {
    await loadUserManagement()
  }
})

async function reDetect() {
  await store.runDetectWorkflowStages()
  if (store.detectionResult) {
    form.workflowStages = [...store.detectionResult.stages]
  }
}

function moveSidelined(status: string) {
  store.moveSidelinedToStages(status)
  if (!form.workflowStages.includes(status)) {
    form.workflowStages.push(status)
  }
}

const reDetectDisabled = computed(() => {
  if (store.detecting) return true
  const r = store.detectionResult
  if (r !== null && r.confidence.transitionCount === 0 && r.confidence.ticketCount === 0 && r.confidence.sprintCount === 0) return true
  return false
})

function addStatus() {
  if (statuses.value.length > 0 && !statusesError.value) {
    // Dropdown mode
    const val = selectedStatus.value.trim()
    if (val && !form.doneStatuses.includes(val)) {
      form.doneStatuses.push(val)
      // Update selection to next available
      const next = availableStatuses.value.find(s => s.name !== val)
      selectedStatus.value = next?.name ?? ''
    }
  } else {
    // Fallback free-text mode
    const val = newStatus.value.trim()
    if (val && !form.doneStatuses.includes(val)) {
      form.doneStatuses.push(val)
    }
    newStatus.value = ''
  }
}

function removeStatus(index: number) {
  form.doneStatuses.splice(index, 1)
}

function addStage() {
  const val = newStage.value.trim()
  if (val && !form.workflowStages.includes(val)) {
    form.workflowStages.push(val)
  }
  newStage.value = ''
}

function removeStage(index: number) {
  form.workflowStages.splice(index, 1)
}

function moveStage(index: number, direction: -1 | 1) {
  const target = index + direction
  if (target < 0 || target >= form.workflowStages.length) return
  const temp = form.workflowStages[index]
  form.workflowStages[index] = form.workflowStages[target]
  form.workflowStages[target] = temp
}


const weightsSum = () => form.healthWeights.completion + form.healthWeights.disruption + form.healthWeights.carryOver
const isReadOnly = computed(() => !authStore.isAdmin)

// Cycle time boundary state
const boundaryStartStage = ref('')
const boundaryEndStage = ref('')
const boundaryAvailableStages = ref<string[]>([])
const boundariesLoading = ref(false)
const boundariesSaving = ref(false)
const boundariesSaved = ref(false)
const boundariesError = ref('')

async function saveBoundaries() {
  boundariesSaved.value = false
  boundariesError.value = ''
  boundariesSaving.value = true
  try {
    const result = await saveCycleTimeBoundaries(boundaryStartStage.value, boundaryEndStage.value)
    boundaryStartStage.value = result.startStage
    boundaryEndStage.value = result.endStage
    boundariesSaved.value = true
    setTimeout(() => boundariesSaved.value = false, 3000)
  } catch (e: any) {
    boundariesError.value = e.message || 'Failed to save boundaries.'
  } finally {
    boundariesSaving.value = false
  }
}

// Sync state
const syncing = ref(false)
const syncResult = ref<{ sprints: SyncSprintsResponse; backlog: SyncBacklogResponse } | null>(null)
const syncError = ref('')

async function syncAll() {
  if (!form.boardId) {
    syncError.value = 'Select a board and save settings first.'
    return
  }

  syncing.value = true
  syncError.value = ''
  syncResult.value = null

  try {
    // Save sync config and board before syncing to ensure values are persisted
    await store.saveSyncConfigAction(form.syncBackSprintCount, form.planningWindowDays)
    await store.saveBoardAction(form.boardId)
    if (store.error) {
      syncError.value = 'Failed to save settings before sync.'
      syncing.value = false
      return
    }

    const { sprints: jiraSprints } = await getJiraSprints(form.boardId)
    if (jiraSprints.length === 0) {
      syncError.value = 'No sprints found on this board.'
      syncing.value = false
      return
    }

    const lastN = jiraSprints.slice(-form.syncBackSprintCount)
    const first = lastN[0]
    const last = lastN[lastN.length - 1]

    const sprintResult = await syncSprints(first.id, last.id)
    const backlogResult = await syncBacklog()

    syncResult.value = { sprints: sprintResult, backlog: backlogResult }
  } catch (e: any) {
    syncError.value = e.message || 'Sync failed.'
  } finally {
    syncing.value = false
  }
}
</script>

<template>
  <div class="max-w-3xl mx-auto space-y-6">
      <h1 class="text-3xl font-bold text-gray-100">Settings</h1>

      <!-- Manager read-only banner -->
      <div
        v-if="authStore.user?.role === 'Manager'"
        class="rounded-md bg-amber-500/10 border border-amber-500/30 px-4 py-3 text-sm text-amber-400"
      >
        View only — contact an Admin to make changes.
      </div>

      <div v-if="store.loading" class="text-gray-400">Loading settings...</div>

      <template v-else>
        <!-- Tab bar -->
        <div class="flex gap-1 border-b border-border-default mb-6">
          <button
            :class="['px-4 py-2 text-sm font-medium border-b-2 -mb-px transition-colors', activeSettingsTab === 'jira' ? 'border-accent-default text-accent-default' : 'border-transparent text-text-secondary hover:text-text-primary']"
            @click="activeSettingsTab = 'jira'"
          >Jira</button>
          <button
            :class="['px-4 py-2 text-sm font-medium border-b-2 -mb-px transition-colors', activeSettingsTab === 'workflow' ? 'border-accent-default text-accent-default' : 'border-transparent text-text-secondary hover:text-text-primary']"
            @click="activeSettingsTab = 'workflow'"
          >Workflow</button>
          <button
            :class="['px-4 py-2 text-sm font-medium border-b-2 -mb-px transition-colors', activeSettingsTab === 'health' ? 'border-accent-default text-accent-default' : 'border-transparent text-text-secondary hover:text-text-primary']"
            @click="activeSettingsTab = 'health'"
          >Health</button>
          <button
            :class="['px-4 py-2 text-sm font-medium border-b-2 -mb-px transition-colors', activeSettingsTab === 'sync' ? 'border-accent-default text-accent-default' : 'border-transparent text-text-secondary hover:text-text-primary']"
            @click="activeSettingsTab = 'sync'"
          >Sync</button>
          <button
            v-if="authStore.isAdmin"
            :class="['px-4 py-2 text-sm font-medium border-b-2 -mb-px transition-colors', activeSettingsTab === 'users' ? 'border-accent-default text-accent-default' : 'border-transparent text-text-secondary hover:text-text-primary']"
            @click="activeSettingsTab = 'users'"
          >Users</button>
        </div>

        <!-- Jira tab -->
        <template v-if="activeSettingsTab === 'jira'">

        <!-- Jira Board -->
        <section class="bg-gray-900 rounded-lg p-6 space-y-4">
          <div class="flex items-center gap-1">
            <h2 class="text-lg font-semibold text-gray-200">Jira Board</h2>
            <span
              class="text-gray-500 cursor-help"
              title="Link Fokus to your Jira instance using your email and an API token."
            >
              <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 20 20" fill="currentColor" class="w-3.5 h-3.5">
                <path fill-rule="evenodd" d="M18 10a8 8 0 11-16 0 8 8 0 0116 0zm-7-4a1 1 0 11-2 0 1 1 0 012 0zM9 9a.75.75 0 000 1.5h.253a.25.25 0 01.244.304l-.459 2.066A1.75 1.75 0 0010.747 15H11a.75.75 0 000-1.5h-.253a.25.25 0 01-.244-.304l.459-2.066A1.75 1.75 0 009.253 9H9z" clip-rule="evenodd" />
              </svg>
            </span>
          </div>
          <div>
            <label class="block text-sm text-gray-400 mb-1 cursor-help" title="Choose which Jira board to sync sprints from. Only one board is supported.">Board</label>
            <!-- Dropdown when boards loaded successfully -->
            <template v-if="!boardsError && !boardsLoading && boards.length > 0">
              <select
                v-model.number="form.boardId"
                :disabled="isReadOnly"
                class="w-full bg-gray-800 border border-gray-700 rounded px-3 py-2 text-gray-100 focus:outline-none focus:border-blue-500 disabled:opacity-50 disabled:cursor-not-allowed"
              >
                <option :value="null">-- Select a board --</option>
                <option v-for="board in boards" :key="board.id" :value="board.id">
                  {{ board.name }} ({{ board.type }})
                </option>
              </select>
            </template>
            <!-- Loading state -->
            <template v-else-if="boardsLoading">
              <input
                v-model.number="form.boardId"
                type="number"
                placeholder="Loading boards..."
                disabled
                class="w-full bg-gray-800 border border-gray-700 rounded px-3 py-2 text-gray-500 placeholder-gray-600 focus:outline-none cursor-not-allowed"
              />
            </template>
            <!-- Fallback: numeric input on error or no boards -->
            <template v-else>
              <input
                v-model.number="form.boardId"
                type="number"
                placeholder="Enter Jira board ID"
                :disabled="isReadOnly"
                class="w-full bg-gray-800 border border-gray-700 rounded px-3 py-2 text-gray-100 placeholder-gray-500 focus:outline-none focus:border-blue-500 disabled:opacity-50 disabled:cursor-not-allowed"
              />
              <p v-if="boardsError" class="text-xs text-amber-400 mt-1">Could not load boards from Jira.</p>
            </template>
          </div>
          <div class="flex items-center gap-4">
            <button
              @click="saveBoardPanel"
              :disabled="boardSaving || isReadOnly"
              class="bg-blue-600 hover:bg-blue-700 disabled:opacity-50 disabled:cursor-not-allowed text-white px-4 py-2 rounded font-medium text-sm"
            >
              {{ boardSaving ? 'Saving...' : 'Save Board' }}
            </button>
            <span v-if="boardSaved" class="text-green-400 text-sm">Saved.</span>
            <span v-if="boardError" class="text-red-400 text-sm">{{ boardError }}</span>
          </div>
        </section>

        <!-- Done Statuses -->
        <section class="bg-gray-900 rounded-lg p-6 space-y-4">
          <h2 class="text-lg font-semibold text-gray-200">Done Statuses</h2>
          <p class="text-sm text-gray-400">Status names that count as "done" when computing metrics.</p>
          <div class="flex flex-wrap gap-2">
            <span
              v-for="(status, i) in form.doneStatuses"
              :key="status"
              class="inline-flex items-center gap-1 bg-gray-800 text-gray-200 px-3 py-1 rounded-full text-sm"
            >
              {{ status }}
              <button @click="removeStatus(i)" :disabled="isReadOnly" class="text-gray-500 hover:text-red-400 ml-1 disabled:opacity-50 disabled:cursor-not-allowed">&times;</button>
            </span>
          </div>
          <!-- Dropdown mode -->
          <template v-if="!statusesError && !statusesLoading && statuses.length > 0">
            <div class="flex gap-2">
              <select
                v-model="selectedStatus"
                :disabled="isReadOnly"
                class="flex-1 bg-gray-800 border border-gray-700 rounded px-3 py-2 text-gray-100 focus:outline-none focus:border-blue-500 disabled:opacity-50 disabled:cursor-not-allowed"
              >
                <option value="" disabled>Select a status...</option>
                <option
                  v-for="s in availableStatuses"
                  :key="s.name"
                  :value="s.name"
                >
                  {{ s.name }}<template v-if="s.categoryKey === 'done'"> *</template>
                </option>
              </select>
              <button
                @click="addStatus"
                :disabled="!selectedStatus || isReadOnly"
                class="bg-blue-600 hover:bg-blue-700 disabled:opacity-50 disabled:cursor-not-allowed text-white px-4 py-2 rounded"
              >Add</button>
            </div>
            <p class="text-xs text-gray-500">* Statuses marked with * are in the "done" category.</p>
          </template>
          <!-- Loading state -->
          <template v-else-if="statusesLoading">
            <div class="flex gap-2">
              <input
                placeholder="Loading statuses..."
                disabled
                class="flex-1 bg-gray-800 border border-gray-700 rounded px-3 py-2 text-gray-500 placeholder-gray-600 focus:outline-none cursor-not-allowed"
              />
              <button disabled class="bg-blue-600 opacity-50 cursor-not-allowed text-white px-4 py-2 rounded">Add</button>
            </div>
          </template>
          <!-- Fallback: free-text input on error -->
          <template v-else>
            <div class="flex gap-2">
              <input
                v-model="newStatus"
                @keyup.enter="addStatus"
                placeholder="Add status..."
                :disabled="isReadOnly"
                class="flex-1 bg-gray-800 border border-gray-700 rounded px-3 py-2 text-gray-100 placeholder-gray-500 focus:outline-none focus:border-blue-500 disabled:opacity-50 disabled:cursor-not-allowed"
              />
              <button @click="addStatus" :disabled="isReadOnly" class="bg-blue-600 hover:bg-blue-700 disabled:opacity-50 disabled:cursor-not-allowed text-white px-4 py-2 rounded">Add</button>
            </div>
            <p v-if="statusesError" class="text-xs text-amber-400 mt-1">Could not load statuses from Jira.</p>
          </template>
          <div class="flex items-center gap-4">
            <button
              @click="saveDoneStatusesPanel"
              :disabled="doneStatusesSaving || isReadOnly"
              class="bg-blue-600 hover:bg-blue-700 disabled:opacity-50 disabled:cursor-not-allowed text-white px-4 py-2 rounded font-medium text-sm"
            >
              {{ doneStatusesSaving ? 'Saving...' : 'Save Done Statuses' }}
            </button>
            <span v-if="doneStatusesSaved" class="text-green-400 text-sm">Saved.</span>
            <span v-if="doneStatusesError" class="text-red-400 text-sm">{{ doneStatusesError }}</span>
          </div>
        </section>

        <!-- Excluded From Scope Statuses (GAP-1) -->
        <section class="bg-gray-900 rounded-lg p-6 space-y-4">
          <h2 class="text-lg font-semibold text-gray-200">Excluded From Scope Statuses</h2>
          <p class="text-sm text-gray-400">Tickets in these statuses are excluded from committed scope when computing metrics. Useful for parked or blocked statuses that shouldn't count as active work.</p>
          <div class="flex flex-wrap gap-2">
            <span
              v-for="(status, i) in excludedStatuses"
              :key="status"
              class="inline-flex items-center gap-1 bg-gray-800 text-gray-200 px-3 py-1 rounded-full text-sm"
            >
              {{ status }}
              <button @click="removeExcludedStatus(i)" :disabled="isReadOnly" class="text-gray-500 hover:text-red-400 ml-1 disabled:opacity-50 disabled:cursor-not-allowed">&times;</button>
            </span>
          </div>
          <!-- Dropdown mode -->
          <template v-if="!statusesError && !statusesLoading && statuses.length > 0">
            <div class="flex gap-2">
              <select
                v-model="selectedExcludedStatus"
                :disabled="isReadOnly"
                class="flex-1 bg-gray-800 border border-gray-700 rounded px-3 py-2 text-gray-100 focus:outline-none focus:border-blue-500 disabled:opacity-50 disabled:cursor-not-allowed"
              >
                <option value="" disabled>Select a status...</option>
                <option
                  v-for="s in availableExcludedStatuses"
                  :key="s.name"
                  :value="s.name"
                >{{ s.name }}</option>
              </select>
              <button
                @click="addExcludedStatus"
                :disabled="!selectedExcludedStatus || isReadOnly"
                class="bg-blue-600 hover:bg-blue-700 disabled:opacity-50 disabled:cursor-not-allowed text-white px-4 py-2 rounded"
              >Add</button>
            </div>
          </template>
          <!-- Loading state -->
          <template v-else-if="statusesLoading">
            <div class="flex gap-2">
              <input
                placeholder="Loading statuses..."
                disabled
                class="flex-1 bg-gray-800 border border-gray-700 rounded px-3 py-2 text-gray-500 placeholder-gray-600 focus:outline-none cursor-not-allowed"
              />
              <button disabled class="bg-blue-600 opacity-50 cursor-not-allowed text-white px-4 py-2 rounded">Add</button>
            </div>
          </template>
          <!-- Fallback: no statuses loaded -->
          <template v-else>
            <p class="text-xs text-gray-500">Load statuses from Jira to add excluded statuses.</p>
          </template>
          <div class="flex items-center gap-4">
            <button
              @click="saveExcluded"
              :disabled="excludedSaving || isReadOnly"
              class="bg-blue-600 hover:bg-blue-700 disabled:opacity-50 disabled:cursor-not-allowed text-white px-4 py-2 rounded font-medium text-sm"
            >
              {{ excludedSaving ? 'Saving...' : 'Save Excluded Statuses' }}
            </button>
            <span v-if="excludedSaved" class="text-green-400 text-sm">Saved.</span>
            <span v-if="excludedError" class="text-red-400 text-sm">{{ excludedError }}</span>
          </div>
        </section>

        </template><!-- end Jira tab -->

        <!-- Workflow tab -->
        <template v-if="activeSettingsTab === 'workflow'">

        <!-- Workflow Stages -->
        <section class="bg-gray-900 rounded-lg p-6 space-y-4">
          <div class="flex items-center justify-between">
            <div class="flex items-center gap-1">
              <h2 class="text-lg font-semibold text-gray-200">Workflow Stages</h2>
              <span
                class="text-gray-500 cursor-help"
                title="The ordered list of status phases your team's tickets move through from start to done."
              >
                <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 20 20" fill="currentColor" class="w-3.5 h-3.5">
                  <path fill-rule="evenodd" d="M18 10a8 8 0 11-16 0 8 8 0 0116 0zm-7-4a1 1 0 11-2 0 1 1 0 012 0zM9 9a.75.75 0 000 1.5h.253a.25.25 0 01.244.304l-.459 2.066A1.75 1.75 0 0010.747 15H11a.75.75 0 000-1.5h-.253a.25.25 0 01-.244-.304l.459-2.066A1.75 1.75 0 009.253 9H9z" clip-rule="evenodd" />
                </svg>
              </span>
            </div>
            <button
              @click="reDetect"
              :disabled="reDetectDisabled || isReadOnly"
              class="border border-gray-600 hover:border-gray-400 text-gray-300 hover:text-gray-100 disabled:opacity-40 disabled:cursor-not-allowed px-3 py-1 rounded text-sm"
              title="Runs a fresh detection using current sync data, replacing the displayed proposal."
            >
              {{ store.detecting ? 'Detecting...' : 'Re-detect' }}
            </button>
          </div>
          <p class="text-sm text-gray-400">Ordered workflow stages for cycle time computation. Will be auto-detected on first sync if left empty.</p>

          <!-- Confidence summary -->
          <p
            v-if="store.detectionResult !== null"
            class="text-sm text-gray-400"
            title="Shows how much data backs the detection: transitions analyzed, tickets covered, sprints spanned."
          >
            Based on {{ store.detectionResult.confidence.transitionCount }} transitions from {{ store.detectionResult.confidence.ticketCount }} tickets across {{ store.detectionResult.confidence.sprintCount }} sprints.
          </p>

          <!-- Empty state: no stages and no detection result (no data) -->
          <p
            v-if="form.workflowStages.length === 0 && store.detectionResult === null && !store.detecting"
            class="text-sm text-gray-500 italic"
          >
            Sync sprints to enable workflow detection.
          </p>

          <div class="space-y-2">
            <div
              v-for="(stage, i) in form.workflowStages"
              :key="i"
              class="flex items-center gap-2 bg-gray-800 rounded px-3 py-2"
            >
              <span class="text-gray-400 text-sm w-6">{{ i + 1 }}.</span>
              <span class="flex-1 text-gray-200">{{ stage }}</span>
              <button @click="moveStage(i, -1)" :disabled="i === 0 || isReadOnly" class="text-gray-500 hover:text-gray-300 disabled:opacity-30">&uarr;</button>
              <button @click="moveStage(i, 1)" :disabled="i === form.workflowStages.length - 1 || isReadOnly" class="text-gray-500 hover:text-gray-300 disabled:opacity-30">&darr;</button>
              <button @click="removeStage(i)" :disabled="isReadOnly" class="text-gray-500 hover:text-red-400 disabled:opacity-50 disabled:cursor-not-allowed">&times;</button>
            </div>
          </div>
          <div class="flex gap-2">
            <input
              v-model="newStage"
              @keyup.enter="addStage"
              placeholder="Add stage..."
              :disabled="isReadOnly"
              class="flex-1 bg-gray-800 border border-gray-700 rounded px-3 py-2 text-gray-100 placeholder-gray-500 focus:outline-none focus:border-blue-500 disabled:opacity-50 disabled:cursor-not-allowed"
              title="Type workflow stages by hand if you already know your pipeline or prefer not to use detection."
            />
            <button @click="addStage" :disabled="isReadOnly" class="bg-blue-600 hover:bg-blue-700 disabled:opacity-50 disabled:cursor-not-allowed text-white px-4 py-2 rounded">Add</button>
          </div>

          <!-- Other statuses (sidelined) -->
          <div
            v-if="store.detectionResult !== null && store.detectionResult.sidelined.length > 0"
            class="space-y-2 pt-2 border-t border-gray-800"
          >
            <p class="text-sm text-gray-400 font-medium cursor-help" title="Statuses found in your data but excluded from the main pipeline — add them manually if needed.">Other statuses</p>
            <div class="flex flex-wrap gap-2">
              <span
                v-for="status in store.detectionResult.sidelined"
                :key="status"
                class="inline-flex items-center gap-1 bg-gray-800 text-gray-300 px-3 py-1 rounded-full text-sm"
              >
                {{ status }}
                <button @click="moveSidelined(status)" :disabled="isReadOnly" class="text-gray-500 hover:text-green-400 ml-1 disabled:opacity-50 disabled:cursor-not-allowed" title="Add to stages">+</button>
              </span>
            </div>
          </div>
          <div class="flex items-center gap-4">
            <button
              @click="saveWorkflowStagesPanel"
              :disabled="workflowStagesSaving || isReadOnly"
              class="bg-blue-600 hover:bg-blue-700 disabled:opacity-50 disabled:cursor-not-allowed text-white px-4 py-2 rounded font-medium text-sm"
            >
              {{ workflowStagesSaving ? 'Saving...' : 'Save Workflow Stages' }}
            </button>
            <span v-if="workflowStagesSaved" class="text-green-400 text-sm">Saved.</span>
            <span v-if="workflowStagesError" class="text-red-400 text-sm">{{ workflowStagesError }}</span>
          </div>
        </section>

        <!-- Cycle Time Boundaries -->
        <section class="bg-gray-900 rounded-lg p-6 space-y-4">
          <h2 class="text-lg font-semibold text-gray-200">Cycle Time Boundaries</h2>
          <p class="text-sm text-gray-400">Configure which workflow stages mark the start and end of cycle time measurement.</p>

          <div v-if="boundariesLoading" class="text-gray-400 text-sm">Loading boundaries...</div>

          <template v-else>
            <div class="grid grid-cols-2 gap-4">
              <div>
                <!-- Cycle Starts At tooltip: Which workflow stage starts the cycle time clock. Default: first active work stage. -->
                <label class="block text-sm text-gray-300 mb-1 cursor-help" title="Which workflow stage starts the cycle time clock. Default: first active work stage.">
                  Cycle Starts At
                </label>
                <select
                  v-model="boundaryStartStage"
                  class="w-full bg-gray-800 border border-gray-700 rounded px-3 py-2 text-gray-100 focus:outline-none focus:border-blue-500 disabled:opacity-50 disabled:cursor-not-allowed"
                  :disabled="boundaryAvailableStages.length === 0 || isReadOnly"
                >
                  <option v-if="boundaryAvailableStages.length === 0" value="">No stages configured</option>
                  <option
                    v-for="stage in boundaryAvailableStages"
                    :key="stage"
                    :value="stage"
                  >{{ stage }}</option>
                </select>
              </div>
              <div>
                <!-- Cycle Ends At tooltip: Which workflow stage stops the cycle time clock. Default: first done status. -->
                <label class="block text-sm text-gray-300 mb-1 cursor-help" title="Which workflow stage stops the cycle time clock. Default: first done status.">
                  Cycle Ends At
                </label>
                <select
                  v-model="boundaryEndStage"
                  class="w-full bg-gray-800 border border-gray-700 rounded px-3 py-2 text-gray-100 focus:outline-none focus:border-blue-500 disabled:opacity-50 disabled:cursor-not-allowed"
                  :disabled="boundaryAvailableStages.length === 0 || isReadOnly"
                >
                  <option v-if="boundaryAvailableStages.length === 0" value="">No stages configured</option>
                  <option
                    v-for="stage in boundaryAvailableStages"
                    :key="stage"
                    :value="stage"
                  >{{ stage }}</option>
                </select>
              </div>
            </div>

            <div class="flex items-center gap-4">
              <button
                @click="saveBoundaries"
                :disabled="boundariesSaving || !boundaryStartStage || !boundaryEndStage || isReadOnly"
                class="bg-blue-600 hover:bg-blue-700 disabled:opacity-50 disabled:cursor-not-allowed text-white px-4 py-2 rounded font-medium text-sm"
              >
                {{ boundariesSaving ? 'Saving...' : 'Save Boundaries' }}
              </button>
              <span v-if="boundariesSaved" class="text-green-400 text-sm">Boundaries saved.</span>
              <span v-if="boundariesError" class="text-red-400 text-sm">{{ boundariesError }}</span>
            </div>
          </template>
        </section>

        </template><!-- end Workflow tab -->

        <!-- Health tab -->
        <template v-if="activeSettingsTab === 'health'">

        <!-- Health Thresholds -->
        <section class="bg-gray-900 rounded-lg p-6 space-y-4">
          <h2 class="text-lg font-semibold text-gray-200">Health Thresholds</h2>
          <p class="text-sm text-gray-400">Green/amber boundaries for health score. Values above green are green, between amber and green are amber, below amber are red.</p>

          <div class="grid grid-cols-3 gap-4">
            <div>
              <h3 class="text-sm font-medium text-gray-300 mb-2">Completion Rate (%)</h3>
              <div class="space-y-2">
                <div>
                  <label class="text-xs text-green-400">Green &ge;</label>
                  <input v-model.number="form.healthThresholds.completionGreen" type="number" min="0" max="100" :disabled="isReadOnly" class="w-full bg-gray-800 border border-gray-700 rounded px-2 py-1 text-gray-100 text-sm focus:outline-none focus:border-blue-500 disabled:opacity-50 disabled:cursor-not-allowed" />
                </div>
                <div>
                  <label class="text-xs text-amber-400">Amber &ge;</label>
                  <input v-model.number="form.healthThresholds.completionAmber" type="number" min="0" max="100" :disabled="isReadOnly" class="w-full bg-gray-800 border border-gray-700 rounded px-2 py-1 text-gray-100 text-sm focus:outline-none focus:border-blue-500 disabled:opacity-50 disabled:cursor-not-allowed" />
                </div>
              </div>
            </div>

            <div>
              <h3 class="text-sm font-medium text-gray-300 mb-2">Disruption Rate (%)</h3>
              <div class="space-y-2">
                <div>
                  <label class="text-xs text-green-400">Green &le;</label>
                  <input v-model.number="form.healthThresholds.disruptionGreen" type="number" min="0" max="100" :disabled="isReadOnly" class="w-full bg-gray-800 border border-gray-700 rounded px-2 py-1 text-gray-100 text-sm focus:outline-none focus:border-blue-500 disabled:opacity-50 disabled:cursor-not-allowed" />
                </div>
                <div>
                  <label class="text-xs text-amber-400">Amber &le;</label>
                  <input v-model.number="form.healthThresholds.disruptionAmber" type="number" min="0" max="100" :disabled="isReadOnly" class="w-full bg-gray-800 border border-gray-700 rounded px-2 py-1 text-gray-100 text-sm focus:outline-none focus:border-blue-500 disabled:opacity-50 disabled:cursor-not-allowed" />
                </div>
              </div>
            </div>

            <div>
              <h3 class="text-sm font-medium text-gray-300 mb-2">Carry-Over Rate (%)</h3>
              <div class="space-y-2">
                <div>
                  <label class="text-xs text-green-400">Green &le;</label>
                  <input v-model.number="form.healthThresholds.carryOverGreen" type="number" min="0" max="100" :disabled="isReadOnly" class="w-full bg-gray-800 border border-gray-700 rounded px-2 py-1 text-gray-100 text-sm focus:outline-none focus:border-blue-500 disabled:opacity-50 disabled:cursor-not-allowed" />
                </div>
                <div>
                  <label class="text-xs text-amber-400">Amber &le;</label>
                  <input v-model.number="form.healthThresholds.carryOverAmber" type="number" min="0" max="100" :disabled="isReadOnly" class="w-full bg-gray-800 border border-gray-700 rounded px-2 py-1 text-gray-100 text-sm focus:outline-none focus:border-blue-500 disabled:opacity-50 disabled:cursor-not-allowed" />
                </div>
              </div>
            </div>
          </div>
        </section>

        <!-- Health Weights -->
        <section class="bg-gray-900 rounded-lg p-6 space-y-4">
          <h2 class="text-lg font-semibold text-gray-200">Health Weights</h2>
          <p class="text-sm text-gray-400">Relative weights for composite health score. Must sum to 100.</p>
          <div class="grid grid-cols-3 gap-4">
            <div>
              <label class="block text-sm text-gray-300 mb-1">Completion</label>
              <input v-model.number="form.healthWeights.completion" type="number" min="0" max="100" :disabled="isReadOnly" class="w-full bg-gray-800 border border-gray-700 rounded px-3 py-2 text-gray-100 focus:outline-none focus:border-blue-500 disabled:opacity-50 disabled:cursor-not-allowed" />
            </div>
            <div>
              <label class="block text-sm text-gray-300 mb-1">Disruption</label>
              <input v-model.number="form.healthWeights.disruption" type="number" min="0" max="100" :disabled="isReadOnly" class="w-full bg-gray-800 border border-gray-700 rounded px-3 py-2 text-gray-100 focus:outline-none focus:border-blue-500 disabled:opacity-50 disabled:cursor-not-allowed" />
            </div>
            <div>
              <label class="block text-sm text-gray-300 mb-1">Carry-Over</label>
              <input v-model.number="form.healthWeights.carryOver" type="number" min="0" max="100" :disabled="isReadOnly" class="w-full bg-gray-800 border border-gray-700 rounded px-3 py-2 text-gray-100 focus:outline-none focus:border-blue-500 disabled:opacity-50 disabled:cursor-not-allowed" />
            </div>
          </div>
          <div class="text-sm" :class="weightsSum() === 100 ? 'text-green-400' : 'text-red-400'">
            Sum: {{ weightsSum() }} / 100
          </div>
          <div class="flex items-center gap-4">
            <button
              @click="saveHealthConfigPanel"
              :disabled="healthConfigSaving || weightsSum() !== 100 || isReadOnly"
              class="bg-blue-600 hover:bg-blue-700 disabled:opacity-50 disabled:cursor-not-allowed text-white px-4 py-2 rounded font-medium text-sm"
            >
              {{ healthConfigSaving ? 'Saving...' : 'Save Health Config' }}
            </button>
            <span v-if="healthConfigSaved" class="text-green-400 text-sm">Saved.</span>
            <span v-if="healthConfigError" class="text-red-400 text-sm">{{ healthConfigError }}</span>
          </div>
        </section>

        <!-- Bug Ratio Alerts -->
        <section class="bg-gray-900 rounded-lg p-6 space-y-4">
          <div class="flex items-center gap-1">
            <h2 class="text-lg font-semibold text-gray-200">Bug Ratio Alerts</h2>
            <span
              class="text-gray-500 cursor-help"
              title="Configure when the bug ratio alert triggers: threshold percentage and consecutive sprint count."
            >
              <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 20 20" fill="currentColor" class="w-3.5 h-3.5">
                <path fill-rule="evenodd" d="M18 10a8 8 0 11-16 0 8 8 0 0116 0zm-7-4a1 1 0 11-2 0 1 1 0 012 0zM9 9a.75.75 0 000 1.5h.253a.25.25 0 01.244.304l-.459 2.066A1.75 1.75 0 0010.747 15H11a.75.75 0 000-1.5h-.253a.25.25 0 01-.244-.304l.459-2.066A1.75 1.75 0 009.253 9H9z" clip-rule="evenodd" />
              </svg>
            </span>
          </div>
          <p class="text-sm text-gray-400">Alert thresholds for flagging developers with consistently high bug ratios.</p>
          <div class="grid grid-cols-2 gap-4">
            <div>
              <label class="block text-sm text-gray-300 mb-1">Alert Threshold (%)</label>
              <input
                v-model.number="form.bugRatioAlertThreshold"
                type="number"
                min="0"
                max="100"
                :disabled="isReadOnly"
                class="w-full bg-gray-800 border border-gray-700 rounded px-3 py-2 text-gray-100 focus:outline-none focus:border-blue-500 disabled:opacity-50 disabled:cursor-not-allowed"
              />
              <p class="text-xs text-gray-500 mt-1">Bug ratio percentage above which a sprint counts toward the alert (0–100).</p>
            </div>
            <div>
              <label class="block text-sm text-gray-300 mb-1">Consecutive Sprint Count</label>
              <input
                v-model.number="form.bugRatioConsecutiveSprintCount"
                type="number"
                min="1"
                max="10"
                :disabled="isReadOnly"
                class="w-full bg-gray-800 border border-gray-700 rounded px-3 py-2 text-gray-100 focus:outline-none focus:border-blue-500 disabled:opacity-50 disabled:cursor-not-allowed"
              />
              <p class="text-xs text-gray-500 mt-1">Number of consecutive sprints above threshold before alert fires (1–10).</p>
            </div>
          </div>
          <div class="grid grid-cols-2 gap-4">
            <div>
              <label class="block text-sm text-gray-300 mb-1 cursor-help" title="Default story points applied to bug tickets with no estimate. Used across all SP calculations. Set to 0 to disable the fallback.">Default SP per Bug</label>
              <input
                v-model.number="form.defaultSpPerBug"
                type="number"
                min="0"
                max="13"
                :disabled="isReadOnly"
                class="w-full bg-gray-800 border border-gray-700 rounded px-3 py-2 text-gray-100 focus:outline-none focus:border-blue-500 disabled:opacity-50 disabled:cursor-not-allowed"
              />
              <p class="text-xs text-gray-500 mt-1">SP applied to unestimated bug tickets across all metrics (0–13). Default: 3.</p>
            </div>
          </div>
          <div class="flex items-center gap-4">
            <button
              @click="saveBugRatioAlertsPanel"
              :disabled="bugRatioAlertsSaving || isReadOnly"
              class="bg-blue-600 hover:bg-blue-700 disabled:opacity-50 disabled:cursor-not-allowed text-white px-4 py-2 rounded font-medium text-sm"
            >
              {{ bugRatioAlertsSaving ? 'Saving...' : 'Save Bug Ratio Alerts' }}
            </button>
            <span v-if="bugRatioAlertsSaved" class="text-green-400 text-sm">Saved.</span>
            <span v-if="bugRatioAlertsError" class="text-red-400 text-sm">{{ bugRatioAlertsError }}</span>
          </div>
        </section>

        </template><!-- end Health tab -->

        <!-- Sync tab -->
        <template v-if="activeSettingsTab === 'sync'">

        <!-- Sync -->
        <section class="bg-gray-900 rounded-lg p-6 space-y-4">
          <div class="flex items-center gap-1">
            <h2 class="text-lg font-semibold text-gray-200">Sync from Jira</h2>
            <span
              class="text-gray-500 cursor-help"
              title="Pull sprint, ticket, developer, and transition data from Jira for the selected range."
            >
              <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 20 20" fill="currentColor" class="w-3.5 h-3.5">
                <path fill-rule="evenodd" d="M18 10a8 8 0 11-16 0 8 8 0 0116 0zm-7-4a1 1 0 11-2 0 1 1 0 012 0zM9 9a.75.75 0 000 1.5h.253a.25.25 0 01.244.304l-.459 2.066A1.75 1.75 0 0010.747 15H11a.75.75 0 000-1.5h-.253a.25.25 0 01-.244-.304l.459-2.066A1.75 1.75 0 009.253 9H9z" clip-rule="evenodd" />
              </svg>
            </span>
          </div>
          <p class="text-sm text-gray-400" title="Re-syncing a sprint overwrites its data with fresh values from Jira. No duplicates created.">Pull all sprints, tickets, developers, and epics from the configured board. Closed sprints are idempotent — safe to re-run.</p>

          <!-- Default sprint count -->
          <div class="flex items-center gap-3">
            <label class="text-sm text-gray-300 whitespace-nowrap" title="Number of most-recent sprints Sync All will pull. Saved with Save Settings.">Default sprint count</label>
            <input
              v-model.number="form.syncBackSprintCount"
              type="number"
              min="1"
              max="50"
              :disabled="isReadOnly"
              class="w-24 bg-gray-800 border border-gray-700 rounded px-3 py-1 text-gray-100 text-sm focus:outline-none focus:border-blue-500 disabled:opacity-50 disabled:cursor-not-allowed"
            />
            <span class="text-xs text-gray-500">(1–50 sprints, save with Save Sync Config)</span>
          </div>

          <!-- Planning window -->
          <div class="flex items-center gap-3">
            <label class="text-sm text-gray-300 whitespace-nowrap" title="Tickets added within this many days after sprint start are treated as committed (part of planning), not mid-sprint additions. Affects disruption rate and classification. Re-sync after changing.">Planning window (days)</label>
            <input
              v-model.number="form.planningWindowDays"
              type="number"
              min="0"
              max="7"
              :disabled="isReadOnly"
              class="w-24 bg-gray-800 border border-gray-700 rounded px-3 py-1 text-gray-100 text-sm focus:outline-none focus:border-blue-500 disabled:opacity-50 disabled:cursor-not-allowed"
            />
            <span class="text-xs text-gray-500">(0–7 days, re-sync to apply)</span>
          </div>

          <div class="flex items-center gap-4">
            <button
              @click="saveSyncConfigPanel"
              :disabled="syncConfigSaving || isReadOnly"
              class="bg-blue-600 hover:bg-blue-700 disabled:opacity-50 disabled:cursor-not-allowed text-white px-4 py-2 rounded font-medium text-sm"
            >
              {{ syncConfigSaving ? 'Saving...' : 'Save Sync Config' }}
            </button>
            <span v-if="syncConfigSaved" class="text-green-400 text-sm">Saved.</span>
            <span v-if="syncConfigError" class="text-red-400 text-sm">{{ syncConfigError }}</span>
          </div>

          <button
            @click="syncAll"
            :disabled="syncing || !form.boardId || isReadOnly"
            class="bg-green-600 hover:bg-green-700 disabled:opacity-50 disabled:cursor-not-allowed text-white px-6 py-2 rounded font-medium"
          >
            {{ syncing ? 'Syncing...' : 'Sync All' }}
          </button>
          <p class="text-xs text-gray-500">Sync All also syncs future sprints and epics.</p>

          <div v-if="syncError" class="text-red-400 text-sm">{{ syncError }}</div>

          <div v-if="syncResult" class="space-y-2 text-sm">
            <p class="text-green-400 font-medium" title="Post-sync report showing sprints synced, tickets upserted, developers found, and any failures.">Sync complete!</p>
            <div class="grid grid-cols-2 gap-x-4 gap-y-1 text-gray-300">
              <span>Sprints synced:</span><span class="text-gray-100">{{ syncResult.sprints.sprintsSynced }}</span>
              <span>Tickets upserted:</span><span class="text-gray-100">{{ syncResult.sprints.ticketsUpserted }}</span>
              <span>Developers discovered:</span><span class="text-gray-100">{{ syncResult.sprints.developersDiscovered }}</span>
              <span>Epic tickets discovered:</span><span class="text-gray-100">{{ syncResult.backlog.epicTicketsDiscovered }}</span>
            </div>
            <div v-if="syncResult.sprints.failures.length > 0" class="text-amber-400">
              {{ syncResult.sprints.failures.length }} sprint(s) failed to sync.
            </div>
          </div>

          <!-- Sync Range (GAP-2) -->
          <div class="border-t border-gray-800 pt-4 space-y-3">
            <div class="flex items-center justify-between">
              <h3 class="text-sm font-medium text-gray-300">Sync Custom Range</h3>
              <button
                @click="loadSprintsForRange"
                :disabled="sprintsForRangeLoading || !form.boardId || isReadOnly"
                class="border border-gray-600 hover:border-gray-400 text-gray-300 hover:text-gray-100 disabled:opacity-40 disabled:cursor-not-allowed px-3 py-1 rounded text-sm"
              >
                {{ sprintsForRangeLoading ? 'Loading...' : 'Load Sprints' }}
              </button>
            </div>
            <p v-if="sprintsForRangeError" class="text-red-400 text-xs">{{ sprintsForRangeError }}</p>
            <template v-if="sprintsForRange.length > 0">
              <div class="grid grid-cols-2 gap-4">
                <div>
                  <label class="block text-xs text-gray-400 mb-1">From sprint</label>
                  <select
                    v-model.number="fromSprintId"
                    :disabled="isReadOnly"
                    class="w-full bg-gray-800 border border-gray-700 rounded px-3 py-2 text-gray-100 text-sm focus:outline-none focus:border-blue-500 disabled:opacity-50 disabled:cursor-not-allowed"
                  >
                    <option v-for="s in sprintsForRange" :key="s.id" :value="s.id">{{ s.name }}</option>
                  </select>
                </div>
                <div>
                  <label class="block text-xs text-gray-400 mb-1">To sprint</label>
                  <select
                    v-model.number="toSprintId"
                    :disabled="isReadOnly"
                    class="w-full bg-gray-800 border border-gray-700 rounded px-3 py-2 text-gray-100 text-sm focus:outline-none focus:border-blue-500 disabled:opacity-50 disabled:cursor-not-allowed"
                  >
                    <option v-for="s in sprintsForRange" :key="s.id" :value="s.id">{{ s.name }}</option>
                  </select>
                </div>
              </div>
              <button
                @click="syncRange"
                :disabled="syncingRange || !fromSprintId || !toSprintId || isReadOnly"
                class="bg-green-600 hover:bg-green-700 disabled:opacity-50 disabled:cursor-not-allowed text-white px-4 py-2 rounded text-sm font-medium"
              >
                {{ syncingRange ? 'Syncing...' : 'Sync Range' }}
              </button>
              <div v-if="syncRangeError" class="text-red-400 text-sm">{{ syncRangeError }}</div>
              <div v-if="syncRangeResult" class="space-y-1 text-sm">
                <p class="text-green-400 font-medium">Range sync complete!</p>
                <div class="grid grid-cols-2 gap-x-4 gap-y-1 text-gray-300">
                  <span>Sprints synced:</span><span class="text-gray-100">{{ syncRangeResult.sprintsSynced }}</span>
                  <span>Tickets upserted:</span><span class="text-gray-100">{{ syncRangeResult.ticketsUpserted }}</span>
                  <span>Developers discovered:</span><span class="text-gray-100">{{ syncRangeResult.developersDiscovered }}</span>
                </div>
                <div v-if="syncRangeResult.failures.length > 0" class="text-amber-400">
                  {{ syncRangeResult.failures.length }} sprint(s) failed to sync.
                </div>
              </div>
            </template>
          </div>
        </section>

        </template><!-- end Sync tab -->

        <!-- Users tab -->
        <template v-if="activeSettingsTab === 'users' && authStore.isAdmin">

        <!-- User Management (Admin only) -->
        <section class="bg-gray-900 rounded-lg p-6 space-y-6">
          <h2 class="text-lg font-semibold text-gray-200">User Management</h2>

          <div v-if="userMgmtError" class="text-red-400 text-sm">{{ userMgmtError }}</div>

          <!-- Invite form -->
          <div class="space-y-3">
            <h3 class="text-sm font-medium text-gray-300">Invite a new user</h3>
            <div class="flex gap-2">
              <input
                v-model="inviteEmail"
                type="email"
                placeholder="user@company.com"
                class="flex-1 bg-gray-800 border border-gray-700 rounded px-3 py-2 text-gray-100 placeholder-gray-500 focus:outline-none focus:border-blue-500 text-sm"
              />
              <select
                v-model="inviteRole"
                class="bg-gray-800 border border-gray-700 rounded px-3 py-2 text-gray-100 focus:outline-none focus:border-blue-500 text-sm"
              >
                <option value="Manager">Manager</option>
                <option value="Admin">Admin</option>
              </select>
              <button
                @click="sendInvite"
                :disabled="inviting || !inviteEmail"
                class="bg-blue-600 hover:bg-blue-700 disabled:opacity-50 disabled:cursor-not-allowed text-white px-4 py-2 rounded text-sm font-medium"
              >
                {{ inviting ? 'Inviting...' : 'Send Invite' }}
              </button>
            </div>
            <div v-if="inviteError" class="text-red-400 text-sm">{{ inviteError }}</div>
            <div v-if="inviteResult" class="space-y-1 bg-gray-800 rounded p-3 text-sm">
              <p class="text-green-400">Invitation created! Share this link:</p>
              <div class="flex items-center gap-2">
                <code class="flex-1 text-gray-300 text-xs break-all">{{ inviteResult.inviteLink }}</code>
                <button
                  @click="copyInviteLink(inviteResult!.inviteLink)"
                  class="border border-gray-600 hover:border-gray-400 text-gray-300 hover:text-gray-100 px-2 py-1 rounded text-xs"
                >Copy</button>
              </div>
              <p class="text-gray-500 text-xs">Expires {{ new Date(inviteResult.expiresAt).toLocaleDateString() }}</p>
            </div>
          </div>

          <!-- Pending invitations -->
          <div v-if="invitations.filter(i => i.status === 'Pending').length > 0" class="space-y-2">
            <h3 class="text-sm font-medium text-gray-300">Pending Invitations</h3>
            <table class="w-full text-sm text-gray-300">
              <thead>
                <tr class="text-gray-500 text-xs border-b border-gray-800">
                  <th class="text-left py-1 font-normal">Email</th>
                  <th class="text-left py-1 font-normal">Role</th>
                  <th class="text-left py-1 font-normal">Expires</th>
                  <th class="text-right py-1 font-normal"></th>
                </tr>
              </thead>
              <tbody>
                <tr v-for="inv in invitations.filter(i => i.status === 'Pending')" :key="inv.id" class="border-b border-gray-800/50">
                  <td class="py-2">{{ inv.email }}</td>
                  <td class="py-2">{{ inv.role }}</td>
                  <td class="py-2 text-xs text-gray-500">{{ new Date(inv.expiresAt).toLocaleDateString() }}</td>
                  <td class="py-2 text-right">
                    <button
                      @click="revokeInvite(inv.id)"
                      class="text-xs text-red-400 hover:text-red-300"
                    >Revoke</button>
                  </td>
                </tr>
              </tbody>
            </table>
          </div>

          <!-- Users table -->
          <div class="space-y-2">
            <h3 class="text-sm font-medium text-gray-300">Users</h3>
            <div v-if="usersLoading" class="text-gray-500 text-sm">Loading users...</div>
            <table v-else class="w-full text-sm text-gray-300">
              <thead>
                <tr class="text-gray-500 text-xs border-b border-gray-800">
                  <th class="text-left py-1 font-normal">Name</th>
                  <th class="text-left py-1 font-normal">Email</th>
                  <th class="text-left py-1 font-normal">Role</th>
                  <th class="text-left py-1 font-normal">Last login</th>
                  <th class="text-left py-1 font-normal">Active</th>
                </tr>
              </thead>
              <tbody>
                <tr v-for="u in users" :key="u.id" class="border-b border-gray-800/50">
                  <td class="py-2">
                    <div class="flex items-center gap-2">
                      <img v-if="u.avatarUrl" :src="u.avatarUrl" class="w-6 h-6 rounded-full" />
                      <span>{{ u.displayName }}</span>
                    </div>
                  </td>
                  <td class="py-2 text-gray-400 text-xs">{{ u.email }}</td>
                  <td class="py-2">
                    <select
                      :value="u.role"
                      @change="changeUserRole(u.id, ($event.target as HTMLSelectElement).value)"
                      class="bg-gray-800 border border-gray-700 rounded px-2 py-1 text-gray-100 text-xs focus:outline-none focus:border-blue-500"
                    >
                      <option value="Admin">Admin</option>
                      <option value="Manager">Manager</option>
                    </select>
                  </td>
                  <td class="py-2 text-xs text-gray-500">
                    {{ u.lastLoginAt ? new Date(u.lastLoginAt).toLocaleDateString() : 'Never' }}
                  </td>
                  <td class="py-2">
                    <button
                      @click="toggleUserStatus(u.id, !u.isActive)"
                      :class="u.isActive ? 'bg-green-600 hover:bg-green-700' : 'bg-gray-700 hover:bg-gray-600'"
                      class="px-2 py-1 rounded text-xs text-white transition-colors"
                    >
                      {{ u.isActive ? 'Active' : 'Inactive' }}
                    </button>
                  </td>
                </tr>
              </tbody>
            </table>
          </div>
        </section>

        </template><!-- end Users tab -->

      </template>
  </div>
</template>
