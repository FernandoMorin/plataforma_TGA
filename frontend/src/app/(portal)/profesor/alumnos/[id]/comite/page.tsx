import { ArmarComite } from "./comite";

// Se pinta después de confirmar la sesión (ver (portal)/layout.tsx, que también pone el Suspense).
export const instant = false;

export default function Page() {
  return <ArmarComite />;
}
