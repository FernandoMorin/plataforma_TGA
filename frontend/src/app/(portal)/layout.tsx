import { Suspense } from "react";
import { MarcoPortal } from "@/components/portal";

// El marco no pinta nada hasta confirmar la sesión, así que Next.js no puede validar la navegación
// instantánea de las páginas del portal; se excluye a propósito (guía instant-navigation, «Opting out»).
export const instant = false;

// Suspense: el marco lee la ruta en el cliente (usePathname), y en rutas dinámicas como
// /alumnos/[clave] eso solo se resuelve en el navegador.
export default function LayoutPortal({ children }: LayoutProps<"/">) {
  return (
    <Suspense>
      <MarcoPortal>{children}</MarcoPortal>
    </Suspense>
  );
}
