import axios from "axios";

const api = axios.create({
  baseURL: "https://localhost:7166/api",
  headers: {
    "Content-Type": "application/json"
  }
});

export default api;