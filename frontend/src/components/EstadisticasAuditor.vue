<template>
  <v-container fluid class="pa-6 bg-grey-lighten-4 h-100">
    <div class="d-flex align-center mb-6">
      <v-icon icon="mdi-chart-box-outline" size="large" color="primary" class="mr-3"></v-icon>
      <h1 class="text-h4 font-weight-bold text-primary">Dashboard de Auditoría</h1>
    </div>

    <v-progress-linear
      v-if="loading"
      indeterminate
      color="primary"
      class="mb-4 rounded"
      height="6"
    ></v-progress-linear>

    <v-alert
      v-if="error"
      type="error"
      variant="tonal"
      class="mb-6 border-s-error"
      border="start"
      closable
    >
      {{ error }}
    </v-alert>

    <div v-if="!loading && stats">
      <v-row>
        <v-col cols="12" md="4">
          <v-card class="h-100 rounded-lg" elevation="3">
            <v-card-item class="bg-blue-darken-2 text-white py-4">
              <template v-slot:prepend>
                <v-icon icon="mdi-trophy-variant" class="mr-2" size="large"></v-icon>
              </template>
              <v-card-title class="text-h6 font-weight-bold">Top ONGs Colaboradoras</v-card-title>
            </v-card-item>
            
            <v-card-text class="pt-6 bg-white">
              <v-list lines="two" class="bg-white">
                <v-list-item
                  v-for="(ong, index) in stats.top3OngsColaboradoras"
                  :key="ong.organizacionId"
                  class="mb-2 rounded"
                >
                  <template v-slot:prepend>
                    <v-avatar :color="getRankColor(index)" size="40" variant="tonal" class="mr-3">
                      <span class="text-h6 font-weight-bold">{{ index + 1 }}</span>
                    </v-avatar>
                  </template>
                  
                  <v-list-item-title class="font-weight-bold text-body-1">{{ ong.nombre }}</v-list-item-title>
                  
                  <v-list-item-subtitle class="text-white-darken-1 mt-1">
                    <v-icon icon="mdi-handshake" size="x-small" class="mr-1"></v-icon>
                    <span class="font-weight-bold text-whitess-darken-3">{{ ong.cantidadColaboraciones }}</span> Colaboraciones
                  </v-list-item-subtitle>
                </v-list-item>
                
                <div v-if="stats.top3OngsColaboradoras.length === 0" class="text-center text-grey mt-4">
                  <v-icon icon="mdi-alert-circle-outline" class="mb-2"></v-icon>
                  <p>No hay datos registrados.</p>
                </div>
              </v-list>
            </v-card-text>
          </v-card>
        </v-col>
  

        <v-col cols="12" md="4">
          <v-card class="h-100 rounded-lg" elevation="3">
            <v-card-item class="bg-deep-purple-darken-1 text-white py-4">
              <template v-slot:prepend>
                <v-icon icon="mdi-tag-multiple" class="mr-2" size="large"></v-icon>
              </template>
              <v-card-title class="text-h6 font-weight-bold">Categoría Más Solicitada</v-card-title>
            </v-card-item>

            <v-card-text class="pt-6 bg-white d-flex flex-column h-100" v-if="stats.categoriaMasPedida">
              <div class="text-center mb-6">
                <v-chip color="deep-blue" variant="flat" size="large" class="font-weight-bold elevation-1">
                
                  {{ stats.categoriaMasPedida.nombreCategoria || 'N/A' }}                
                </v-chip>
                <div class="text-h1 font-weight-black text-deep-purple mb-2 text-uppercase">
                  {{ stats.categoriaMasPedida.cantidadPedidos }} Solicitudes Totales
                </div>
              </div>

              <v-divider class="mb-4"></v-divider>
              
              <div class="text-subtitle-2 font-weight-bold text-grey-darken-2 mb-3 d-flex align-center">
                <v-icon icon="mdi-account-group" size="small" class="mr-2"></v-icon>
                Mayores contribuyentes en esta categoría:
              </div>

              <v-list density="compact" class="bg-grey-lighten-5 rounded pa-2 flex-grow-1">
                <v-list-item
                  v-for="ong in stats.categoriaMasPedida.top3OngsComprometidas"
                  :key="ong.organizacionId"
                  class="mb-1"
                >
                  <template v-slot:prepend>
                    <v-icon icon="mdi-link-variant" size="small" color="deep-purple-lighten-2"></v-icon>
                  </template>
                  <v-list-item-title class="text-body-2 font-weight-medium">
                    {{ ong.nombre }}
                  </v-list-item-title>
                  <template v-slot:append>
                    <v-badge
                      color="deep-purple-lighten-4"
                      text-color="deep-purple-darken-4"
                      :content="ong.cantidadColaboraciones"
                      inline
                    ></v-badge>
                  </template>
                </v-list-item>
              </v-list>
            </v-card-text>
            <v-card-text v-else class="text-center text-grey pt-10 bg-white">
              Sin información de categorías.
            </v-card-text>
          </v-card>
        </v-col>

        <v-col cols="12" md="4">
          <v-card class="h-100 rounded-lg" elevation="3">
            <v-card-item class="bg-teal-darken-2 text-white py-4">
              <template v-slot:prepend>
                <v-icon icon="mdi-timer-outline" class="mr-2" size="large"></v-icon>
              </template>
              <v-card-title class="text-h6 font-weight-bold">Tiempo Promedio</v-card-title>
            </v-card-item>

            <v-card-text class="d-flex flex-column justify-center align-center h-100 bg-white pt-8 pb-8">
              
              <div class="d-flex align-center justify-center mb-4">
                <v-icon icon="mdi-calendar-check" size="64" color="teal-lighten-1" class="mr-4"></v-icon>
                <div class="d-flex flex-column align-start">
                  <div class="text-h2 font-weight-black text-teal-darken-3 lh-1">
                    {{ stats.tiempoPromedioDias }}
                  </div>
                  <div class="text-h5 font-weight-medium text-teal text-uppercase ml-1">
                    Días
                  </div>
                </div>
              </div>

              <v-divider class="w-50 mb-4 border-teal-lighten-4"></v-divider>

              <p class="text-body-2 text-center px-4 text-grey-darken-1" style="max-width: 300px;">
                Duración media calculada desde el inicio de la primera etapa hasta la finalización del proyecto.
              </p>
            </v-card-text>
          </v-card>
        </v-col>
      </v-row>
    </div>
  </v-container>
</template>

<script lang="ts">
import { defineComponent } from 'vue';
import api from '../api';

interface TopOng {
  organizacionId: number;
  nombre: string;
  cantidadColaboraciones: number;
}

interface CategoriaStat {
  nombreCategoria: string;
  cantidadPedidos: number;
  top3OngsComprometidas: TopOng[];
}

interface EstadisticasResponse {
  top3OngsColaboradoras: TopOng[];
  categoriaMasPedida: CategoriaStat;
  tiempoPromedioDias: number;
}

export default defineComponent({
  name: 'EstadisticasAuditor',
  data() {
    return {
      stats: null as EstadisticasResponse | null,
      loading: false,
      error: null as string | null
    }
  },
  mounted() {
    this.cargarEstadisticas();
  },
  methods: {
    async cargarEstadisticas() {
      this.loading = true;
      this.error = null;
      try {
        const response = await api.get('/Estadisticas');
        if (response && response.data) {
          this.stats = response.data;
        }
      } catch (err: any) {
        console.error("Error cargando estadísticas:", err);
        this.error = "No se pudieron cargar las estadísticas. " + (err.response?.data || err.message);
      } finally {
        this.loading = false;
      }
    },
    // Función para dar color de medalla al ranking
    getRankColor(index: number) {
      if (index === 0) return 'amber-darken-2'; // Oro
      if (index === 1) return 'blue-grey-lighten-1'; // Plata
      if (index === 2) return 'brown-lighten-1'; // Bronce
      return 'grey-lighten-1';
    }
  }
});
</script>

<style scoped>
/* Ajuste de altura de línea para que el número grande no tenga tanto espacio arriba/abajo */
.lh-1 {
  line-height: 1;
}

.hover-effect:hover {
  background-color: #f5f5f5;
}

.border-s-error {
  border-left: 4px solid rgb(var(--v-theme-error));
}
</style>