<template>
  <v-app-bar color="primary" app>
    <v-app-bar-nav-icon v-if="isAuthenticated" @click.stop="$emit('toggle-drawer')" />

    <v-toolbar-title>HEADER, NO ESTA EN USO (reemplazado por App.vue)</v-toolbar-title>

    <template v-if="$vuetify.display.mdAndUp && isAuthenticated">
      <v-btn icon="mdi-magnify" variant="text" />
      <v-btn icon="mdi-filter" variant="text" />
    </template>

    <v-spacer />

    <v-btn
      v-if="isAuthenticated"
      text
      @click="logout"
    >
      Cerrar sesión
    </v-btn>
  </v-app-bar>
</template>

<script lang="ts">
import { defineComponent, ref, onMounted } from 'vue'

export default defineComponent({
  emits: ['toggle-drawer'],
  setup() {
    const isAuthenticated = ref(false)

    onMounted(() => {
      try {
        isAuthenticated.value = !!localStorage.getItem('token')
      } catch (e) {
        isAuthenticated.value = false
      }
    })

    function logout() {
      try {
        localStorage.removeItem('token')
      } catch (e) {}
      window.location.reload()
    }

    function goToLogin() {
      window.location.href = '/'
    }

    return { isAuthenticated, logout, goToLogin }
  }
})
</script>
