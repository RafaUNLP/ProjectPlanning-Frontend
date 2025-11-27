<template>
  <v-container fluid>
    <h1 class="text-h5 text-md-h4 mb-4">Agregar Proyecto</h1>

    <v-row class="mb-4">
      <v-col cols="12">
        <v-btn color="primary" @click="showForm = !showForm">
          {{ showForm ? 'Cancelar' : 'Agregar Proyecto' }}
        </v-btn>
      </v-col>
    </v-row>

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
import api from '../api'

interface EtapaLocal {
  nombre: string
  descripcion: string
  fechaInicio: string
  fechaFin: string
  requiereColaboracion: boolean
  opcionesElegidas: string[]
  descripcionColaboracion: string
}

export default defineComponent({
  components: { AgregarEtapa },
  data() {
    return {
      nombreProyecto: '',
      descripcionProyecto: '',
      etapas: [] as EtapaLocal[],
      organizacionId: '' as string,
      showForm: false,
    }
  },
  mounted() {
    this.cargarOrganizacionId()
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
    async cargarOrganizacionId() {
      try {
        const token = localStorage.getItem('token')
        const payload = this.parseJwt(token)
        const userid = payload?.sub || payload?.userid || null
        if (!userid) return

        this.organizacionId = userid
      } catch (error) {
        console.error('Error al cargar organización:', error)
      }
    },
    agregarEtapa() {
      this.etapas.push({
        nombre: '',
        descripcion: '',
        fechaInicio: new Date().toISOString().substring(0, 10),
        fechaFin: '',
        requiereColaboracion: false,
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

        const opcionesColaboracionMap: Record<string, number> = {
          'Económica': 1,
          'Materiales': 2,
          'Mano de Obra': 3,
          'Otra': 4
        };

        const payload = {
          Nombre: this.nombreProyecto,
          Descripcion: this.descripcionProyecto,
          OrganizacionId: this.organizacionId,
          Etapas: this.etapas.map(e => ({
            Nombre: e.nombre,
            Descripcion: e.descripcion,
            FechaInicio: e.fechaInicio,
            FechaFin: e.fechaFin,
            RequiereColaboracion: e.requiereColaboracion,
            CategoriaColaboracion: e.requiereColaboracion && e.opcionesElegidas[0]
              ? opcionesColaboracionMap[e.opcionesElegidas[0]]
              : null,
            DescripcionColaboracion: e.requiereColaboracion ? e.descripcionColaboracion : null
          }))
        }

        const response = await api.post('/Proyecto', payload)
        if (response.status === 200) {
          alert('Proyecto agregado exitosamente');
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
