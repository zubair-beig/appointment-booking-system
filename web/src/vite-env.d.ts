/// <reference types="vite/client" />

// Typed view of the VITE_* variables the app reads through import.meta.env.
interface ImportMetaEnv {
  /** Base URL of the AppointmentBook API, e.g. http://localhost:5080 or https://api.example.com */
  readonly VITE_API_URL?: string
}

interface ImportMeta {
  readonly env: ImportMetaEnv
}
