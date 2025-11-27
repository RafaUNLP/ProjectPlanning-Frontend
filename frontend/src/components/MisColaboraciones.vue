<template>
  <v-container fluid>
    <h1 class="text-h5 text-md-h4 mb-4">Mis Propuestas de Colaboración</h1>

    <!-- Filtros de Estado (Tabs) -->
    <v-tabs
      v-model="tabActiva"
      color="primary"
      align-tabs="start"
      class="mb-6 border-b"
    >
      <v-tab :value="ESTADOS.PENDIENTE">Pendientes</v-tab>
      <v-tab :value="ESTADOS.ACEPTADA">Aceptadas</v-tab>
      <v-tab :value="ESTADOS.RECHAZADA">Rechazadas</v-tab>
    </v-tabs>

    <!-- Loading -->
    <v-progress-linear
      v-if="loading"
      indeterminate
      color="primary"
      class="mb-4"
    ></v-progress-linear>

    <!-- Error -->
    <v-alert
      v-if="error"
      type="error"
      variant="tonal"
      class="mb-4"
      closable
      @click:close="error = null"
    >
      {{ error }}
    </v-alert>

    <!-- Lista de Propuestas -->
    <v-row v-if="!loading && propuestasFiltradas.length > 0">
      <v-col 
        v-for="propuesta in propuestasFiltradas" 
        :key="propuesta.id" 
        cols="11"
      >
        <v-card elevation="2" :border="esAceptada(propuesta) ? 'start' : false" :class="claseBorde(propuesta)">
          
          <!-- Cabecera de la tarjeta -->
          <v-card-item>
            <template v-slot:prepend>
              <v-icon :color="colorIcono(propuesta)" :icon="iconoEstado(propuesta)"></v-icon>
            </template>
            <v-card-title>
               Etapa: {{ propuesta.etapa?.nombre || 'Sin nombre' }}
            </v-card-title>
            <v-card-subtitle>
              Categoría: {{ obtenerNombreCategoria(propuesta.categoriaColaboracion) }}
            </v-card-subtitle>
            
            <template v-slot:append>
              <v-chip size="medium<" :color="colorIcono(propuesta)" label>
                {{ propuesta.esParcial ? 'Parcial' : 'Total' }}
              </v-chip>
            </template>
          </v-card-item>

          <v-card-text>
            <p class="text-body-1 mb-2"><strong>Mi propuesta:</strong> {{ propuesta.descripcion }}</p>
            <!--<p class="text-caption text-grey">Proyecto asociado: {{ propuesta.etapa?.proyecto?.nombre || 'Cargando...' }}</p>-->

            <!-- SECCIÓN: Solo visible si es ACEPTADA -->
            <div v-if="esAceptada(propuesta)" class="mt-4">
              <v-divider class="mb-4"></v-divider>
              
              <!-- 1. Acción de Completar Etapa -->
              <div class="d-flex align-center justify-space-between bg-grey-lighten-4 pa-3 rounded mb-4">
                <div>
                  <!--<div class="text-subtitle-2 font-weight-bold">Estado de la Etapa</div>-->
                  <div class="text-caption">
                    {{ estaEtapaCompletada(propuesta) ? 'Estado: La etapa ha sido completada.' : 'Estado: Pendiente de realización.' }}
                  </div>
                </div>
                
                <v-btn
                  v-if="!estaEtapaCompletada(propuesta)"
                  color="success"
                  size="small"
                  v-tooltip="'Marcar como resuelta'"
                  prepend-icon="mdi-check-all"
                  :loading="loadingEtapaId === propuesta.etapaId"
                  @click="completarEtapa(propuesta)"
                >
                  Marcar como Realizada
                </v-btn>
                <v-icon v-else color="success" icon="mdi-checkbox-marked-circle" size="large"></v-icon>
              </div>

              <!-- 2. Listado de Observaciones -->
              <div v-if="propuesta.observaciones && propuesta.observaciones.length > 0">
                <div class="text-subtitle-1 mb-2 font-weight-medium">Observaciones / Requerimientos</div>
                
                <v-list density="compact" class="bg-transparent">
                  <v-list-item
                    v-for="obs in propuesta.observaciones"
                    :key="obs.id"
                    class="mb-2 border rounded bg-white"
                    lines="two"
                  >
                    <template v-slot:prepend>
                       <v-icon 
                        :icon="obs.fechaRealizacion ? 'mdi-check-circle' : 'mdi-alert-circle-outline'"
                        :color="obs.fechaRealizacion ? 'success' : 'warning'"
                        class="mr-2"
                      ></v-icon>
                    </template>

                    <v-list-item-title class="font-weight-bold">
                      {{ obs.descripcion || 'Observación sin descripción' }}
                    </v-list-item-title>
                    <v-list-item-subtitle>
                      {{ obs.fechaRealizacion ? 'Resuelta el ' + formatearFecha(obs.fechaRealizacion) : 'Pendiente de resolución' }}
                    </v-list-item-subtitle>

                    <template v-slot:append>
                    <v-btn
                      v-if="!obs.fechaRealizacion"
                      color="success"
                      variant="flat"
                      size="large"
                      icon
                      v-tooltip="'Marcar observación como resuelta'"
                      :loading="loadingObservacionId === obs.id"
                      @click="resolverObservacion(obs)"
                    >
                      <v-icon
                        class="ma-0" 
                      >
                        mdi-check
                      </v-icon>
                    </v-btn>
                  </template>

                  </v-list-item>
                </v-list>
              </div>
              <div v-else class="text-caption text-grey font-italic mt-2">
                No hay observaciones registradas para esta colaboración.
              </div>
            </div>
            <!-- FIN SECCIÓN ACEPTADA -->

          </v-card-text>
        </v-card>
      </v-col>
    </v-row>

    <!-- Estado Vacío -->
    <div v-if="!loading && propuestasFiltradas.length === 0" class="text-center mt-10 text-grey">
      <v-icon icon="mdi-file-document-outline" size="64" class="mb-2"></v-icon>
      <p class="text-h6">No tienes propuestas en estado "{{ nombreEstadoActual }}"</p>
    </div>

  </v-container>
</template>

<script lang="ts">
import { defineComponent } from "vue";
import api from '../api'; // Ajusta la ruta según tu estructura

// Ajusta estos valores según tu Enum en C#
const ESTADOS = {
  PENDIENTE: 1,
  ACEPTADA: 2,
  RECHAZADA: 3
};

export default defineComponent({
  name: "MisColaboraciones",

  data() {
    return {
      ESTADOS,
      tabActiva: ESTADOS.ACEPTADA, // Por defecto mostramos las aceptadas que es donde hay acción
      opcionesColaboracion: ['Económica', 'Materiales', 'Mano de Obra', 'Otra'],
      
      propuestas: [] as any[],
      loading: false,
      error: null as string | null,
      
      // Control de loading individual para botones
      loadingEtapaId: null as string | number | null,
      loadingObservacionId: null as number | null,
      
      organizacionId: null as number | null,
    };
  },

  computed: {
    propuestasFiltradas() {
      // Filtra el array total basado en el tab seleccionado
      return this.propuestas.filter(p => p.estado === this.tabActiva);
    },
    nombreEstadoActual() {
      switch(this.tabActiva) {
        case ESTADOS.PENDIENTE: return 'Pendiente';
        case ESTADOS.ACEPTADA: return 'Aceptada';
        case ESTADOS.RECHAZADA: return 'Rechazada';
        default: return '';
      }
    }
  },

  mounted() {
    this.cargarPropuestas();
  },

  methods: {
    parseJwt(token: string | null) {
      if (!token) return null;
      try {
        const base64Url = token.split('.')[1];
        if (!base64Url) return null
        const base64 = base64Url.replace(/-/g, '+').replace(/_/g, '/');
        const jsonPayload = decodeURIComponent(atob(base64).split('').map(function(c) {
          return '%' + ('00' + c.charCodeAt(0).toString(16)).slice(-2);
        }).join(''));
        return JSON.parse(jsonPayload);
      } catch (e) {
        return null;
      }
    },

    async cargarPropuestas() {
      this.loading = true;
      this.error = null;
      try {
        const token = localStorage.getItem('token');
        const decoded = this.parseJwt(token);
        
        if (!decoded || !decoded.sub) {
          throw new Error("No se pudo identificar la organización del usuario.");
        }
        
        this.organizacionId = decoded.sub;

        // GET PropuestaColaboracion/organizacion/{organizacionId}
        const response = await api.get(`/PropuestaColaboracion/propone/${this.organizacionId}`);
        
        if (response && response.data) {
          this.propuestas = response.data;
        }
      } catch (err: any) {
        console.error('Error cargando propuestas:', err);
        this.error = "No se pudieron cargar tus propuestas de colaboración.";
      } finally {
        this.loading = false;
      }
    },

    // --- LÓGICA DE NEGOCIO ---

    esAceptada(propuesta: any) {
      return propuesta.estado === ESTADOS.ACEPTADA;
    },

    // Verifica si la etapa ya fue realizada (usando Etapa.Completada o un flag de fecha)
    estaEtapaCompletada(propuesta: any) {
      // El backend actualiza etapa.Completada = true. 
      // Si usas FechaRealizacion en la etapa, ajusta esta lógica.
      return propuesta.etapa?.completada === true || propuesta.etapa?.fechaRealizacion != null;
    },

    async completarEtapa(propuesta: any) {
      if (!propuesta.etapaId) return;

      this.loadingEtapaId = propuesta.etapaId;
      try {
        // POST /Proyecto/completar-etapa/{etapaId}
        const res = await api.post(`/Proyecto/completar-etapa/${propuesta.etapaId}`);
        
        if (res.status === 200) {
          // Actualización optimista local
          if (propuesta.etapa) {
            propuesta.etapa.completada = true;
            propuesta.etapa.fechaRealizacion = new Date().toISOString(); // Para reflejar cambio visual
          }
          // Opcional: mostrar notificación de éxito
        }
      } catch (err: any) {
        console.error(err);
        alert("Error al completar la etapa: " + (err.response?.data || err.message));
      } finally {
        this.loadingEtapaId = null;
      }
    },

    async resolverObservacion(observacion: any) {
      this.loadingObservacionId = observacion.id;
      
      // Construimos el objeto DTO que espera el endpoint
      const observacionDTO = {
        id: observacion.id,
        caseId: observacion.caseId
      };

      try {
        // PUT /Observacion
        const res = await api.put(`/Observacion`, observacionDTO);

        if (res.status === 200) {
          // Actualización local
          observacion.fechaRealizacion = new Date().toISOString();
        }
      } catch (err: any) {
        console.error(err);
        alert("Error al resolver la observación: " + (err.response?.data || err.message));
      } finally {
        this.loadingObservacionId = null;
      }
    },

    // --- UTILIDADES VISUALES ---

    obtenerNombreCategoria(index: number) {
      return this.opcionesColaboracion[index] || 'Desconocida';
    },

    formatearFecha(fecha: string) {
      if (!fecha) return '';
      return new Date(fecha).toLocaleDateString();
    },

    colorIcono(propuesta: any) {
      switch(propuesta.estado) {
        case ESTADOS.ACEPTADA: return 'success';
        case ESTADOS.RECHAZADA: return 'error';
        default: return 'grey';
      }
    },

    iconoEstado(propuesta: any) {
      switch(propuesta.estado) {
        case ESTADOS.ACEPTADA: return 'mdi-handshake';
        case ESTADOS.RECHAZADA: return 'mdi-handshake-outline'; // o mdi-cancel
        default: return 'mdi-clock-outline';
      }
    },
    
    claseBorde(propuesta: any) {
        if(this.esAceptada(propuesta)) return 'border-success-start';
        return '';
    }
  }
});
</script>

<style scoped>
/* Estilo personalizado para resaltar tarjetas aceptadas con un borde izquierdo verde */
.border-success-start {
  border-left: 4px solid rgb(var(--v-theme-success)) !important;
}
</style>