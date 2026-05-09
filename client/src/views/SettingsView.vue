<script setup lang="ts">
import { onMounted, reactive, ref, computed } from 'vue'
import { useSettingsStore } from '../stores/settingsStore'
import { getBoards, getStatuses } from '../api/settings'
import { getJiraSprints, syncSprints, syncBacklog } from '../api/sync'
import type { AppSettings, BoardOption, StatusOption, SyncSprintsResponse, SyncBacklogResponse } from '../types'

const store = useSettingsStore()
const saved = ref(false)
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
  }
})

function syncFromStore() {
  const s = store.settings
  form.boardId = s.boardId
  form.doneStatuses = [...s.doneStatuses]
  form.workflowStages = [...s.workflowStages]
  form.healthThresholds = { ...s.healthThresholds }
  form.healthWeights = { ...s.healthWeights }
}

// Statuses available to add (not already in doneStatuses), done category first
const availableStatuses = computed<StatusOption[]>(() => {
  const notAdded = statuses.value.filter(s => !form.doneStatuses.includes(s.name))
  const done = notAdded.filter(s => s.categoryKey === 'done')
  const rest = notAdded.filter(s => s.categoryKey !== 'done')
  return [...done, ...rest]
})

onMounted(async () => {
  // All three fire in parallel — form is not blocked on dropdown data
  boardsLoading.value = true
  statusesLoading.value = true

  const [, boardsResult, statusesResult] = await Promise.allSettled([
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
    getStatuses()
  ])

  boardsLoading.value = false
  statusesLoading.value = false

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
  } else {
    statusesError.value = true
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

async function save() {
  saved.value = false
  await store.updateSettings({ ...form, doneStatuses: [...form.doneStatuses], workflowStages: [...form.workflowStages], healthThresholds: { ...form.healthThresholds }, healthWeights: { ...form.healthWeights } })
  if (!store.error) {
    store.clearDetection()
    saved.value = true
    setTimeout(() => saved.value = false, 3000)
  }
}

const weightsSum = () => form.healthWeights.completion + form.healthWeights.disruption + form.healthWeights.carryOver

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
    // Save settings first to ensure boardId is persisted
    await save()
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

    const first = jiraSprints[0]
    const last = jiraSprints[jiraSprints.length - 1]

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

      <div v-if="store.loading" class="text-gray-400">Loading settings...</div>

      <template v-else>
        <!-- Jira Board -->
        <section class="bg-gray-900 rounded-lg p-6 space-y-4">
          <h2 class="text-lg font-semibold text-gray-200">Jira Board</h2>
          <div>
            <label class="block text-sm text-gray-400 mb-1">Board</label>
            <!-- Dropdown when boards loaded successfully -->
            <template v-if="!boardsError && !boardsLoading && boards.length > 0">
              <select
                v-model.number="form.boardId"
                class="w-full bg-gray-800 border border-gray-700 rounded px-3 py-2 text-gray-100 focus:outline-none focus:border-blue-500"
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
                class="w-full bg-gray-800 border border-gray-700 rounded px-3 py-2 text-gray-100 placeholder-gray-500 focus:outline-none focus:border-blue-500"
              />
              <p v-if="boardsError" class="text-xs text-amber-400 mt-1">Could not load boards from Jira.</p>
            </template>
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
              <button @click="removeStatus(i)" class="text-gray-500 hover:text-red-400 ml-1">&times;</button>
            </span>
          </div>
          <!-- Dropdown mode -->
          <template v-if="!statusesError && !statusesLoading && statuses.length > 0">
            <div class="flex gap-2">
              <select
                v-model="selectedStatus"
                class="flex-1 bg-gray-800 border border-gray-700 rounded px-3 py-2 text-gray-100 focus:outline-none focus:border-blue-500"
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
                :disabled="!selectedStatus"
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
                class="flex-1 bg-gray-800 border border-gray-700 rounded px-3 py-2 text-gray-100 placeholder-gray-500 focus:outline-none focus:border-blue-500"
              />
              <button @click="addStatus" class="bg-blue-600 hover:bg-blue-700 text-white px-4 py-2 rounded">Add</button>
            </div>
            <p v-if="statusesError" class="text-xs text-amber-400 mt-1">Could not load statuses from Jira.</p>
          </template>
        </section>

        <!-- Workflow Stages -->
        <section class="bg-gray-900 rounded-lg p-6 space-y-4">
          <div class="flex items-center justify-between">
            <h2 class="text-lg font-semibold text-gray-200">Workflow Stages</h2>
            <button
              @click="reDetect"
              :disabled="reDetectDisabled"
              class="border border-gray-600 hover:border-gray-400 text-gray-300 hover:text-gray-100 disabled:opacity-40 disabled:cursor-not-allowed px-3 py-1 rounded text-sm"
            >
              {{ store.detecting ? 'Detecting...' : 'Re-detect' }}
            </button>
          </div>
          <p class="text-sm text-gray-400">Ordered workflow stages for cycle time computation. Will be auto-detected on first sync if left empty.</p>

          <!-- Confidence summary -->
          <p
            v-if="store.detectionResult !== null"
            class="text-sm text-gray-400"
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
              <button @click="moveStage(i, -1)" :disabled="i === 0" class="text-gray-500 hover:text-gray-300 disabled:opacity-30">&uarr;</button>
              <button @click="moveStage(i, 1)" :disabled="i === form.workflowStages.length - 1" class="text-gray-500 hover:text-gray-300 disabled:opacity-30">&darr;</button>
              <button @click="removeStage(i)" class="text-gray-500 hover:text-red-400">&times;</button>
            </div>
          </div>
          <div class="flex gap-2">
            <input
              v-model="newStage"
              @keyup.enter="addStage"
              placeholder="Add stage..."
              class="flex-1 bg-gray-800 border border-gray-700 rounded px-3 py-2 text-gray-100 placeholder-gray-500 focus:outline-none focus:border-blue-500"
            />
            <button @click="addStage" class="bg-blue-600 hover:bg-blue-700 text-white px-4 py-2 rounded">Add</button>
          </div>

          <!-- Other statuses (sidelined) -->
          <div
            v-if="store.detectionResult !== null && store.detectionResult.sidelined.length > 0"
            class="space-y-2 pt-2 border-t border-gray-800"
          >
            <p class="text-sm text-gray-400 font-medium">Other statuses</p>
            <div class="flex flex-wrap gap-2">
              <span
                v-for="status in store.detectionResult.sidelined"
                :key="status"
                class="inline-flex items-center gap-1 bg-gray-800 text-gray-300 px-3 py-1 rounded-full text-sm"
              >
                {{ status }}
                <button @click="moveSidelined(status)" class="text-gray-500 hover:text-green-400 ml-1" title="Add to stages">+</button>
              </span>
            </div>
          </div>
        </section>

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
                  <input v-model.number="form.healthThresholds.completionGreen" type="number" min="0" max="100" class="w-full bg-gray-800 border border-gray-700 rounded px-2 py-1 text-gray-100 text-sm focus:outline-none focus:border-blue-500" />
                </div>
                <div>
                  <label class="text-xs text-amber-400">Amber &ge;</label>
                  <input v-model.number="form.healthThresholds.completionAmber" type="number" min="0" max="100" class="w-full bg-gray-800 border border-gray-700 rounded px-2 py-1 text-gray-100 text-sm focus:outline-none focus:border-blue-500" />
                </div>
              </div>
            </div>

            <div>
              <h3 class="text-sm font-medium text-gray-300 mb-2">Disruption Rate (%)</h3>
              <div class="space-y-2">
                <div>
                  <label class="text-xs text-green-400">Green &le;</label>
                  <input v-model.number="form.healthThresholds.disruptionGreen" type="number" min="0" max="100" class="w-full bg-gray-800 border border-gray-700 rounded px-2 py-1 text-gray-100 text-sm focus:outline-none focus:border-blue-500" />
                </div>
                <div>
                  <label class="text-xs text-amber-400">Amber &le;</label>
                  <input v-model.number="form.healthThresholds.disruptionAmber" type="number" min="0" max="100" class="w-full bg-gray-800 border border-gray-700 rounded px-2 py-1 text-gray-100 text-sm focus:outline-none focus:border-blue-500" />
                </div>
              </div>
            </div>

            <div>
              <h3 class="text-sm font-medium text-gray-300 mb-2">Carry-Over Rate (%)</h3>
              <div class="space-y-2">
                <div>
                  <label class="text-xs text-green-400">Green &le;</label>
                  <input v-model.number="form.healthThresholds.carryOverGreen" type="number" min="0" max="100" class="w-full bg-gray-800 border border-gray-700 rounded px-2 py-1 text-gray-100 text-sm focus:outline-none focus:border-blue-500" />
                </div>
                <div>
                  <label class="text-xs text-amber-400">Amber &le;</label>
                  <input v-model.number="form.healthThresholds.carryOverAmber" type="number" min="0" max="100" class="w-full bg-gray-800 border border-gray-700 rounded px-2 py-1 text-gray-100 text-sm focus:outline-none focus:border-blue-500" />
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
              <input v-model.number="form.healthWeights.completion" type="number" min="0" max="100" class="w-full bg-gray-800 border border-gray-700 rounded px-3 py-2 text-gray-100 focus:outline-none focus:border-blue-500" />
            </div>
            <div>
              <label class="block text-sm text-gray-300 mb-1">Disruption</label>
              <input v-model.number="form.healthWeights.disruption" type="number" min="0" max="100" class="w-full bg-gray-800 border border-gray-700 rounded px-3 py-2 text-gray-100 focus:outline-none focus:border-blue-500" />
            </div>
            <div>
              <label class="block text-sm text-gray-300 mb-1">Carry-Over</label>
              <input v-model.number="form.healthWeights.carryOver" type="number" min="0" max="100" class="w-full bg-gray-800 border border-gray-700 rounded px-3 py-2 text-gray-100 focus:outline-none focus:border-blue-500" />
            </div>
          </div>
          <div class="text-sm" :class="weightsSum() === 100 ? 'text-green-400' : 'text-red-400'">
            Sum: {{ weightsSum() }} / 100
          </div>
        </section>

        <!-- Save -->
        <div class="flex items-center gap-4">
          <button
            @click="save"
            :disabled="store.saving || weightsSum() !== 100"
            class="bg-blue-600 hover:bg-blue-700 disabled:opacity-50 disabled:cursor-not-allowed text-white px-6 py-2 rounded font-medium"
          >
            {{ store.saving ? 'Saving...' : 'Save Settings' }}
          </button>
          <span v-if="saved" class="text-green-400 text-sm">Settings saved successfully.</span>
          <span v-if="store.error" class="text-red-400 text-sm">{{ store.error }}</span>
        </div>

        <!-- Sync -->
        <section class="bg-gray-900 rounded-lg p-6 space-y-4">
          <h2 class="text-lg font-semibold text-gray-200">Sync from Jira</h2>
          <p class="text-sm text-gray-400">Pull all sprints, tickets, developers, and epics from the configured board. Closed sprints are idempotent — safe to re-run.</p>

          <button
            @click="syncAll"
            :disabled="syncing || !form.boardId"
            class="bg-green-600 hover:bg-green-700 disabled:opacity-50 disabled:cursor-not-allowed text-white px-6 py-2 rounded font-medium"
          >
            {{ syncing ? 'Syncing...' : 'Sync All' }}
          </button>

          <div v-if="syncError" class="text-red-400 text-sm">{{ syncError }}</div>

          <div v-if="syncResult" class="space-y-2 text-sm">
            <p class="text-green-400 font-medium">Sync complete!</p>
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
        </section>
      </template>
  </div>
</template>
