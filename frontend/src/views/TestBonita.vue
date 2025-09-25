<script setup>
import { ref, onMounted } from "vue";
import api, { loginBonita } from "../services/access";

const cases = ref([]);
const mensaje = ref("Probando conexión...");

onMounted(async () => {
  try {
    await loginBonita(); // 🔑 login primero
    // const res = await api.get("/API/bpm/case");
    // cases.value = res.data;
    mensaje.value = "Conexión exitosa 🎉";
  } catch (err) {
    mensaje.value = "Error: " + err.message;
  }
});
</script>

<template>
  <div>
    <h1>Test Bonita</h1>
    <p>{{ mensaje }}</p>


  </div>
</template>
