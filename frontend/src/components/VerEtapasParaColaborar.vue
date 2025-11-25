<template>
  <div class="p-4">

    <h1 class="text-2xl font-bold mb-4">Etapas que requieren colaboración</h1>

    <div v-if="loading">Cargando...</div>
    <div v-if="error" class="text-red-600">{{ error }}</div>

    <div v-if="proyectos.length != 0">
      <div v-for="proyecto in proyectos" :key="proyecto.id" class="mb-6 p-4 border rounded">

        <h2 class="text-xl font-semibold">{{ proyecto.nombre }}</h2>
        <p class="mb-3">{{ proyecto.descripcion }}</p>

        <div v-if="proyecto.etapas.length === 0" class="text-gray-500">
          No quedan etapas disponibles.
        </div>

        <div
          v-for="etapa in proyecto.etapas"
          :key="etapa.id"
          class="p-3 mt-2 border rounded bg-gray-50"
        >

          <h3 class="font-semibold">{{ etapa.nombre }}</h3>
          <p>{{ etapa.descripcion }}</p>

          <div class="mt-3 flex gap-2">
            <button
              class="px-3 py-1 bg-blue-600 text-white rounded"
              @click="abrirModal(etapa, false)"
            >
              Postularme (total)
            </button>

            <button
              class="px-3 py-1 bg-orange-500 text-white rounded"
              @click="abrirModal(etapa, true)"
            >
              Postularme (parcial)
            </button>
          </div>

        </div>
      </div>
    </div>

    <div v-if="proyectos.length === 0"class="text-gray-500">
          No hay proyectos que requieran colaboraciones.
    </div>
    <!-- Modal ----------------------------------------------------------- -->
    <div
      v-if="showModal"
      class="fixed inset-0 bg-black bg-opacity-40 flex justify-center items-center p-4"
    >
      <div class="bg-white p-6 rounded shadow-md w-full max-w-md">

        <h2 class="text-xl font-bold mb-4">
          Proponer colaboración
        </h2>

        <p class="mb-2">
          Etapa seleccionada:
          <b>{{ etapaSeleccionada?.nombre }}</b>
        </p>

        <label class="block mb-2">
          Descripción:
          <textarea
            v-model="propuesta.descripcion"
            class="w-full p-2 border rounded"
          ></textarea>
        </label>

        <label class="block mb-2">
          Categoría:
          <input
            v-model="propuesta.categoriaColaboracion"
            class="w-full p-2 border rounded"
          />
        </label>

        <label class="block mb-4">
          Tipo:
          <b>{{ propuesta.esParcial ? "Parcial" : "Total" }}</b>
        </label>

        <div class="flex justify-end gap-2">
          <button
            class="px-3 py-1 bg-gray-300 rounded"
            @click="showModal = false"
          >
            Cancelar
          </button>

          <button
            class="px-3 py-1 bg-green-600 text-white rounded"
            @click="enviarPropuesta"
          >
            Enviar
          </button>
        </div>

      </div>
    </div>
  </div>
</template>

<script lang="ts">
import { defineComponent } from "vue";
import api from '../api'

interface Propuesta {
  descripcion: string;
  categoriaColaboracion: string;
  esParcial: boolean;
  organizacionProponenteId: number | null;
}

export default defineComponent({
  name: "VerEtapasParaColaborar",

  data() {
    return {
      proyectos: [] as any[],
      loading: false,
      error: null as any,

      showModal: false,
      etapaSeleccionada: null as any,

      propuesta: {
        descripcion: "",
        categoriaColaboracion: "",
        esParcial: false,
        organizacionProponenteId: null,
      } as Propuesta,

      token: localStorage.getItem("token"),
    };
  },

  methods: {
    async cargarProyectos() {
      this.loading = true;
      /*try { 
        const res = await fetch("/requierenColaboraciones", {
          headers: { Authorization: `Bearer ${this.token}` },
        });
        if (!res.ok) throw await res.text();
        this.proyectos = await res.json();
      } catch (err) {
        this.error = err;
      } finally {
        this.loading = false;
      }*/
     try {
        this.loading = true
        const response = await api.get(`/requierenColaboraciones`)
        
        if (response && response.data) {
          this.proyectos = response.data
        }
      } catch (error) {
        console.error('Error al cargar propuestas de colaboración:', error)
      } finally {
        this.loading = false
      }
    },

    abrirModal(etapa: any, esParcial: boolean) {
      this.etapaSeleccionada = etapa;
      this.propuesta = {
        descripcion: "",
        categoriaColaboracion: "",
        esParcial,
        organizacionProponenteId: 123, // aquí ponés la org del usuario logueado
      };
      this.showModal = true;
    },

    async enviarPropuesta() {
      const body = {
        etapaId: this.etapaSeleccionada.id,
        organizacionProponenteId: this.propuesta.organizacionProponenteId,
        descripcion: this.propuesta.descripcion,
        categoriaColaboracion: this.propuesta.categoriaColaboracion,
        esParcial: this.propuesta.esParcial,
      };

      try {
        const res = await fetch("/PropuestaColaboracion", {
          method: "POST",
          headers: {
            "Content-Type": "application/json",
            Authorization: `Bearer ${this.token}`,
          },
          body: JSON.stringify(body),
        });

        if (!res.ok) {
          const err = await res.text();
          alert("Error: " + err);
          return;
        }

        alert("¡Propuesta enviada!");
        this.showModal = false;
        this.cargarProyectos();
      } catch (err) {
        alert("Error de red: " + err);
      }
    },
  },

  mounted() {
    this.cargarProyectos();
  },
});
</script>
