<template>
  <v-container cols="12" md="6" outlined>
    <v-row class="mb-1">
      <v-col cols="12" class="d-flex justify-start align-center">
        <h2 class="mr-2">Etapa {{ index + 1 }}</h2>
        <v-btn align-se class="ml-auto" color="secondary" variant="tonal" @click="eliminarEtapa">
          Eliminar
        </v-btn>
      </v-col>
    </v-row>

    <!-- Nombre y Descripción -->
    <v-row>
      <v-col cols="12" md="6">
        <v-text-field v-model="etapa.nombre" label="Nombre de la etapa" outlined dense />
      </v-col>
      <v-col cols="12" md="6">
        <v-text-field v-model="etapa.descripcion" label="Descripción de la etapa" outlined dense />
      </v-col>
    </v-row>

    <!-- Fechas -->
    <v-row>
      <v-col cols="12" md="6">
        <v-menu
          v-model="menuInicio"
          :close-on-content-click="true"
          transition="scale-transition"
          offset-y
          min-width="auto"
        >
          <template v-slot:activator="{ props }">
            <v-text-field v-bind="props" v-model="etapa.fechaInicio" :value="formartearFecha(etapa.fechaInicio)" label="Fecha de Inicio" readonly />
          </template>
          <v-date-picker
            v-model="etapa.fechaInicio"
            :allowed-dates="soloFuturas"
            @update:modelValue="menuInicio = false"
          />
        </v-menu>
      </v-col>
      <v-col cols="12" md="6">
        <v-menu
          v-model="menuFin"
          :close-on-content-click="true"
          transition="scale-transition"
          offset-y
          min-width="auto"
        >
          <template v-slot:activator="{ props }">
            <v-text-field v-bind="props" v-model="etapa.fechaFin" :value="formartearFecha(etapa.fechaFin)" label="Fecha de Fin" readonly />
          </template>
          <v-date-picker
            v-model="etapa.fechaFin"
            :allowed-dates="mayorAlInicio"
            @update:modelValue="menuFin = false"
          />
        </v-menu>
      </v-col>
    </v-row>

    <!-- Colaboraciones -->
    <v-row>
      <v-col cols="12" md="6">
        <v-select
          v-model="etapa.opcionesElegidas"
          :items="colaboracionesParaSelect"
          item-title="title"
          item-value="value"
          label="Tipo de colaboración"
        />
      </v-col><!-- outlined dense -->

      <v-col cols="12" md="6">
        <v-textarea
          v-model="etapa.descripcionColaboracion"
          label="Detalles de la colaboración"
          auto-grow
         /><!-- outlined dense -->
      </v-col>
    </v-row>
  </v-container>
</template>

<script lang="ts">
import { defineComponent } from 'vue'

export default defineComponent({
  name: 'AgregarEtapa',
  props: {
    index: { type: Number, required: true },
    etapa: { type: Object, required: true }
  },
  data() {
    return {
      menuInicio: false,
      menuFin: false,
      opcionesColaboracion: {1:'Económica', 2: 'Materiales', 3: 'Mano de Obra', 4: 'Otra'}
    }
  },
  computed: {
    colaboracionesParaSelect() {
      return Object.entries(this.opcionesColaboracion).map(([key, value]) => {
        return {
          title: value,   
          value: parseInt(key)
        }
      });
    }
  },
  methods: {
    eliminarEtapa() {
      this.$emit('eliminar', this.index)
    },
    soloFuturas(date: unknown): boolean {
      const seleccionada = new Date(String(date))
      const hoy = new Date()
      hoy.setHours(0, 0, 0, 0)
      return seleccionada >= hoy
    },
    mayorAlInicio(date: unknown): boolean {
      const seleccionada = new Date(String(date))
      const inicio = new Date(this.etapa.fechaInicio)
      return seleccionada > inicio
    },
    formartearFecha(fecha: string | Date){
      return new Date(fecha).toLocaleDateString('es-AR');
    }
  }
})
</script>
