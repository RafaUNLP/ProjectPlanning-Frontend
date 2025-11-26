<template>
  <div>
    <v-app-bar color="primary" density="compact">
      <v-app-bar-nav-icon @click="drawer = !drawer"></v-app-bar-nav-icon>
      <v-app-bar-title>NAVBAR, NO ESTA EN USO (reemplazado por App.vue)</v-app-bar-title>
    </v-app-bar>

    <v-navigation-drawer v-model="drawer" temporary>
      <v-list>
        <v-list-subheader>Menú Principal</v-list-subheader>
        
        <v-list-item 
          v-for="(item, index) in menuItems" 
          :key="index" 
          @click="selectComponent(item.value)"
          :active="selectedKey === item.value"
          color="primary"
        >
          <v-list-item-title>{{ item.title }}</v-list-item-title>
        </v-list-item>
      </v-list>
    </v-navigation-drawer>

    <v-main>
      <v-container>
        <component 
          :is="currentComponent" 
          v-if="currentComponent" 
        />
        
        <div v-else class="text-center mt-10">
          <h3>Selecciona una opción del menú</h3>
        </div>
      </v-container>
    </v-main>
    
  </div>
</template>

<script setup lang="ts">
import { ref, computed } from 'vue';
import type { Component } from 'vue';

// --- 1. Importación de Componentes ---
// Asegúrate de que estas rutas sean las REALES de tus archivos
import CargarProyecto from './AgregarProyecto.vue';
import ListaProyectos from './AgregarProyecto.vue'; // OJO: Apunta al mismo archivo en tu ejemplo
import Colaboraciones from './VerEtapasParaColaborar.vue';

// --- 2. Definición de Tipos ---
// Definimos las claves permitidas explícitamente
type ComponentKey = 'cargar' | 'proyectos' | 'colaboraciones';

interface MenuItem {
  title: string;
  value: ComponentKey;
}

// --- 3. Estado ---
const drawer = ref(false); // Controla la visibilidad del menú lateral
const selectedKey = ref<ComponentKey | null>(null); // Controla qué key está activa

// --- 4. Configuración del Menú ---
const menuItems: MenuItem[] = [
  { title: 'Cargar Proyecto', value: 'cargar' },
  { title: 'Proyectos', value: 'proyectos' },
  { title: 'Colaboraciones', value: 'colaboraciones' },
];

// --- 5. Mapeo de Componentes (Lookup Table) ---
// Usamos el tipo 'Component' de Vue para mayor seguridad
const componentMap: Record<ComponentKey, Component> = {
  cargar: CargarProyecto,
  proyectos: ListaProyectos,
  colaboraciones: Colaboraciones,
};

// --- 6. Computed para obtener el componente actual ---
const currentComponent = computed(() => {
  if (!selectedKey.value) return null;
  return componentMap[selectedKey.value];
});

// --- 7. Métodos ---
const selectComponent = (key: ComponentKey) => {
  selectedKey.value = key;
  drawer.value = false; // Cierra el drawer al seleccionar (opcional)
};
</script>