<template>
  <v-container fluid>
    <h1 class="text-h5 text-md-h4 mb-4">Propuestas Recibidas (Pendientes)</h1>

    <v-progress-linear
      v-if="loading"
      indeterminate
      color="primary"
      class="mb-4"
    ></v-progress-linear>

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

    <!-- Si no hay propuestas pendientes -->
    <div v-if="!loading && Object.keys(propuestasAgrupadas).length === 0" class="text-center mt-10 text-grey">
      <v-icon icon="mdi-inbox-outline" size="64" class="mb-2"></v-icon>
      <p class="text-h6">No tienes propuestas pendientes de revisión.</p>
    </div>

    <!-- Listado Agrupado por Proyecto -->
    <div v-else>
      <div 
        v-for="(etapasDelProyecto, nombreProyecto) in propuestasAgrupadas" 
        :key="nombreProyecto"
        class="mb-6"
      >
        <!-- Cabecera del Proyecto -->
        <div class="d-flex align-center mb-2 text-primary">
          <v-icon icon="mdi-folder-open" class="mr-2"></v-icon>
          <h2 class="text-h6 font-weight-bold">{{ nombreProyecto }}</h2>
        </div>
        <v-divider class="mb-4 border-opacity-25"></v-divider>

        <!-- Iteración de Etapas dentro del Proyecto -->
        <v-row>
          <v-col 
            v-for="(datosEtapa, etapaId) in etapasDelProyecto" 
            :key="etapaId"
            cols="12"
          >
            <v-card variant="outlined" class="bg-grey-lighten-5">
              <v-card-title class="text-subtitle-1 font-weight-bold d-flex align-center">
                <v-icon icon="mdi-layers-triple" size="small" class="mr-2 text-grey-darken-1"></v-icon>
                Etapa: {{ datosEtapa.nombreEtapa }}
              </v-card-title>
              
              <v-card-text>
                <v-alert 
                  type="info" 
                  variant="text" 
                  density="compact" 
                  class="mb-2 text-caption"
                  icon="mdi-information"
                >
                  Al aceptar una propuesta, las demás propuestas para esta etapa se rechazarán automáticamente.
                </v-alert>

                <!-- Lista de Propuestas individuales para esta etapa -->
                <v-row dense>
                  <v-col 
                    v-for="propuesta in datosEtapa.propuestas" 
                    :key="propuesta.id" 
                    cols="12" 
                    md="6"
                  >
                    <v-card elevation="2" class="mb-2 h-100">
                      <v-card-item>
                        <template v-slot:prepend>
                          <v-avatar color="primary" variant="tonal">
                            <v-icon icon="mdi-account-hard-hat"></v-icon>
                          </v-avatar>
                        </template>
                        <v-card-title class="text-body-1 font-weight-bold">
                          {{ obtenerNombreCategoria(propuesta.categoriaColaboracion) }}
                        </v-card-title>
                        <v-card-subtitle>
                          {{ propuesta.esParcial ? 'Colaboración Parcial' : 'Colaboración Total' }}
                        </v-card-subtitle>
                      </v-card-item>

                      <v-card-text class="pt-2">
                        <p class="text-body-2 mb-3">
                          {{ propuesta.descripcion }}
                        </p>
                        
                        <!-- Listado breve de observaciones previas si existen -->
                         <div v-if="propuesta.observaciones && propuesta.observaciones.length > 0" class="mt-2">
                           <v-divider class="mb-2"></v-divider>
                           <div class="text-caption font-weight-bold text-grey-darken-1">Historial de Observaciones:</div>
                           <ul class="text-caption text-grey ml-4">
                             <li v-for="obs in propuesta.observaciones.slice(0, 2)" :key="obs.id">
                               {{ obs.descripcion }} ({{ obs.fechaRealizacion ? 'Resuelta' : 'Pendiente' }})
                             </li>
                             <li v-if="propuesta.observaciones.length > 2">... y {{ propuesta.observaciones.length - 2 }} más.</li>
                           </ul>
                         </div>
                      </v-card-text>

                      <v-divider></v-divider>

                      <v-card-actions>
                        <v-spacer></v-spacer>
                        <v-btn
                          color="success"
                          variant="elevated"
                          prepend-icon="mdi-check-decagram"
                          :loading="loadingPropuestaId === propuesta.id"
                          @click="aceptarPropuesta(propuesta)"
                        >
                          Aceptar Propuesta
                        </v-btn>
                      </v-card-actions>
                    </v-card>
                  </v-col>
                </v-row>

              </v-card-text>
            </v-card>
          </v-col>
        </v-row>
      </div>
    </div>
  </v-container>
</template>

<script lang="ts">
import { defineComponent } from "vue";
import api from '../api'; 

// Enumeración basada en tu Backend C#
const ESTADOS = {
  PENDIENTE: 1,
  ACEPTADA: 2,
  RECHAZADA: 3
};

interface Propuesta {
  id: string;
  descripcion: string;
  categoriaColaboracion: number; // enum index
  esParcial: boolean;
  estado: number;
  etapaId: string;
  organizacionProponenteId: number;
  proyecto?: string; // Nombre del proyecto (viene del DTO)
  etapa?: {
    id: string;
    nombre: string;
    proyectoId: string;
  };
  observaciones?: any[];
}

export default defineComponent({
  name: "PropuestasRecibidas",

  data() {
    return {
      opcionesColaboracion: ['Económica', 'Materiales', 'Mano de Obra', 'Otra'],
      
      propuestasRaw: [] as Propuesta[], // Lista plana original
      loading: false,
      error: null as string | null,
      loadingPropuestaId: null as string | null,
      organizacionId: null as number | null,
    };
  },

  computed: {
    // Transforma la lista plana en: 
    // { "Nombre Proyecto": { "GuidEtapa": { nombreEtapa: "...", propuestas: [...] } } }
    propuestasAgrupadas() {
      // 1. Filtramos solo las pendientes
      const pendientes = this.propuestasRaw.filter(p => p.estado === ESTADOS.PENDIENTE);
      
      const agrupado: Record<string, Record<string, { nombreEtapa: string, propuestas: Propuesta[] }>> = {};

      pendientes.forEach(p => {
        const nombreProyecto = p.proyecto || 'Proyecto Desconocido';
        const etapaId = p.etapaId;
        const nombreEtapa = p.etapa?.nombre || 'Etapa sin nombre';

        if (!agrupado[nombreProyecto]) {
          agrupado[nombreProyecto] = {};
        }

        if (!agrupado[nombreProyecto][etapaId]) {
          agrupado[nombreProyecto][etapaId] = {
            nombreEtapa: nombreEtapa,
            propuestas: []
          };
        }

        agrupado[nombreProyecto][etapaId].propuestas.push(p);
      });

      return agrupado;
    }
  },

  mounted() {
    this.cargarPropuestasRecibidas();
  },

  methods: {
    parseJwt(token: string | null) {
      if (!token) return null;
      try {
        const base64Url = token.split('.')[1];
        if(base64Url == null) return null
        const base64 = base64Url.replace(/-/g, '+').replace(/_/g, '/');
        const jsonPayload = decodeURIComponent(atob(base64).split('').map(function(c) {
          return '%' + ('00' + c.charCodeAt(0).toString(16)).slice(-2);
        }).join(''));
        return JSON.parse(jsonPayload);
      } catch (e) {
        return null;
      }
    },

    async cargarPropuestasRecibidas() {
      this.loading = true;
      this.error = null;
      try {
        const token = localStorage.getItem('token');
        const decoded = this.parseJwt(token);
        if (!decoded || !decoded.sub) throw new Error("Token inválido");
        
        this.organizacionId = decoded.sub;

        // GET PropuestaColaboracion/recibe/{organizacionId}
        const response = await api.get(`/PropuestaColaboracion/recibe/${this.organizacionId}`);
        
        if (response && response.data) {
          this.propuestasRaw = response.data;
        }
      } catch (err: any) {
        console.error('Error cargando propuestas recibidas:', err);
        this.error = "No se pudieron cargar las propuestas recibidas.";
      } finally {
        this.loading = false;
      }
    },

    async aceptarPropuesta(propuesta: Propuesta) {
      if (!confirm(`¿Estás seguro de aceptar esta propuesta de ${this.obtenerNombreCategoria(propuesta.categoriaColaboracion)}? \n\nEsto rechazará automáticamente las otras propuestas pendientes para la etapa "${propuesta.etapa?.nombre}".`)) {
        return;
      }

      this.loadingPropuestaId = propuesta.id;
      
      try {
        // POST /PropuestaColaboracion/aceptar/{propuestaId}
        const res = await api.post(`/PropuestaColaboracion/aceptar/${propuesta.id}`);

        if (res && res.status === 200) {
          // Lógica de actualización local:
          
          // 1. Marcar la propuesta actual como ACEPTADA (2)
          const indexAceptada = this.propuestasRaw.findIndex(p => p.id === propuesta.id);
          if (indexAceptada !== -1) {
            const propuestaAceptada = this.propuestasRaw[indexAceptada];
            if (propuestaAceptada) {
                propuestaAceptada.estado = ESTADOS.ACEPTADA;
            }
          }

          // 2. Marcar el resto de propuestas DE LA MISMA ETAPA como RECHAZADAS (3)
          this.propuestasRaw.forEach(p => {
             if (p.etapaId === propuesta.etapaId && p.id !== propuesta.id && p.estado === ESTADOS.PENDIENTE) {
               p.estado = ESTADOS.RECHAZADA;
             }
          });

          // Al modificar 'propuestasRaw', la computed 'propuestasAgrupadas' se recalcula sola 
          // y elimina visualmente todo lo que ya no es Pendiente.
        }
      } catch (err: any) {
        console.error(err);
        const msg = err.response?.data || err.message || "Error desconocido";
        // Manejo específico del conflicto 409 (ya existe colaboración)
        if (err.response?.status === 409) {
           alert("Conflicto: " + msg);
        } else {
           alert("Error al aceptar la propuesta: " + msg);
        }
      } finally {
        this.loadingPropuestaId = null;
      }
    },

    obtenerNombreCategoria(index: number) {
      return this.opcionesColaboracion[index] || 'Desconocida';
    }
  }
});
</script>