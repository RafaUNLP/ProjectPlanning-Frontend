import axios from "axios";

const api = axios.create({
  baseURL: "http://localhost:5165",
  headers: {
    "Content-Type": "application/json"
  }
});

// Attach Authorization header if token exists in localStorage
api.interceptors.request.use((config) => {
  try {
    const token = localStorage.getItem('token')
    if (token && config.headers) {
      config.headers['Authorization'] = `Bearer ${token}`
    }
  } catch (e) {
    // ignore (e.g., server-side rendering or blocked storage)
  }
  return config
}, (error) => {
  return Promise.reject(error)
})

export default api;