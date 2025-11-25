<script setup lang="ts">
import { ref, onMounted, computed } from 'vue'
import AgregarProyecto from './components/AgregarProyecto.vue'
import Navbar from './components/Navbar.vue'
import Header from './components/Header.vue'
import Login from './components/Login.vue'
import Auditoria from './components/Auditoria.vue'

const drawer = ref(false)
const isAuthenticated = ref(false)
const role = ref('')

function parseJwt(token: string | null) {
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
}

function extractRoleFromPayload(payload: any) {
  //TODO
}

onMounted(() => {
  try {
    const token = localStorage.getItem('token')
    isAuthenticated.value = !!token
    if (token) {
      const payload = parseJwt(token)
      const r = extractRoleFromPayload(payload)
      role.value = r || ''
    }
  } catch (e) {
    isAuthenticated.value = false
    role.value = ''
  }
})

const currentView = computed(() => {
  if (!isAuthenticated.value) return Login
  const r = role.value.toLowerCase()
  if (r.includes('auditor')) return Auditoria
  if (r.includes('organizacion')) return AgregarProyecto
  //Fallback??
  return AgregarProyecto
})
</script>

<template>
  <v-app>
    <Header @toggle-drawer="drawer = !drawer" />
    <Navbar v-if="isAuthenticated" v-model="drawer" />
    <v-main>
      <component :is="currentView" />
    </v-main>
  </v-app>
</template>

<style scoped>
</style>
