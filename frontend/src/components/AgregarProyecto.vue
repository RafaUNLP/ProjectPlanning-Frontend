<template>
  <v-container fluid>
  
    <h1 class="text-h5 text-md-h4 mb-4">Agregar un Proyecto</h1>

    <!-- Datos del proyecto -->
    <v-row>
      <v-col cols="12" md="6">
        <v-text-field
          v-model="nombreProyecto"
          label="Nombre del proyecto"
          dense
        />
      </v-col>

      <v-col cols="12" md="6">
        <v-textarea
          v-model="descripcionProyecto"
          label="Descripción del proyecto"
          dense
        />
      </v-col>
    </v-row>

    <!-- Etapas -->
    <v-row>
      <v-col cols="12">
        <div
          v-for="(etapa, index) in etapas"
          :key="index"
          class="mb-4"
        >
        <AgregarEtapa :index="index" @eliminar="eliminarEtapa(index)" />
        </div>
      </v-col>
    </v-row>

    <!-- Botón agregar etapa -->
    <v-row class="mb-2">
      <v-col cols="12">
        <v-btn color="primary" @click="agregarEtapa">
          Agregar Etapa
        </v-btn>
      </v-col>
    </v-row>

    <!-- Botón Enviar -->
    <v-row>
      <v-col cols="12">
        <v-btn class="mt-4" @click="agregarProyecto">
          Enviar
        </v-btn>
      </v-col>
    </v-row>
  
  </v-container>
</template>

<script lang="ts">
import { defineComponent } from 'vue'
import AgregarEtapa from './AgregarEtapa.vue'
import api from '../api';

interface Etapa {
  nombre: string
  descripcion: string
  fechaInicio: Date
  fechaFin: Date
}

export default defineComponent({
  components: { AgregarEtapa },
  data() {
    return {
      nombreProyecto: '',
      descripcionProyecto: '',
      etapas: [] as Etapa[],
    }
  },
  methods: {
    agregarEtapa() {
      this.etapas.push({ 
        nombre: 'hola1', 
        descripcion: 'unaDescripcion',
        fechaInicio: new Date(),
        fechaFin: new Date("2025-10-04")})
    },
    eliminarEtapa(index: number) {
      this.etapas.splice(index, 1)
    },
    async agregarProyecto(){
      try{
        const payload = {
          Nombre: this.nombreProyecto,
          Descripcion: this.descripcionProyecto,
          Etapas: this.etapas
        }

        const response = await api.post('/proyecto', payload); 
        console.log('Proyecto agregado:', response.data);
        alert('Proyecto agregado exitosamente');
        
        // Limpiar el formulario

        this.nombreProyecto = '';
        this.descripcionProyecto = '';
        this.etapas = [];
      }
      catch(error: any){
        console.error('Error al agregar el proyecto:', error);
      }
    }
  },
})
</script>
