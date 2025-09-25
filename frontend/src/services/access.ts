import axios from "axios";
import type { AxiosInstance } from "axios";


// // El token puede ser string o null
 let bonitaToken: string | null = null;



const api = axios.create({
  baseURL: "/bonita", // pasa por el proxy de Vite
  withCredentials: true, // envia las cookies en cada request
});

export const loginBonita = async () => {
  try {
    // const response = await api.post("/loginservice?username=walter.bates&password=bpm&redirect=false");
    const response = await api.post("/loginservice", null, {
      params: { //query params
        username: "walter.bates",
        password: "bpm",
        redirect: false,
      },
    });

    console.log(response)

    // El token viene en los headers
    bonitaToken = response.headers["X-Bonita-API-Token"] as string;

    console.log("Login OK, token:", bonitaToken);
    return bonitaToken;

  }
  catch (err) {
    console.error("Error en login:", err);
    throw err;
  }
};

// // Interceptor para agregar el token en cada request
// api.interceptors.request.use((config) => {
//   if (bonitaToken && config.headers) {
//     // headers puede ser AxiosRequestHeaders o un objeto plano
//     (config.headers as Record<string, string>)["X-Bonita-API-Token"] = bonitaToken;
//   }
//   return config;
// });

export default api;
