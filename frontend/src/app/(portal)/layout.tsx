import { MarcoPortal } from "@/components/portal";

// El marco no pinta nada hasta confirmar la sesión, así que Next.js no puede validar la navegación
// instantánea de las páginas del portal; se excluye a propósito (guía instant-navigation, «Opting out»).
export const instant = false;

export default function LayoutPortal({ children }: LayoutProps<"/">) {
  return <MarcoPortal>{children}</MarcoPortal>;
}
