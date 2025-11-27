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

    <v-row>
      <v-col cols="12" md="6">
        <v-text-field v-model="etapa.nombre" label="Nombre de la etapa" outlined dense />
      </v-col>
      <v-col cols="12" md="6">
        <v-text-field v-model="etapa.descripcion" label="Descripción de la etapa" outlined dense />
      </v-col>
    </v-row>

    <v-row>
      <v-col cols="12" md="6">
        <v-menu
          v-model="menuInicio"
          :close-on-content-click="false"
          transition="scale-transition"
          offset-y
          min-width="auto"
        >
          <template v-slot:activator="{ props }">
            <v-text-field 
              v-bind="props" 
              :model-value="formartearFecha(etapa.fechaInicio)" 
              label="Fecha de Inicio" 
              readonly 
              append-inner-icon="mdi-calendar"
            />
          </template>
          <v-date-picker
            v-model="fechaInicioPicker"
            :allowed-dates="soloFuturas"
            color="primary"
          />
        </v-menu>
      </v-col>
      <v-col cols="12" md="6">
        <v-menu
          v-model="menuFin"
          :close-on-content-click="false"
          transition="scale-transition"
          offset-y
          min-width="auto"
        >
          <template v-slot:activator="{ props }">
            <v-text-field 
              v-bind="props" 
              :model-value="formartearFecha(etapa.fechaFin)" 
              label="Fecha de Fin" 
              readonly 
              append-inner-icon="mdi-calendar"
            />
          </template>
          <v-date-picker
            v-model="fechaFinPicker"
            :allowed-dates="mayorAlInicio"
            color="primary"
          />
        </v-menu>
      </v-col>
    </v-row>

    <v-row>
      <v-col cols="12">
        <v-switch
          v-model="etapa.requiereColaboracion"
          label="Requiere colaboración"
          color="primary"
          hide-details
        ></v-switch>
      </v-col>
    </v-row>

    <v-row v-if="etapa.requiereColaboracion">
      <v-col cols="12" md="6">
        <v-select
          v-model="etapa.opcionesElegidas"
          :items="colaboracionesParaSelect"
          item-title="title"
          item-value="value"
          label="Tipo de colaboración"
        />
      </v-col>

      <v-col cols="12" md="6">
        <v-textarea
          v-model="etapa.descripcionColaboracion"
          label="Detalles de la colaboración"
          auto-grow
          rows="1"
         />
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
    },
    // COMPUTED PROPERTIES PARA MANEJAR FECHAS (String <-> Date)
    fechaInicioPicker: {
      get(): Date | undefined {
        if (!this.etapa.fechaInicio) return undefined;
        // Parseamos manualmente para asegurar zona horaria local (evitar problemas UTC)
        const [y, m, d] = this.etapa.fechaInicio.split('-').map(Number);
        return new Date(y, m - 1, d);
      },
      set(val: Date) {
        if (!val) return;
        // Formateamos a YYYY-MM-DD
        const year = val.getFullYear();
        const month = String(val.getMonth() + 1).padStart(2, '0');
        const day = String(val.getDate()).padStart(2, '0');
        this.etapa.fechaInicio = `${year}-${month}-${day}`;
        this.menuInicio = false; // Cerramos el menú al seleccionar
      }
    },
    fechaFinPicker: {
      get(): Date | undefined {
        if (!this.etapa.fechaFin) return undefined;
        const [y, m, d] = this.etapa.fechaFin.split('-').map(Number);
        return new Date(y, m - 1, d);
      },
      set(val: Date) {
        if (!val) return;
        const year = val.getFullYear();
        const month = String(val.getMonth() + 1).padStart(2, '0');
        const day = String(val.getDate()).padStart(2, '0');
        this.etapa.fechaFin = `${year}-${month}-${day}`;
        this.menuFin = false; // Cerramos el menú al seleccionar
      }
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
      if (!this.etapa.fechaInicio) return true;
      
      const seleccionada = new Date(String(date))
      
      // Parseamos la fecha de inicio como Local para comparar manzanas con manzanas
      const [y, m, d] = this.etapa.fechaInicio.split('-').map(Number);
      const inicio = new Date(y, m - 1, d);
      
      return seleccionada > inicio
    },
    formartearFecha(fecha: string | Date){
      if (!fecha) return '';
      // Aseguramos parseo local para visualización
      const [y, m, d] = String(fecha).split('-').map(Number);
      const dateObj = new Date(y, m - 1, d);
      return dateObj.toLocaleDateString('es-AR');
    }
  }
})
</script>