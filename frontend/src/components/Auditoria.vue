<template>
  <v-container fluid class="pa-4">
    <h1 class="text-h5 text-md-h4 mb-6 font-weight-bold text-primary">Auditoría de Proyectos</h1>

    <!-- Loading global -->
    <v-progress-linear
      v-if="loading"
      indeterminate
      color="primary"
      class="mb-4"
    ></v-progress-linear>

    <!-- Error global -->
    <v-alert
      v-if="error"
      type="error"
      variant="tonal"
      class="mb-6"
      closable
      @click:close="error = ''"
    >
      {{ error }}
    </v-alert>

    <!-- Estado vacío -->
    <div v-if="!loading && proyectos.length === 0" class="text-center mt-10 text-grey-darken-1">
      <v-icon icon="mdi-clipboard-text-clock-outline" size="64" class="mb-2"></v-icon>
      <p class="text-h6">No hay proyectos en ejecución para auditar.</p>
    </div>

    <!-- Listado de Proyectos -->
    <div v-if="!loading && proyectos.length > 0">
      <h2 class="text-h6 mb-3 mt-4 text-secondary">Proyectos en Ejecución ({{ proyectos.length }})</h2>
      <v-divider class="mb-4"></v-divider>

      <v-expansion-panels variant="popout" class="mb-8">
        <v-expansion-panel
          v-for="proy in proyectos"
          :key="proy.id"
          class="mb-4 elevation-4"
        >
          <!-- Cabecera del Proyecto -->
          <v-expansion-panel-title class="font-weight-bold text-body-1 bg-white border-s-primary">
            <div class="d-flex align-center w-100 flex-wrap">
              <v-icon icon="mdi-briefcase-search" class="mr-3 text-primary"></v-icon>
              <span class="mr-2">{{ proy.nombre }}</span>
              
              <!--<v-chip size="small" color="info" variant="tonal" class="ml-2" label>
                <v-icon start icon="mdi-identifier"></v-icon>
                Caso Bonita: {{ caseId }}
              </v-chip>-->
            </div>
          </v-expansion-panel-title>

          <!-- Contenido del Proyecto (Etapas) -->
          <v-expansion-panel-text class="pa-4 bg-grey-lighten-4">
            <p class="text-body-2 text-medium-emphasis mb-4">{{ proy.descripcion }}</p>
            
            <h3 class="text-subtitle-1 font-weight-bold mb-3 d-flex align-center">
              <v-icon icon="mdi-layers-triple" size="small" class="mr-2"></v-icon>
              Etapas y Colaboraciones
            </h3>

            <v-row dense>
              <v-col 
                v-for="et in proy.etapas || []" 
                :key="et.id"
                cols="12"
                lg="6"
                xl="4"
              >
                <!-- Tarjeta de Etapa -->
                <v-card elevation="1" class="h-100 border-t-lg-primary">
                  <v-card-title class="text-subtitle-2 font-weight-bold bg-grey-lighten-5 pt-3 pb-2 border-b">
                    <div class="d-flex justify-space-between align-center">
                      <span>{{ et.nombre }}</span>
                      <v-chip size="x-small" color="grey-darken-1" variant="outlined">
                         {{ formatearFecha(et.fechaInicio) }} - {{ formatearFecha(et.fechaFin) }}
                      </v-chip>
                    </div>
                  </v-card-title>

                  <v-card-text class="pt-3">
                    <p class="text-caption text-medium-emphasis mb-3">{{ et.descripcion }}</p>
                    
                    <!-- Sección de Colaboración -->
                    <div v-if="et.colaboracion" class="mt-2">
                      <v-divider class="mb-2"></v-divider>
                      <div class="d-flex align-center mb-2">
                        <v-icon icon="mdi-handshake" size="small" color="secondary" class="mr-2"></v-icon>
                        <span class="text-caption font-weight-bold text-secondary">
                          Colaboración Activa (Org. {{ et.colaboracion.organizacionComprometidaId }})
                        </span>
                      </div>

                      <!-- Listado de Observaciones -->
                      <div class="bg-grey-lighten-5 rounded pa-2 mb-3 border">
                        <div class="text-caption font-weight-bold text-grey-darken-2 mb-1">
                          Observaciones ({{ et.colaboracion.observaciones?.length || 0 }})
                        </div>

                        <v-list v-if="et.colaboracion.observaciones && et.colaboracion.observaciones.length" density="compact" class="bg-transparent pa-0" max-height="200" style="overflow-y: auto;">
                          <v-list-item 
                            v-for="obs in et.colaboracion.observaciones" 
                            :key="obs.id" 
                            class="mb-1 bg-white rounded border pa-1 min-h-0"
                            density="compact"
                          >
                            <template v-slot:prepend>
                              <v-icon 
                                :icon="obs.fechaRealizacion ? 'mdi-check-circle' : 'mdi-clock-outline'" 
                                :color="obs.fechaRealizacion ? 'success' : 'warning'" 
                                size="x-small"
                                class="mt-1 mr-2"
                              ></v-icon>
                            </template>
                            
                            <div class="d-flex flex-column">
                              <span class="text-caption font-weight-medium" style="line-height: 1.2;">{{ obs.descripcion }}</span>
                              <span class="text-caption text-grey" style="font-size: 0.7rem !important;">
                                {{ new Date(obs.fechaCarga).toLocaleDateString() }} 
                                <span v-if="obs.fechaRealizacion" class="text-success ml-1">
                                  (Resuelta)
                                </span>
                              </span>
                            </div>
                          </v-list-item>
                        </v-list>
                        <div v-else class="text-caption text-grey font-italic text-center py-2">
                          Sin observaciones registradas.
                        </div>
                      </div>

                      <!-- Formulario Nueva Observación -->
                      <v-form @submit.prevent="agregarObservacion(et.colaboracion)">
                        <v-text-field
                          v-model="nuevaObservacion[et.colaboracion.id]"
                          label="Agregar nueva observación..."
                          variant="outlined"
                          density="compact"
                          hide-details
                          bg-color="white"
                          style="color: black"
                          class="text-caption"
                          append-inner-icon="mdi-send"
                          :loading="cargandoObservacion[et.colaboracion.id]"
                          @click:append-inner="agregarObservacion(et.colaboracion)"
                          @keyup.enter="agregarObservacion(et.colaboracion)"
                        ></v-text-field>
                      </v-form>
                    </div>

                    <div v-else class="text-caption text-grey font-italic mt-4 border-t pt-2">
                      <v-icon icon="mdi-cancel" size="x-small" class="mr-1"></v-icon>
                      Sin colaboración asignada.
                    </div>

                  </v-card-text>
                </v-card>
              </v-col>
            </v-row>
          </v-expansion-panel-text>
        </v-expansion-panel>
      </v-expansion-panels>
    </div>
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
    // Podrías usar un snackbar aquí en lugar de alertar error global, pero mantenemos simple
    return
  }

  cargandoObservacion.value[colabId] = true
  try {
    const resp = await api.post('/Observacion', {
      colaboracionId: colabId,
      descripcion: descripcion,
      fechaCarga: new Date().toISOString(),
      caseId: caseId.value ? parseInt(caseId.value) : 0
    })

    if (resp.data) {
      // El backend devuelve la observación creada, o a veces envuelto. Ajustar según respuesta real.
      // Asumimos que devuelve el objeto Observacion directamente o dentro de .observacion
      const obsCreada = resp.data.observacion || resp.data;

      if (!colaboracion.observaciones) {
        colaboracion.observaciones = []
      }
      colaboracion.observaciones.push(obsCreada)
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

// Utilidad visual
const formatearFecha = (fecha: string) => {
  if (!fecha) return ''
  return new Date(fecha).toLocaleDateString()
}
</script>

<style scoped>
/* Estilo para simular el borde inicial de Vuetify */
.border-s-primary {
  border-left: 5px solid rgb(25, 118, 210); /* primary color approximation */
}

/* Borde superior sutil para tarjetas de etapa */
.border-t-lg-primary {
  border-top: 3px solid rgba(25, 118, 210, 0.3);
}

.min-h-0 {
  min-height: 0 !important;
}

/* Scroll para lista de observaciones si es muy larga */
.v-list::-webkit-scrollbar {
  width: 4px;
}
.v-list::-webkit-scrollbar-thumb {
  background-color: #e0e0e0;
  border-radius: 4px;
}

/* FIX: Desactivar hover y active state para expansion panel title */
.v-expansion-panel-title:hover {
  background-color: transparent !important;
  color: inherit !important;
  filter: none !important;
  box-shadow: none !important;
}

.v-expansion-panel-title--active {
  background-color: transparent !important;
  color: inherit !important;
}
</style>