<template>
  <v-app>
    <v-app-bar color="primary" app>
      <v-app-bar-nav-icon v-if="isAuthenticated" @click="drawer = !drawer" />

      <v-toolbar-title>{{ pageTitle }}</v-toolbar-title>

      <v-spacer />

      <v-btn
        v-if="isAuthenticated"
        prepend-icon="mdi-logout"
        variant="text"
        @click="logout"
      >
        Cerrar sesión
      </v-btn>
    </v-app-bar>

    <v-navigation-drawer v-model="drawer" v-if="isAuthenticated" temporary>
      <v-list>
        <v-list-subheader>Menú Principal</v-list-subheader>
        
        <v-list-item 
          v-for="(item, index) in menuItems" 
          :key="index" 
          @click="cambiarVista(item.component)"
          :active="currentView === item.component"
          color="white"
          link
        >
          <v-list-item-title>{{ item.title }}</v-list-item-title>
        </v-list-item>
      </v-list>
    </v-navigation-drawer>

    <v-main>
      <v-container fluid>
        <component :is="currentView" />
      </v-container>
    </v-main>
  </v-app>
</template>

<script setup lang="ts">
import { ref, onMounted, shallowRef } from 'vue'
import type { Component } from 'vue'
import { useDisplay } from 'vuetify'

// --- Imports de tus componentes ---
import Login from './components/Login.vue'
import Auditoria from './components/Auditoria.vue'
import AgregarProyecto from './components/AgregarProyecto.vue'
import ListaProyectos from './components/ListaProyectos.vue'
import VerEtapasParaColaborar from './components/VerEtapasParaColaborar.vue'
import MisColaboraciones from './components/MisColaboraciones.vue'
import PropuestasRecibidas from './components/PropuestasRecibidas.vue'

// --- Tipos ---
interface MenuItem {
  title: string;
  component: Component; // Guardamos el componente directo aquí para facilitar
}

// --- Estado Global ---
const display = useDisplay()
const drawer = ref(false)
const isAuthenticated = ref(false)
const role = ref('')
const pageTitle = ref('Proyect Planning')

// Usamos shallowRef para guardar el COMPONENTE actual. 
// shallowRef es mejor que ref para componentes porque Vue no intenta hacerlo reactivo profundamente.
const currentView = shallowRef<Component>(Login) 

// --- Configuración del Menú ---
const menuItems = ref<MenuItem[]>([
  { title: 'Agregar Proyecto', component: AgregarProyecto },
  { title: 'Mis Proyectos', component: ListaProyectos },
  { title: 'Colaborar', component: VerEtapasParaColaborar },
  { title: 'Mis Colaboraciones', component: MisColaboraciones },
  { title: 'Propuestas Recibidas', component: PropuestasRecibidas },
]);

// --- Lógica de Auth y JWT ---
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
  // Ajusta esto según cómo venga tu rol en el JWT
  if (!payload) return ''
  return payload.role || payload.rol || '' 
}

function generateTitle(payload: any) {
  let username = ''
  if (payload){
    username = payload.username || payload.user || payload.Username || ''
  }
  return 'Proyect Planning (' + username + ')'
}

function logout() {
  localStorage.removeItem('token')
  window.location.reload()
}

// --- Navegación ---
function cambiarVista(componente: Component) {
  currentView.value = componente
  drawer.value = false // Cierra el menú al seleccionar
}

// --- Lifecycle ---
onMounted(() => {
  try {
    const token = localStorage.getItem('token')
    
    if (token) {
      isAuthenticated.value = true
      const payload = parseJwt(token)
      const r = extractRoleFromPayload(payload)
      pageTitle.value = generateTitle(payload)
      role.value = r || ''

      // Lógica inicial: ¿Qué mostramos apenas carga?
      const roleLower = role.value.toLowerCase()
      console.log('rol aca',roleLower)
      if (roleLower.includes('auditor')) {
        currentView.value = Auditoria
        // Quizás quieras vaciar el menú si es auditor
        menuItems.value = [] 
      } else if (roleLower.includes('organizacion') || roleLower.includes('colaborador')) {
        currentView.value = ListaProyectos
      } else {
        // Default fallback si está logueado pero el rol es raro
        currentView.value = VerEtapasParaColaborar 
      }
    } else {
      isAuthenticated.value = false
      currentView.value = Login
    }
  } catch (e) {
    isAuthenticated.value = false
    currentView.value = Login
  }
})
</script>