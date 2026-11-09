import { notFound } from "next/navigation";
import { EncabezadoPagina } from "@/components/portal";
import { pagina, todasLasPaginas } from "@/lib/menu";

// Página genérica para cada entrada del menú que todavía no tiene su módulo.
// Al hacer un módulo, su propia page.tsx (por ejemplo, (portal)/alumnos/page.tsx) toma prioridad sobre esta.

// Igual que el layout del portal: se pinta después de confirmar la sesión.
export const instant = false;

export function generateStaticParams() {
  return todasLasPaginas().map((p) => ({ ruta: p.ruta.slice(1).split("/") }));
}

export default async function PaginaPendiente({ params }: PageProps<"/[...ruta]">) {
  const { ruta } = await params;
  const p = pagina(`/${ruta.join("/")}`);
  if (!p) notFound();
  return (
    <>
      <EncabezadoPagina titulo={p.titulo} descripcion={p.descripcion} />
      <div className="flex flex-1 items-center justify-center rounded-[10px] border-2 border-dashed border-portal-borde text-portal-subtexto">
        Contenido del módulo — se maqueta por separado
      </div>
    </>
  );
}
