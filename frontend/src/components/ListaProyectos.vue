<template>
  <v-container fluid class="pa-4">
    <h1 class="text-h5 text-md-h4 mb-6 font-weight-bold text-primary">Mis Proyectos de Organización</h1>

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
      @click:close="error = null"
    >
      {{ error }}
    </v-alert>

    <!-- Estado vacío -->
    <div v-if="!loading && proyectos.length === 0" class="text-center mt-10 text-grey-darken-1">
      <v-icon icon="mdi-lightbulb-on-outline" size="64" class="mb-2"></v-icon>
      <p class="text-h6">Aún no tienes proyectos registrados.</p>
    </div>

    <div v-if="!loading && proyectos.length > 0">
      
      <!-- 1. Proyectos En Curso -->
      <h2 class="text-h6 mb-3 mt-4 text-secondary">Proyectos En Curso ({{ proyectosAgrupados.enCurso.length }})</h2>
      <v-divider class="mb-4"></v-divider>
      <v-expansion-panels variant="popout" class="mb-8">
        <v-expansion-panel
          v-for="proyecto in proyectosAgrupados.enCurso"
          :key="proyecto.id"
          class="mb-4 elevation-4"
        >
          <v-expansion-panel-title
            class="font-weight-bold text-body-1 bg-white"
            :class="{'border-s-primary': isProyectoCompletable(proyecto) }"
          >
            <div class="d-flex align-center w-100">
              <v-icon icon="mdi-chart-timeline-variant" class="mr-3 text-primary"></v-icon>
              {{ proyecto.nombre }}
              <v-spacer></v-spacer>
              
              <!-- Botón Completar Proyecto (SOLO si es completable) -->
              <v-btn
                v-if="isProyectoCompletable(proyecto)"
                color="primary"
                variant="flat"
                size="small"
                prepend-icon="mdi-check-decagram"
                :loading="loadingProyectoId === proyecto.id"
                @click.stop="completarProyecto(proyecto)"
                class="text-none ml-4"
              >
                Completar Proyecto
              </v-btn>
              
              <v-chip v-else size="small" color="secondary" label class="ml-4">
                 {{ calcularProgreso(proyecto) }}% Etapas completadas
              </v-chip>
            </div>
          </v-expansion-panel-title>

          <v-expansion-panel-text class="pa-4 bg-grey-lighten-4">
            <p class="text-body-2 text-medium-emphasis mb-4">{{ proyecto.descripcion }}</p>

            <h3 class="text-subtitle-1 font-weight-bold mb-3">Etapas del Proyecto:</h3>
            
            <!-- Listado de Etapas (Cards) -->
            <v-row dense>
              <v-col 
                v-for="etapa in proyecto.etapas" 
                :key="etapa.id" 
                cols="12" 
                lg="6"
              >
                <v-card 
                  elevation="1" 
                  :border="etapa.completada ? 'start success' : 'start grey-lighten-1'"
                  class="h-100 pa-3"
                  :class="{'bg-green-lighten-5': etapa.completada}"
                >
                  <v-card-title class="text-subtitle-2 font-weight-bold d-flex align-center pa-0">
                    <v-icon :icon="etapa.completada ? 'mdi-check-circle' : 'mdi-circle-half-full'" 
                            :color="etapa.completada ? 'success' : 'grey-darken-1'" 
                            size="small" 
                            class="mr-2"></v-icon>
                    {{ etapa.nombre }}
                    <v-chip v-if="etapa.completada" size="x-small" color="success" class="ml-2" label>Completada</v-chip>
                    <v-chip v-else size="x-small" color="warning" class="ml-2" label>Pendiente</v-chip>
                  </v-card-title>
                  
                  <v-card-text class="text-caption text-medium-emphasis pa-0 pt-2">
                    <p class="mb-2">{{ etapa.descripcion }}</p>
                    <p class="text-caption text-grey-darken-1">
                      {{ obtenerRangoFechas(etapa.fechaInicio, etapa.fechaFin) }}
                    </p>

                    <!-- Propuestas (Mini-lista) -->
                    <div v-if="etapa.propuestas && etapa.propuestas.length > 0" class="mt-2 pt-2 border-t">
                      <div class="font-weight-bold text-caption mb-1">Propuestas ({{ etapa.propuestas.length }}):</div>
                      <v-list density="compact" class="bg-transparent">
                        <v-list-item
                          v-for="propuesta in etapa.propuestas.slice(0, 3)"
                          :key="propuesta.id"
                          class="pa-0 mb-1"
                          :title="propuesta.descripcion"
                          lines="one"
                        >
                          <template v-slot:prepend>
                            <v-icon :color="colorPropuesta(propuesta)" size="small" class="mr-1">
                              {{ iconoPropuesta(propuesta) }}
                            </v-icon>
                          </template>
                          <template v-slot:append>
                            <v-chip size="x-small" :color="colorPropuesta(propuesta)">
                              {{ estadoPropuesta(propuesta.estado) }}
                            </v-chip>
                          </template>
                        </v-list-item>
                        <v-list-item v-if="etapa.propuestas.length > 3" class="pa-0 text-caption text-center text-grey">
                          ... y {{ etapa.propuestas.length - 3 }} más.
                        </v-list-item>
                      </v-list>
                    </div>
                    <div v-else class="mt-2 text-caption text-grey font-italic">
                        Sin propuestas.
                    </div>

                  </v-card-text>
                </v-card>
              </v-col>
            </v-row>
          </v-expansion-panel-text>
        </v-expansion-panel>
      </v-expansion-panels>

      <!-- 2. Proyectos Completados -->
      <h2 class="text-h6 mb-3 mt-8 text-success">Proyectos Completados ({{ proyectosAgrupados.completados.length }})</h2>
      <v-divider class="mb-4"></v-divider>
      <v-expansion-panels variant="popout" class="mb-8">
        <v-expansion-panel
          v-for="proyecto in proyectosAgrupados.completados"
          :key="proyecto.id"
          class="mb-4 elevation-2 bg-green-lighten-5"
        >
          <v-expansion-panel-title class="font-weight-bold text-body-1 text-success">
            <v-icon icon="mdi-check-all" class="mr-3"></v-icon>
            {{ proyecto.nombre }}
            <v-spacer></v-spacer>
            <v-chip size="small" color="success" label>Completado</v-chip>
          </v-expansion-panel-title>

          <!--<v-expansion-panel-text class="pa-4 bg-white">
             <p class="text-body-2 text-medium-emphasis mb-4">{{ proyecto.descripcion }}</p>
             <
             <div class="text-caption text-grey-darken-1">Este proyecto ha sido completado exitosamente.</div>
          </v-expansion-panel-text>-->
          <v-expansion-panel-text class="pa-4 bg-grey-lighten-4">
            <p class="text-body-2 text-medium-emphasis mb-4">{{ proyecto.descripcion }}</p>

            <h3 class="text-subtitle-1 font-weight-bold mb-3">Etapas del Proyecto:</h3>
            
            <!-- Listado de Etapas (Cards) -->
            <v-row dense>
              <v-col 
                v-for="etapa in proyecto.etapas" 
                :key="etapa.id" 
                cols="12" 
                lg="6"
              >
                <v-card 
                  elevation="1" 
                  :border="etapa.completada ? 'start success' : 'start grey-lighten-1'"
                  class="h-100 pa-3"
                  :class="{'bg-green-lighten-5': etapa.completada}"
                >
                  <v-card-title class="text-subtitle-2 font-weight-bold d-flex align-center pa-0">
                    <v-icon :icon="etapa.completada ? 'mdi-check-circle' : 'mdi-circle-half-full'" 
                            :color="etapa.completada ? 'success' : 'grey-darken-1'" 
                            size="small" 
                            class="mr-2"></v-icon>
                    {{ etapa.nombre }}
                    <v-chip v-if="etapa.completada" size="x-small" color="success" class="ml-2" label>Completada</v-chip>
                    <v-chip v-else size="x-small" color="warning" class="ml-2" label>Pendiente</v-chip>
                  </v-card-title>
                  
                  <v-card-text class="text-caption text-medium-emphasis pa-0 pt-2">
                    <p class="mb-2">{{ etapa.descripcion }}</p>
                    <p class="text-caption text-grey-darken-1">
                      {{ obtenerRangoFechas(etapa.fechaInicio, etapa.fechaFin) }}
                    </p>

                    <!-- Propuestas (Mini-lista) -->
                    <div v-if="etapa.propuestas && etapa.propuestas.length > 0" class="mt-2 pt-2 border-t">
                      <div class="font-weight-bold text-caption mb-1">Propuestas ({{ etapa.propuestas.length }}):</div>
                      <v-list density="compact" class="bg-transparent">
                        <v-list-item
                          v-for="propuesta in etapa.propuestas.slice(0, 3)"
                          :key="propuesta.id"
                          class="pa-0 mb-1"
                          :title="propuesta.descripcion"
                          lines="one"
                        >
                          <template v-slot:prepend>
                            <v-icon :color="colorPropuesta(propuesta)" size="small" class="mr-1">
                              {{ iconoPropuesta(propuesta) }}
                            </v-icon>
                          </template>
                          <template v-slot:append>
                            <v-chip size="x-small" :color="colorPropuesta(propuesta)">
                              {{ estadoPropuesta(propuesta.estado) }}
                            </v-chip>
                          </template>
                        </v-list-item>
                        <v-list-item v-if="etapa.propuestas.length > 3" class="pa-0 text-caption text-center text-grey">
                          ... y {{ etapa.propuestas.length - 3 }} más.
                        </v-list-item>
                      </v-list>
                    </div>
                    <div v-else class="mt-2 text-caption text-grey font-italic">
                        Sin propuestas.
                    </div>

                  </v-card-text>
                </v-card>
              </v-col>
            </v-row>
          </v-expansion-panel-text>
        </v-expansion-panel>
      </v-expansion-panels>
    </div>
    
    <!-- Modal para Confirmación -->
    <v-dialog v-model="showConfirmModal" max-width="500">
      <v-card>
        <v-card-title class="text-h5 primary">Confirmación</v-card-title>
        <v-card-text class="py-4">
          ¿Estás seguro de que deseas completar el proyecto "{{ proyectoACompletar?.nombre }}"?
          <br><br>
          Esta acción es final y notificará a los colaboradores.
        </v-card-text>
        <v-card-actions>
          <v-spacer></v-spacer>
          <v-btn 
            color="grey-darken-1" 
            variant="text" 
            @click="showConfirmModal = false"
            :disabled="loadingProyectoId !== null"
          >
            Cancelar
          </v-btn>
          <v-btn 
            color="primary" 
            variant="flat" 
            @click="confirmarCompletarProyecto" 
            :loading="loadingProyectoId !== null"
          >
            Sí, Completar
          </v-btn>
        </v-card-actions>
      </v-card>
    </v-dialog>
    
  </v-container>
</template>

<script lang="ts">
import { defineComponent, computed } from "vue";
import api from '../api'; // Asumiendo que 'api' está configurado para Axios o fetch

// --- CONSTANTES Y TIPOS ---

const ESTADOS_PROPUESTA = {
  PENDIENTE: 1,
  ACEPTADA: 2,
  RECHAZADA: 3,
  EJECUCION: 4
};

interface Propuesta {
  id: string;
  descripcion: string;
  categoriaColaboracion: number; // enum index
  esParcial: boolean;
  estado: number; // ESTADOS_PROPUESTA
}

interface Etapa {
  id: string;
  nombre: string;
  descripcion: string;
  fechaInicio: string; // DateTime
  fechaFin: string; // DateTime
  requiereColaboracion: boolean;
  completada: boolean; // TRUE si la etapa está finalizada
  propuestas: Propuesta[]; // PropuestasColaboracion
}

interface Proyecto {
  id: string;
  nombre: string;
  descripcion: string;
  organizacionId: number;
  etapas: Etapa[];
  completado: boolean; // TRUE si el proyecto está finalizado
}

// --- COMPONENTE VUE ---

export default defineComponent({
  name: "ProyectosOrganizacion",

  data() {
    return {
      proyectos: [] as Proyecto[],
      loading: false,
      error: null as string | null,
      organizacionId: null as number | null,
      
      // Control de estado para la acción de completar
      loadingProyectoId: null as string | null,
      showConfirmModal: false,
      proyectoACompletar: null as Proyecto | null,
    };
  },

  computed: {
    // Agrupa y ordena los proyectos: En Curso primero, luego Completados
    proyectosAgrupados() {
      const enCurso = this.proyectos.filter(p => !p.completado);
      const completados = this.proyectos.filter(p => p.completado);
      
      // La lógica de ordenamiento del backend ya parece ordenar por Fecha descendente.
      // Aquí nos centramos en separar por estado.
      return { enCurso, completados };
    },
  },

  mounted() {
    this.cargarProyectos();
  },

  methods: {
    // Utilidad para extraer el ID de organización del token
    parseJwt(token: string | null): any {
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

    // 1. Carga de datos del API
    async cargarProyectos() {
      this.loading = true;
      this.error = null;
      try {
        const token = localStorage.getItem('token');
        const decoded = this.parseJwt(token);
        
        if (!decoded || !decoded.sub) {
          throw new Error("No se pudo identificar la organización del usuario.");
        }
        
        this.organizacionId = decoded.sub;

        // GET /Proyecto/porOrganizacion/{userId}
        const response = await api.get(`/Proyecto/porOrganizacion/${this.organizacionId}`);
        
        if (response && response.data) {
          this.proyectos = response.data as Proyecto[];
        }
      } catch (err: any) {
        console.error('Error cargando proyectos:', err);
        this.error = "No se pudieron cargar los proyectos de tu organización.";
      } finally {
        this.loading = false;
      }
    },

    // 2. Lógica para completar el proyecto
    isProyectoCompletable(proyecto: Proyecto): boolean {
      if (proyecto.completado) return false;
      
      // El proyecto es completable si TODAS sus etapas están marcadas como completadas.
      return proyecto.etapas.length > 0 && proyecto.etapas.every(etapa => etapa.completada);
    },
    
    // Inicia el flujo de confirmación para completar el proyecto
    completarProyecto(proyecto: Proyecto) {
      this.proyectoACompletar = proyecto;
      this.showConfirmModal = true;
    },
    
    // Ejecuta la llamada al API después de la confirmación
    async confirmarCompletarProyecto() {
      const proyecto = this.proyectoACompletar;
      if (!proyecto || !proyecto.id) {
        this.showConfirmModal = false;
        return;
      }

      this.loadingProyectoId = proyecto.id;
      
      try {
        // POST /Proyecto/completar-proyecto/{proyectoId}
        const res = await api.post(`/Proyecto/completar-proyecto/${proyecto.id}`);

        // Verificamos que 'res' exista antes de acceder a 'res.status'
        if (res && res.status === 200) {
          // 1. Actualización local: marcar el proyecto como completado
          const index = this.proyectos.findIndex(p => p.id === proyecto.id);
          if (index !== -1) {
            if(this.proyectos[index]){
              this.proyectos[index].completado = true;
            }
          }
          alert(`Proyecto "${proyecto.nombre}" completado exitosamente.`);
        } else {
             alert("Error al completar el proyecto. Respuesta inesperada del servidor.");
        }
      } catch (err: any) {
        console.error(err);
        const msg = err.response?.data?.message || err.response?.data || err.message || "Error desconocido al intentar completar el proyecto.";
        alert(`Error: ${msg}`);
      } finally {
        this.loadingProyectoId = null;
        this.showConfirmModal = false;
        this.proyectoACompletar = null;
      }
    },

    // --- UTILIDADES VISUALES ---

    calcularProgreso(proyecto: Proyecto): number {
      if (!proyecto.etapas || proyecto.etapas.length === 0) return 0;
      
      const completadas = proyecto.etapas.filter(e => e.completada).length;
      return Math.floor((completadas / proyecto.etapas.length) * 100);
    },

    obtenerRangoFechas(inicio: string, fin: string): string {
      const fInicio = new Date(inicio).toLocaleDateString();
      const fFin = new Date(fin).toLocaleDateString();
      return `Desde ${fInicio} hasta ${fFin}`;
    },
    
    estadoPropuesta(estado: number): string {
      switch(estado) {
        case ESTADOS_PROPUESTA.ACEPTADA: return 'Aceptada';
        case ESTADOS_PROPUESTA.RECHAZADA: return 'Rechazada';
        case ESTADOS_PROPUESTA.PENDIENTE: return 'Pendiente';
        default: return 'Desconocido';
      }
    },
    
    colorPropuesta(propuesta: Propuesta): string {
      switch(propuesta.estado) {
        case ESTADOS_PROPUESTA.ACEPTADA: return 'success';
        case ESTADOS_PROPUESTA.RECHAZADA: return 'error';
        default: return 'grey';
      }
    },
    
    iconoPropuesta(propuesta: Propuesta): string {
      switch(propuesta.estado) {
        case ESTADOS_PROPUESTA.ACEPTADA: return 'mdi-handshake';
        case ESTADOS_PROPUESTA.RECHAZADA: return 'mdi-close-circle';
        default: return 'mdi-clock-outline';
      }
    }
  }
});
</script>

<style scoped>
/* Estilo para simular el borde inicial de Vuetify */
.border-s-primary {
  border-left: 5px solid rgb(22, 29, 36); /* primary: #1976D2 */
}

/* Desactivar hover (mouse encima) en el título del panel */
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