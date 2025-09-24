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
    <v-row>
      <v-col cols="12">
        <v-btn color="primary" @click="agregarEtapa">
          Agregar Etapa
        </v-btn>
      </v-col>
    </v-row>
  </v-container>
</template>

<script lang="ts">
import { defineComponent } from 'vue'
import AgregarEtapa from './AgregarEtapa.vue'

export default defineComponent({
  components: { AgregarEtapa },
  data() {
    return {
      nombreProyecto: '',
      descripcionProyecto: '',
      etapas: [] as { nombre: string; descripcion: string }[],
    }
  },
  methods: {
    agregarEtapa() {
      this.etapas.push({ nombre: '', descripcion: '' })
    },
    eliminarEtapa(index: number) {
      this.etapas.splice(index, 1)
    },
  },
})
</script>
