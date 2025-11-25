<template>
  <!-- Drawer lateral -->
  <v-navigation-drawer v-model="localDrawer" app temporary>
    <v-list :items="items" />
  </v-navigation-drawer>
</template>

<script lang="ts">
import { defineComponent, computed } from 'vue'

export default defineComponent({
  name: 'Navbar',
  props: {
    modelValue: {
      type: Boolean,
      required: true
    }
  },
  emits: ['update:modelValue'],
  setup(props, { emit }) {
    const items = [
      { title: 'Cargar Proyecto', value: 'cargar' },
      { title: 'Proyectos', value: 'proyectos' },
      { title: 'Colaboraciones', value: 'colaboraciones' },
    ]

    const localDrawer = computed({
      get: () => props.modelValue,
      set: (v: boolean) => emit('update:modelValue', v)
    })

    return { items, localDrawer }
  }
})
</script>
