<template>
  <v-container class="fill-height" fluid>
    <v-row align="center" justify="center">
      <v-col cols="12" sm="8" md="4">
        <v-card>
          <v-card-title>Iniciar sesión</v-card-title>
          <v-card-text>
            <v-form ref="form" @submit.prevent="submit">
              <v-text-field
                v-model="username"
                label="Usuario"
                required
                dense
              />

              <v-text-field
                v-model="password"
                label="Contraseña"
                type="password"
                required
                dense
              />

              <v-alert
                v-if="error"
                type="error"
                dense
                class="mb-2"
              >
                {{ error }}
              </v-alert>

              <v-card-actions>
                <v-spacer />
                <v-btn color="primary" type="submit" variant="flat">Ingresar</v-btn>
              </v-card-actions>
            </v-form>
          </v-card-text>
        </v-card>
      </v-col>
    </v-row>
  </v-container>
</template>

<script setup lang="ts">
import { ref } from 'vue'
import api from '../api'

const username = ref('')
const password = ref('')
const error = ref('')
const form = ref<HTMLFormElement | null>(null)

async function submit() {
  error.value = ''
  try {
    const payload = { username: username.value, password: password.value }
    const resp = await api.post('/auth/login', payload)
    console.log('Login response:', resp)
    if (resp && resp.data && resp.data.token) {
      console.log('Token recibido:', resp.data.token)
      localStorage.setItem('token', resp.data.token)
      // reload to let the app pick up auth state; simple and effective for now
      window.location.reload()
    } else {
      error.value = 'Respuesta inválida del servidor.'
    }
  } catch (err: any) {
    console.error('Login error:', err)
    if (err.response && err.response.data && err.response.data.message) {
      error.value = err.response.data.message
    } else {
      error.value = 'Error conectando con el servidor.'
    }
  }
}
</script>

<style scoped>
/* Forzar color legible en los inputs (evita texto blanco sobre fondo claro). */
::v-deep .v-field input,
::v-deep .v-field textarea,
::v-deep .v-field__input {
  color: #111 !important;
}

::v-deep .v-field .v-label,
::v-deep .v-field label {
  color: rgba(0, 0, 0, 0.7) !important;
}

/* En caso de placeholders o autofill */
::v-deep input::placeholder {
  color: rgba(0,0,0,0.5) !important;
}

/* Si el input tuviera fondo blanco sobre fondo blanco, forzamos fondo claro y borde sutil */
::v-deep .v-field {
  background: transparent !important;
}
</style>  
