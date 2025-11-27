<template>
  <v-container fluid>
    <h1 class="text-h5 text-md-h4 mb-4">Etapas que requieren colaboración</h1>

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
    >
      {{ error }}
    </v-alert>

    <v-row v-if="!loading && proyectos.length > 0">
      <v-col 
        v-for="proyecto in proyectos" 
        :key="proyecto.id" 
        cols="12"
      >
        <v-card elevation="2">
          <v-card-title class="text-h6 font-weight-bold text-white">
            {{ proyecto.nombre }}
          </v-card-title>
          
          <v-card-subtitle class="mb-2 text-white">
            {{ proyecto.descripcion }}
          </v-card-subtitle>

          <!--<v-divider></v-divider>-->

          <v-card-text>
            <div v-if="!proyecto.etapas || proyecto.etapas.length === 0" class="text-grey">
              <v-icon icon="mdi-information-outline" size="small" class="mr-1"></v-icon>
              No quedan etapas disponibles para colaborar en este proyecto.
            </div>

            <div 
              v-for="etapa in proyecto.etapas" 
              :key="etapa.id" 
              class="mb-4"
            >
              <v-sheet border rounded class="pa-4 bg-grey-lighten-5">
                <div class="d-flex flex-column flex-md-row justify-space-between align-start align-md-center">
                  <div>
                    <div class="text-subtitle-1 font-weight-bold mb-1">
                      {{ etapa.nombre }}
                    </div>
                    <div class="text-subtitle-2 text-medium-emphasis">
                      {{ etapa.descripcion }}
                    </div>
                  </div>

                  <div class="d-flex gap-2 mt-3 mt-md-0">
                    <v-btn
                      color="primary"
                      variant="flat"
                      size="small"
                      prepend-icon="mdi-handshake"
                      @click="abrirModal(etapa, false)"
                    >
                      Postularme (Total)
                    </v-btn>

                    <v-btn
                      color="warning"
                      variant="flat"
                      size="small"
                      prepend-icon="mdi-handshake-outline"
                      @click="abrirModal(etapa, true)"
                    >
                      Postularme (Parcial)
                    </v-btn>
                  </div>
                </div>
              </v-sheet>
            </div>
          </v-card-text>
        </v-card>
      </v-col>
    </v-row>

    <div v-if="!loading && proyectos.length === 0" class="text-center mt-10 text-grey">
      <v-icon icon="mdi-folder-open-outline" size="large" class="mb-2"></v-icon>
      <p>No hay proyectos que requieran colaboraciones actualmente.</p>
    </div>

    <v-dialog v-model="showModal" max-width="500px" persistent>
      <v-card>
        <v-card-title class="text-h5 bg-primary text-white">
          Proponer Colaboración
        </v-card-title>

        <v-card-text >
          <p class="mb-4 text-body-1">
            Vas a postularte para la etapa: <strong>{{ etapaSeleccionada?.nombre }}</strong>
          </p>
          
          <v-chip
            class="mb-4"
            :color="'warning'"
            size="small"
            label
          >
            {{ propuesta.esParcial ? "Colaboración Parcial" : "Colaboración Total" }}
          </v-chip>

          <v-form ref="formPropuesta">
            <v-row>
              <v-col cols="12" md="10">
                <v-text-field 
                  v-model="propuesta.descripcion"
                  label="Detalles de la colaboración" 
                  dense
                  style="color: black"
                />
              </v-col>
            </v-row>

            <v-row>
            <div class="mb-4 mt-2">
              <v-chip
                color="info"
                variant="flat"
                size="default"
                prepend-icon="mdi-cash-multiple"
              >
                Tipo de colaboración requerida: {{ propuesta.categoriaColaboracion }}
              </v-chip>
            </div>
          </v-row>
          </v-form>
        </v-card-text>

        <v-card-actions class="justify-end pa-4">
          <v-btn
            color="grey-darken-1"
            variant="text"
            @click="showModal = false"
          >
            Cancelar
          </v-btn>
          <v-btn
            color="primary"
            variant="elevated"
            @click="enviarPropuesta"
            :loading="loadingPropuesta"
          >
            Enviar Propuesta
          </v-btn>
        </v-card-actions>
      </v-card>
    </v-dialog>

  </v-container>
</template>

<script lang="ts">
import { defineComponent } from "vue";
import api from '../api'

interface Propuesta {
  descripcion: string;
  categoriaColaboracion: string;
  esParcial: boolean;
  organizacionProponenteId: number | string | null;
}

export default defineComponent({
  name: "VerEtapasParaColaborar",

  data() {
    return {
      // Las categorías están 0-indexed, si el backend usa 1-indexed, hay que restar 1 al índice
      opcionesColaboracion: ['Económica', 'Materiales', 'Mano de Obra', 'Otra'], 
      organizacionId: null as number | null,
      proyectos: [] as any[],
      loading: false,
      loadingPropuesta: false, // Loading específico para el botón del modal
      error: null as string | null,

      showModal: false,
      etapaSeleccionada: null as any,

      propuesta: {
        etapaId: "",
        descripcion: "",
        categoriaColaboracion: "",
        esParcial: false,
        organizacionProponenteId: null as number | string | null,
      } as Propuesta,
    };
  },

  mounted() {
    this.cargarProyectos();
  },

  methods: {
    // Método auxiliar para decodificar el token (copiado de tu referencia para consistencia)
    parseJwt(token: string | null) {
      if (!token) return null
      try {
        const base64Url = token.split('.')[1]
        if (!base64Url) return null
        const base64 = base64Url.replace(/-/g, '+').replace(/_/g, '/')
        const jsonPayload = decodeURIComponent(atob(base64).split('').map(function(c) {
          return '%' + ('00' + c.charCodeAt(0).toString(16)).slice(-2)
        }).join(''))
        return JSON.parse(jsonPayload)
      } catch (e) {
        return null
      }
    },

    async cargarProyectos() {
      this.loading = true;
      this.error = null;
      try {
        this.organizacionId = (this.parseJwt(localStorage.getItem('token')))?.sub
        const response = await api.get(`/Proyecto/requierenColaboraciones`)
        if (response && response.data) {
          this.proyectos = response.data.filter((p:any) => p.organizacionId != this.organizacionId)//que NO sean de mis proyectos
        }
      } catch (err: any) {
        console.error('Error al cargar propuestas:', err)
        this.error = "Hubo un error al cargar los proyectos disponibles.";
      } finally {
        this.loading = false;
      }
    },

    abrirModal(etapa: any, esParcial: boolean) {
      this.etapaSeleccionada = etapa;
      
      // Asumimos que etapa.categoriaColaboracion contiene el índice numérico (0, 1, 2, 3...) 
      // de la categoría de colaboración requerida para esta etapa.
      const categoriaIndex = etapa.categoriaColaboracion; 
      
      // Obtener el string de la categoría. Si el backend usa 1-based (1-4) como tu referencia, 
      // y nuestro array es 0-based (0-3), restamos 1.
      // Si ya es 0-based, el índice es correcto. Para ser seguro, asumo que el backend usa el índice del array 0-based.
      const categoriaString = this.opcionesColaboracion[categoriaIndex] || this.opcionesColaboracion[0] || '';

      this.propuesta = {
        descripcion: "",
        categoriaColaboracion: categoriaString, // Se guarda el STRING para DISPLAY
        esParcial: esParcial,
        organizacionProponenteId: this.organizacionId, 
      };
      this.showModal = true;
    },

    async enviarPropuesta() {
      if (!this.propuesta.descripcion) {
        alert("Por favor completa los detalles de la colaboración.");
        return;
      }

      this.loadingPropuesta = true;
      
      const body = {
        etapaId: this.etapaSeleccionada.id,
        organizacionProponenteId: this.propuesta.organizacionProponenteId,
        descripcion: this.propuesta.descripcion,
        // ENVIAMOS EL ÍNDICE NUMÉRICO ORIGINAL DE LA ETAPA (que está en etapaSeleccionada)
        categoriaColaboracion: this.etapaSeleccionada.categoriaColaboracion,
        esParcial: this.propuesta.esParcial,
      };

      try {
        // Usamos la instancia 'api' para mantener consistencia con headers y baseURL
        const res = await api.post("/PropuestaColaboracion", body);

        if (res.status === 200 || res.status === 201) {
          alert("¡Propuesta enviada exitosamente!");
          this.showModal = false;
          this.cargarProyectos(); // Recargar lista para ver cambios si fuera necesario
        } 
      } catch (err: any) {
        console.error(err);
        const mensaje = err.response?.data || err.message || "Error desconocido";
        alert("Error al enviar la propuesta: " + mensaje);
      } finally {
        this.loadingPropuesta = false;
      }
    },
  },
});
</script>

<style scoped>
/* Utilidad pequeña para espaciado en flexbox si gap no funciona en navegadores muy viejos, 
   aunque Vuetify 3 ya usa flex gap moderno */
.gap-2 {
  gap: 8px;
}
</style>