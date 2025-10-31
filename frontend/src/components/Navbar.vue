<template>
    <!-- La barra superior -->
    <v-app-bar color="primary" app>
      <v-app-bar-nav-icon @click.stop="drawer = !drawer" />
  
      <v-toolbar-title>My files</v-toolbar-title>
  
      <template v-if="$vuetify.display.mdAndUp">
        <v-btn icon="mdi-magnify" variant="text" />
        <v-btn icon="mdi-filter" variant="text" />
      </template>
  
      <v-btn icon="mdi-dots-vertical" variant="text" />
    </v-app-bar>
  
    <!-- Drawer lateral -->
    <v-navigation-drawer v-model="drawer" app temporary>
      <v-list :items="items" />
    </v-navigation-drawer>
  </template>
  
  <script lang="ts">
  import { defineComponent, ref } from 'vue'
  
  export default defineComponent({
    setup() {
      const drawer = ref(false)
      const items = [
        { title: 'Cargar Proyecto', value: 'cargar' },
        { title: 'Proyectos', value: 'proyectos' },
        { title: 'Colaboraciones', value: 'colaboraciones' },
      ]
      return { drawer, items }
    }
  })
  </script>
  