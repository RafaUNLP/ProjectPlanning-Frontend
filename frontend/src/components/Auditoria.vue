<template>
  <v-container fluid>
    <v-row>
      <v-col cols="12">
        <h2>Proyectos en ejecución</h2>
        <v-alert v-if="error" type="error" dense>{{ error }}</v-alert>
        <v-progress-circular v-if="loading" indeterminate color="primary" />

        <v-list v-if="!loading && proyectos.length">
          <v-list-item v-for="proy in proyectos" :key="proy.id" class="mb-4">
            <v-card outlined class="w-100">
              <v-card-title>
                <div>
                  <div class="text-h6">{{ proy.nombre }}</div>
                  <div class="text-subtitle-2">ID caso: {{ caseId }}</div>
                </div>
              </v-card-title>
              <v-card-text>
                <div>{{ proy.descripcion }}</div>

                <v-expansion-panels>
                  <v-expansion-panel v-for="et in proy.etapas || []" :key="et.id">
                    <v-expansion-panel-title>
                      {{ et.nombre }} — {{ new Date(et.fechaInicio).toLocaleDateString() }} → {{ new Date(et.fechaFin).toLocaleDateString() }}
                    </v-expansion-panel-title>
                    <v-expansion-panel-text>
                      <div>{{ et.descripcion }}</div>
                      <div v-if="et.colaboracion">
                        <strong>Colaboración:</strong> {{ et.colaboracion.organizacionComprometidaId || 'Sin asignar' }}
                        
                        <div class="mt-4">
                          <strong>Observaciones:</strong>
                          <v-list v-if="et.colaboracion.observaciones && et.colaboracion.observaciones.length">
                            <v-list-item v-for="obs in et.colaboracion.observaciones" :key="obs.id" density="compact">
                              <div class="observation-item">
                                <div class="text-body2">{{ obs.descripcion }}</div>
                                <div class="text-caption text-grey">{{ new Date(obs.fechaCarga).toLocaleString() }}</div>
                                <div v-if="obs.fechaRealizacion" class="text-caption text-green">
                                  Realizada: {{ new Date(obs.fechaRealizacion).toLocaleString() }}
                                </div>
                              </div>
                            </v-list-item>
                          </v-list>
                          <div v-else class="text-caption text-grey">Sin observaciones</div>

                          <v-form @submit.prevent="agregarObservacion(et.colaboracion)" class="mt-3">
                            <v-text-field
                              v-model="nuevaObservacion[et.colaboracion.id]"
                              label="Nueva observación"
                              dense
                              @keyup.enter="agregarObservacion(et.colaboracion)"
                            />
                            <v-btn
                              type="submit"
                              color="primary"
                              size="small"
                              :loading="cargandoObservacion[et.colaboracion.id]"
                            >
                              Agregar
                            </v-btn>
                          </v-form>
                        </div>
                      </div>
                    </v-expansion-panel-text>
                  </v-expansion-panel>
                </v-expansion-panels>
              </v-card-text>
            </v-card>
          </v-list-item>
        </v-list>

        <div v-if="!loading && !proyectos.length" class="text-center">No hay proyectos en ejecución.</div>
      </v-col>
    </v-row>
  </v-container>
</template>

<script setup lang="ts">
import { ref, onMounted } from 'vue'
import api from '../api'

const proyectos = ref<any[]>([])
const loading = ref(false)
const error = ref('')
const caseId = ref<string>('')
const nuevaObservacion = ref<Record<string, string>>({})
const cargandoObservacion = ref<Record<string, boolean>>({})

onMounted(async () => {
  loading.value = true
  error.value = ''
  try {
    const resp = await api.get('/Auditoria')
    // El backend retorna { caseId, proyectos }
    if (resp && resp.data) {
      caseId.value = resp.data.caseId
      proyectos.value = resp.data.proyectos || resp.data || []
    }
  } catch (err: any) {
    console.error('Error al recuperar auditoría:', err)
    error.value = err?.response?.data || 'Error recuperando proyectos en ejecución.'
  } finally {
    loading.value = false
  }
})

const agregarObservacion = async (colaboracion: any) => {
  const colabId = colaboracion.id
  const descripcion = nuevaObservacion.value[colabId]?.trim()

  if (!descripcion) {
    error.value = 'La descripción de la observación no puede estar vacía'
    return
  }

  cargandoObservacion.value[colabId] = true
  try {
    const resp = await api.post('/Observacion', {
      colaboracionId: colabId,
      descripcion: descripcion,
      fechaCarga: new Date().toISOString(),
      caseId: parseInt(caseId.value)
    })

    if (resp.data.observacion) {
      // Agregar la nueva observación a la lista
      if (!colaboracion.observaciones) {
        colaboracion.observaciones = []
      }
      colaboracion.observaciones.push(resp.data.observacion)
      nuevaObservacion.value[colabId] = ''
      error.value = ''
    }
  } catch (err: any) {
    console.error('Error al agregar observación:', err)
    error.value = err?.response?.data?.mensaje || err?.response?.data || 'Error al agregar la observación'
  } finally {
    cargandoObservacion.value[colabId] = false
  }
}
</script>

<style scoped>
.w-100 { 
  width: 100%; 
}

.observation-item {
  padding: 8px 0;
  border-bottom: 1px solid #eee;
}

.observation-item:last-child {
  border-bottom: none;
}

.text-grey {
  color: #999;
}

.text-green {
  color: #4caf50;
}

.mt-3 {
  margin-top: 12px;
}

.mt-4 {
  margin-top: 16px;
}
</style>
