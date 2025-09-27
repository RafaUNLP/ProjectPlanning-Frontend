<template>
  <v-container cols="12" md="6" outlined>
    <v-row class="mb-1">
      <v-col cols="12" class="d-flex justify-start align-center">
        <h2 class="mr-2">Etapa {{ index + 1 }}</h2>
        <v-btn class="ml-auto" color="error" variant="tonal" @click="eliminarEtapa">
          Eliminar
        </v-btn>
      </v-col>
    </v-row>

    <!-- Nombre y Descripción -->
    <v-row>
      <v-col cols="12" md="6">
        <v-text-field
          v-model="nombre"
          label="Nombre de la etapa"
          outlined
          dense
        />
      </v-col>

      <v-col cols="12" md="6">
        <v-text-field
          v-model="descripcion"
          label="Descripción de la etapa"
          outlined
          dense
        />
      </v-col>
    </v-row>

    <!-- Fechas -->
    <v-row>
      <v-col cols="12" md="6">
        <v-date-input
          label="Fecha de Inicio"
          v-model="fechaInicio"
          :allowed-dates="soloFuturas"
        />
      </v-col>

      <v-col cols="12" md="6">
        <v-date-input
          label="Fecha de Fin"
          v-model="fechaFin"
          :allowed-dates="mayorAlInicio"
        />
      </v-col>
    </v-row>

    <!-- Colaboraciones -->
    <v-row>
      <v-col cols="12" md="6">
        <v-select
          v-model="opcionesElegidas"
          :items="opcionesColaboracion"
          multiple
          label="Tipo de colaboración"
          outlined
          dense
        />
      </v-col>

      <v-col cols="12" md="6">
        <v-textarea
          v-model="descripcionColaboraciones"
          label="Detalles de la colaboración"
          auto-grow
          outlined
          dense
        />
      </v-col>
    </v-row>

  </v-container>
</template>

<script lang="ts">
import { defineComponent } from "vue";

export default defineComponent({
  name: "AgregarEtapa",
  props: {
    index: {
      type: Number,
      required: true,
    },
  },
  data() {
    return {
      nombre: "",
      descripcion: "",
      fechaInicio: null as string | null,
      fechaFin: null as string | null,
      opcionesColaboracion: ["Económica", "Materiales", "Mano de Obra", "Otra"],
      opcionesElegidas: ["Económica"] as string[],
      descripcionColaboraciones: "",
    };
  },
  methods: {
    eliminarEtapa() {
      this.$emit("eliminar", this.index);
    },
    soloFuturas(date: unknown): boolean {
      if (typeof date !== "string") return false;
      const seleccionada = new Date(date);
      const hoy = new Date();
      hoy.setHours(0, 0, 0, 0);
      return seleccionada > hoy;
    },
    mayorAlInicio(date: unknown): boolean {
      if (typeof date !== "string" || !this.fechaInicio) return false;
      const seleccionada = new Date(date);
      const inicio = new Date(this.fechaInicio);
      return seleccionada > inicio;
    },
  },
});
</script>
