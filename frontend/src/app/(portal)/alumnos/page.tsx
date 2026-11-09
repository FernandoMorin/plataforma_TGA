import { ConsultaAlumnos } from "./consulta";

// Se pinta después de confirmar la sesión (ver (portal)/layout.tsx).
export const instant = false;

export default function Page() {
  return <ConsultaAlumnos />;
}
