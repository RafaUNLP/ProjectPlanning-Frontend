<template>
  <v-container fluid>
    <h1 class="text-h5 text-md-h4 mb-4">Mis Proyectos</h1>

    <!-- Listado de proyectos -->
    <v-row v-if="proyectos && proyectos.length">
      <v-col v-for="p in proyectos" :key="p.id" cols="12">
        <v-card class="proyecto-card">
          <v-card-title>
            <div class="d-flex justify-space-between align-center w-100">
              <span>{{ p.nombre }}</span>
              <v-btn
                icon
                size="small"
                @click="toggleProyecto(p.id)"
                :color="proyectosExpandidos.includes(p.id) ? 'primary' : 'default'"
              >
                <v-icon>{{ proyectosExpandidos.includes(p.id) ? 'mdi-chevron-up' : 'mdi-chevron-down' }}</v-icon>
              </v-btn>
            </div>
          </v-card-title>
          
          <v-card-text>
            <div>{{ p.descripcion }}</div>
            <div class="text-caption">Fecha: {{ formatFecha(p.fecha) }}</div>
          </v-card-text>

          <!-- Etapas y Propuestas de Colaboración -->
          <v-expand-transition>
            <div v-if="proyectosExpandidos.includes(p.id)" class="proyecto-detalles pa-4 border-t">
              <VerPropuestasColaboracion 
                :proyecto-id="p.id"
                :etapas="p.etapas || []"
              />
            </div>
          </v-expand-transition>
        </v-card>
      </v-col>
    </v-row>
    <div v-else>
      <p>No hay proyectos para esta organización.</p>
    </div>
  </v-container>
</template>

<script lang="ts">
import { defineComponent } from 'vue'
import VerPropuestasColaboracion from './VerPropuestasColaboracion.vue'
import api from '../api'

export default defineComponent({
  components: { VerPropuestasColaboracion },
  data() {
    return {
      proyectos: [] as any[],
      proyectosExpandidos: [] as string[],
    }
  },
  mounted() {
    this.cargarProyectos()
  },
  methods: {
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
      try {
        const token = localStorage.getItem('token')
        const payload = this.parseJwt(token)
        const userid = payload?.sub || payload?.userid || null
        if (!userid) return

        const resp = await api.get(`/porOrganizacion/${encodeURIComponent(userid)}`)
        if (resp && resp.data) {
          this.proyectos = resp.data || []
        }
      } catch (error) {
        console.error('Error al cargar proyectos:', error)
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
    toggleProyecto(proyectoId: string) {
      const index = this.proyectosExpandidos.indexOf(proyectoId)
      if (index > -1) {
        this.proyectosExpandidos.splice(index, 1)
      } else {
        this.proyectosExpandidos.push(proyectoId)
      }
    }
  }
})
</script>
