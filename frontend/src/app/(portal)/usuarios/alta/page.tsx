import { AltaUsuario } from "./alta";

// Se pinta después de confirmar la sesión (ver (portal)/layout.tsx).
export const instant = false;

export default function Page() {
  return <AltaUsuario />;
}
