import { createRouter, createWebHistory } from "vue-router";
import type { RouteRecordRaw } from "vue-router"; 
// Importar vistas
import TestBonita from "../views/TestBonita.vue";

// Definir las rutas con tipado
const routes: Array<RouteRecordRaw> = [
  {
    path: "/",
    name: "Home",
    component: TestBonita, // por ahora muestra TestBonita
  },
  {
    path: "/test-bonita",
    name: "TestBonita",
    component: TestBonita,
  },
];

// Crear router
const router = createRouter({
  history: createWebHistory(),
  routes,
});

export default router;
