import { ModuloPendiente } from "@/components/portal";

// Necesaria porque /alumnos/[clave] atraparía «importar» antes que la página genérica.
// shortcut: módulo pendiente (Figma I1-I3); se reemplaza al hacer la importación.
export const instant = false;

export default function Page() {
  return <ModuloPendiente ruta="/alumnos/importar" />;
}
