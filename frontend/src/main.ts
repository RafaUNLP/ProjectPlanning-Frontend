import { createApp } from 'vue'
import './style.css'
import App from './App.vue'

// Vuetify
import { createVuetify } from 'vuetify'
import * as components from 'vuetify/components'
import * as directives from 'vuetify/directives'
import '@mdi/font/css/materialdesignicons.css'


const vuetify = createVuetify({
  components,
  directives,
  theme: {
    defaultTheme: 'myCustomLight',     // 👈 tema por defecto
    themes: {
      myCustomLight: {
        dark: false,
        colors: {
          background: '#EEEEEE', // color general de fondo, grey-lighten-3
          surface: '#3a8bddff', // para cards, drawer, etc, blue-darken-1
          primary: '#1976D2', // blue-darken-1
          secondary: '#5E35B1'// deep-purple-darken-1
        },
      },
    },
  },
})
  
createApp(App).use(vuetify).mount('#app')