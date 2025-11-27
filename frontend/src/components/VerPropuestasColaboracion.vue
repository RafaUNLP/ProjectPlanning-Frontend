<template>
  <div class="propuestas-container">
    <v-expansion-panels>
      <v-expansion-panel v-for="etapa in etapas" :key="etapa.id">
        <template v-slot:title>
          <div class="etapa-header">
            <span class="etapa-nombre">{{ etapa.nombre }}</span>
            <v-chip
              v-if="etapa.requiereColaboracion"
              size="small"
              color="primary"
              label
            >
              Requiere Colaboración
            </v-chip>
            <v-chip
              v-else
              size="small"
              color="grey"
              label
            >
              No requiere Colaboración
            </v-chip>
          </div>
        </template>

        <v-card-text>
          <div class="etapa-details mb-4">
            <p><strong>Descripción:</strong> {{ etapa.descripcion }}</p>
            <p>
              <strong>Fechas:</strong>
              {{ formatFecha(etapa.fechaInicio) }} - {{ formatFecha(etapa.fechaFin) }}
            </p>
          </div>

          <!-- Propuestas de Colaboración -->
          <div v-if="etapa.requiereColaboracion">
            <h4 class="mb-3">Propuestas de Colaboración</h4>
            
            <v-list v-if="propuestasPorEtapa[etapa.id]?.length">
              <v-list-item
                v-for="propuesta in propuestasPorEtapa[etapa.id]"
                :key="propuesta.id"
                class="mb-2"
              >
                <template v-slot:prepend>
                  <v-icon
                    :color="getEstadoColor(propuesta.estado)"
                  >
                    {{ getEstadoIcon(propuesta.estado) }}
                  </v-icon>
                </template>

                <v-list-item-title>
                  {{ propuesta.organizacionProponente?.nombre || 'Organización Desconocida' }}
                </v-list-item-title>

                <v-list-item-subtitle>
                  <span class="text-caption">
                    {{ propuesta.descripcion }}
                  </span>
                </v-list-item-subtitle>

                <template v-slot:append>
                  <v-chip
                    size="small"
                    :color="getEstadoColor(propuesta.estado)"
                    :text-color="propuesta.estado === 'Pendiente' ? 'black' : 'white'"
                  >
                    {{ propuesta.estado }}
                  </v-chip>
                </template>
              </v-list-item>
            </v-list>

            <v-alert
              v-else
              type="info"
              text="No hay propuestas de colaboración para esta etapa"
              class="mt-3"
            />
          </div>

          <v-alert
            v-else
            type="info"
            text="Esta etapa no requiere colaboración"
            class="mt-3"
          />
        </v-card-text>
      </v-expansion-panel>
    </v-expansion-panels>
  </div>
</template>

<script lang="ts">
import type { PropType } from 'vue'
import { defineComponent } from 'vue'
import api from '../api'

interface Etapa {
  id: string
  nombre: string
  descripcion: string
  fechaInicio: string
  fechaFin: string
  requiereColaboracion: boolean
}

interface PropuestaColaboracion {
  id: string
  etapaId: string
  descripcion: string
  categoriaColaboracion: string
  esParcial: boolean
  estado: string
  organizacionProponente?: {
    id: string
    nombre: string
  }
}

export default defineComponent({
  props: {
    proyectoId: {
      type: String as PropType<string>,
      required: true
    },
    etapas: {
      type: Array as PropType<Etapa[]>,
      required: true
    }
  },
  data() {
    return {
      propuestasPorEtapa: {} as Record<string, PropuestaColaboracion[]>,
      cargando: false
    }
  },
  mounted() {
    this.cargarPropuestas()
  },
  methods: {
    async cargarPropuestas() {
      try {
        this.cargando = true
        const response = await api.get(`/PropuestaColaboracion/proyecto/${this.proyectoId}`)
        
        if (response && response.data) {
          // Agrupar propuestas por etapa
          this.propuestasPorEtapa = {}
          response.data.forEach((propuesta: PropuestaColaboracion) => {
            if (!this.propuestasPorEtapa[propuesta.etapaId]) {
              this.propuestasPorEtapa[propuesta.etapaId] = []
            }
            const etapaArray = this.propuestasPorEtapa[propuesta.etapaId]
            if (etapaArray) {
              etapaArray.push(propuesta)
            }
          })
        }
      } catch (error) {
        console.error('Error al cargar propuestas de colaboración:', error)
      } finally {
        this.cargando = false
      }
    },
    formatFecha(fecha: string | null) {
      if (!fecha) return ''
      try {
        return new Date(fecha).toLocaleDateString('es-AR')
      } catch (e) {
        return ''
      }
    },
    getEstadoColor(estado: string) {
      switch (estado) {
        case 'Aceptada':
          return 'success'
        case 'Rechazada':
          return 'error'
        case 'Pendiente':
        default:
          return 'warning'
      }
    },
    getEstadoIcon(estado: string) {
      switch (estado) {
        case 'Aceptada':
          return 'mdi-check-circle'
        case 'Rechazada':
          return 'mdi-close-circle'
        case 'Pendiente':
        default:
          return 'mdi-clock-outline'
      }
    }
  }
})
</script>

<style scoped>
.propuestas-container {
  width: 100%;
}

.etapa-header {
  display: flex;
  align-items: center;
  gap: 12px;
  width: 100%;
}

.etapa-nombre {
  font-weight: 500;
  flex: 1;
}

.etapa-details {
  background-color: rgba(0, 0, 0, 0.02);
  padding: 12px;
  border-radius: 4px;
  margin-bottom: 16px;
}

.etapa-details p {
  margin: 8px 0;
}
</style>
