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
                  <div class="text-subtitle-2">ID caso: {{ proy.bonitaCaseId }}</div>
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
                      <div v-if="et.colaboracion">Colaboración: {{ et.colaboracion.organizacionComprometidaId || 'Sin asignar' }}</div>
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

onMounted(async () => {
  loading.value = true
  error.value = ''
  try {
    const resp = await api.get('/Auditoria')
    // El backend retorna { caseId, proyectos }
    if (resp && resp.data) {
      proyectos.value = resp.data.proyectos || resp.data || []
    }
  } catch (err: any) {
    console.error('Error al recuperar auditoría:', err)
    error.value = err?.response?.data || 'Error recuperando proyectos en ejecución.'
  } finally {
    loading.value = false
  }
})
</script>

<style scoped>
.w-100 { width: 100%; }
</style>
