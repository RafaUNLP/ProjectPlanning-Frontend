<template>
  <v-container fluid>
    <h1 class="text-h5 text-md-h4 mb-4">Mis Proyectos</h1>

    <v-row class="mb-4">
      <v-col cols="12">
        <v-btn color="primary" @click="showForm = !showForm">
          {{ showForm ? 'Cancelar' : 'Agregar Proyecto' }}
        </v-btn>
      </v-col>
    </v-row>

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

    <!-- Formulario (toggle) -->
    <div v-if="showForm">
      <h2 class="text-h6 mt-4">Agregar un Proyecto</h2>

      <!-- Datos del proyecto -->
      <v-row>
        <v-col cols="12" md="6">
          <v-text-field v-model="nombreProyecto" label="Nombre del proyecto" dense />
        </v-col>
        <v-col cols="12" md="6">
          <v-textarea v-model="descripcionProyecto" label="Descripción del proyecto" dense />
        </v-col>
      </v-row>

      <!-- Etapas -->
      <v-row>
        <v-col cols="12">
          <div v-for="(etapa, index) in etapas" :key="index" class="mb-4">
            <AgregarEtapa
              :index="index"
              :etapa="etapa"
              @eliminar="eliminarEtapa(index)"
            />
          </div>
        </v-col>
      </v-row>

      <!-- Botón agregar etapa -->
      <v-row class="mb-2">
        <v-col cols="12">
          <v-btn color="secondary" @click="agregarEtapa">
            Agregar Etapa
          </v-btn>
        </v-col>
      </v-row>

      <!-- Botón Enviar -->
      <v-row>
        <v-col cols="12">
          <v-btn color='primary' class="mt-4" @click="agregarProyecto">
            Enviar
          </v-btn>
        </v-col>
      </v-row>
    </div>
  </v-container>
</template>

<script lang="ts">
import { defineComponent } from 'vue'
import AgregarEtapa from './AgregarEtapa.vue'
import VerPropuestasColaboracion from './VerPropuestasColaboracion.vue'
import api from '../api'

interface EtapaLocal {
  nombre: string
  descripcion: string
  fechaInicio: string
  fechaFin: string
  opcionesElegidas: string[]
  descripcionColaboracion: string
}

export default defineComponent({
  components: { AgregarEtapa, VerPropuestasColaboracion },
  data() {
    return {
      nombreProyecto: '',
      descripcionProyecto: '',
      etapas: [] as EtapaLocal[],
      organizacionId: '' as string,
      proyectos: [] as any[],
      showForm: false,
      proyectosExpandidos: [] as string[],
    }
  },
  mounted() {
    this.cargarOrganizacionYProyectos()
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
    async cargarOrganizacionYProyectos() {
      try {
        const token = localStorage.getItem('token')
        const payload = this.parseJwt(token)
        const username = payload?.sub || payload?.username || null
        if (!username) return

        const resp = await api.get(`/organizacion/byname/${encodeURIComponent(username)}`)
        if (resp && resp.data) {
          this.organizacionId = resp.data.id
          this.proyectos = resp.data.proyectos || []
        }
      } catch (error) {
        console.error('Error al cargar organización y proyectos:', error)
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
    },
    agregarEtapa() {
      this.etapas.push({
        nombre: '',
        descripcion: '',
        fechaInicio: new Date().toISOString().substring(0, 10),
        fechaFin: '',
        opcionesElegidas: ['Económica'],
        descripcionColaboracion: '',
      })
    },
    eliminarEtapa(index: number) {
      this.etapas.splice(index, 1)
    },
    async agregarProyecto() {
      try {
        if (!this.organizacionId) {
          alert('No se pudo determinar la organización.');
          return
        }

        const payload = {
          Nombre: this.nombreProyecto,
          Descripcion: this.descripcionProyecto,
          OrganizacionId: this.organizacionId,
          Etapas: this.etapas.map(e => ({
            Nombre: e.nombre,
            Descripcion: e.descripcion,
            FechaInicio: e.fechaInicio,
            FechaFin: e.fechaFin,
            RequiereColaboracion: (e.opcionesElegidas && e.opcionesElegidas.length > 0),
            DescripcionColaboracion: e.descripcionColaboracion
          }))
        }

        const response = await api.post('/proyecto', payload)
        if (response.status === 200) {
          alert('Proyecto agregado exitosamente');
          // refrescar lista
          this.cargarOrganizacionYProyectos()
          // limpiar formulario
          this.nombreProyecto = ''
          this.descripcionProyecto = ''
          this.etapas = []
          this.showForm = false
        }
      } catch (error) {
        console.error('Error al agregar el proyecto:', error)
        alert('Error al agregar el proyecto')
      }
    }
  }
})
</script>
