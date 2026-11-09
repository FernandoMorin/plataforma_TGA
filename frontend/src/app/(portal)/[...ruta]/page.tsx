import { notFound } from "next/navigation";
import { ModuloPendiente } from "@/components/portal";
import { pagina, todasLasPaginas } from "@/lib/menu";

// Página genérica para cada entrada del menú que todavía no tiene su módulo.
// Al hacer un módulo, su propia page.tsx (por ejemplo, (portal)/alumnos/page.tsx) toma prioridad sobre esta.

// Igual que el layout del portal: se pinta después de confirmar la sesión.
export const instant = false;

export function generateStaticParams() {
  return todasLasPaginas().map((p) => ({ ruta: p.ruta.slice(1).split("/") }));
}

export default async function PaginaPendiente({ params }: PageProps<"/[...ruta]">) {
  const ruta = `/${(await params).ruta.join("/")}`;
  if (!pagina(ruta)) notFound();
  return <ModuloPendiente ruta={ruta} />;
}
