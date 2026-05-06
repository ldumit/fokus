<script setup lang="ts">
import { onMounted, reactive, ref } from 'vue'
import { useSettingsStore } from '../stores/settingsStore'
import type { AppSettings } from '../types'

const store = useSettingsStore()
const saved = ref(false)
const newStatus = ref('')
const newStage = ref('')

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

onMounted(async () => {
  await store.fetchSettings()
  syncFromStore()
})

function addStatus() {
  const val = newStatus.value.trim()
  if (val && !form.doneStatuses.includes(val)) {
    form.doneStatuses.push(val)
  }
  newStatus.value = ''
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
    saved.value = true
    setTimeout(() => saved.value = false, 3000)
  }
}

const weightsSum = () => form.healthWeights.completion + form.healthWeights.disruption + form.healthWeights.carryOver
</script>

<template>
  <div class="min-h-screen bg-gray-950 p-8">
    <div class="max-w-3xl mx-auto space-y-6">
      <h1 class="text-3xl font-bold text-gray-100">Settings</h1>

      <div v-if="store.loading" class="text-gray-400">Loading settings...</div>

      <template v-else>
        <!-- Jira Board -->
        <section class="bg-gray-900 rounded-lg p-6 space-y-4">
          <h2 class="text-lg font-semibold text-gray-200">Jira Board</h2>
          <div>
            <label class="block text-sm text-gray-400 mb-1">Board ID</label>
            <input
              v-model.number="form.boardId"
              type="number"
              placeholder="Enter Jira board ID"
              class="w-full bg-gray-800 border border-gray-700 rounded px-3 py-2 text-gray-100 placeholder-gray-500 focus:outline-none focus:border-blue-500"
            />
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
          <div class="flex gap-2">
            <input
              v-model="newStatus"
              @keyup.enter="addStatus"
              placeholder="Add status..."
              class="flex-1 bg-gray-800 border border-gray-700 rounded px-3 py-2 text-gray-100 placeholder-gray-500 focus:outline-none focus:border-blue-500"
            />
            <button @click="addStatus" class="bg-blue-600 hover:bg-blue-700 text-white px-4 py-2 rounded">Add</button>
          </div>
        </section>

        <!-- Workflow Stages -->
        <section class="bg-gray-900 rounded-lg p-6 space-y-4">
          <h2 class="text-lg font-semibold text-gray-200">Workflow Stages</h2>
          <p class="text-sm text-gray-400">Ordered workflow stages for cycle time computation. Will be auto-detected on first sync if left empty.</p>
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
      </template>
    </div>
  </div>
</template>
