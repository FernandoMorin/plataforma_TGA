"use client";

import { useEffect, useState } from "react";
import { Download } from "lucide-react";
import { EncabezadoPagina } from "@/components/portal";
import { Insignia, Paginacion } from "@/components/modulo";
import { sesionVencida } from "@/lib/acceso";
import { desdeUtc, fechaLocal, horaLocal } from "@/lib/fechas";

// Historial de reportes (Figma R4).

type Reporte = { id: number; fecha: string | null; tipo: string; filtros: string | null; genero: string | null; formato: "PDF" | "Excel" };
const POR_PAGINA = 10;

export function HistorialReportes() {
  const [reportes, setReportes] = useState<Reporte[] | null>(null);
  const [pag, setPag] = useState(1);

  useEffect(() => {
    fetch("/api/reportes/historial").then(async (res) => {
      if (res.status === 401) sesionVencida();
      else if (res.ok) setReportes(await res.json());
    });
  }, []);

  const lista = reportes ?? [];
  const total = Math.max(1, Math.ceil(lista.length / POR_PAGINA));
  const actual = Math.min(pag, total);

  return (
    <>
      <EncabezadoPagina titulo="Historial de reportes" descripcion="Reportes generados y sus filtros." />
      <section className="overflow-hidden rounded-xl border border-portal-borde bg-blanco">
        <table className="w-full text-left">
          <thead className="bg-portal-tabla-cabecera text-[13px] text-portal-subtexto">
            <tr>
              <th className="py-2.5 pl-5 font-semibold">Fecha</th>
              <th className="font-semibold">Tipo</th>
              <th className="font-semibold">Filtros</th>
              <th className="font-semibold">Generó</th>
              <th className="font-semibold">Formato</th>
              <th><span className="sr-only">Descargar</span></th>
            </tr>
          </thead>
          <tbody className="text-sm text-portal-menu-texto">
            {reportes && lista.length === 0 && (
              <tr className="border-t border-portal-borde"><td colSpan={6} className="py-4 pl-5 text-portal-placeholder">Aún no se ha generado ningún reporte.</td></tr>
            )}
            {lista.slice((actual - 1) * POR_PAGINA, actual * POR_PAGINA).map((r) => {
              const f = r.fecha ? desdeUtc(r.fecha) : null;
              return (
                <tr key={r.id} className="border-t border-portal-borde">
                  <td className="py-4 pl-5 whitespace-nowrap">{f ? `${fechaLocal(f)} ${horaLocal(f)}` : "—"}</td>
                  <td className="pr-4 font-semibold text-portal-titulo">{r.tipo}</td>
                  <td className="pr-4 text-[13px] text-portal-subtexto">{r.filtros}</td>
                  <td className="pr-4">{r.genero}</td>
                  <td><Insignia tono="gris">{r.formato}</Insignia></td>
                  <td className="pr-5 text-right">
                    <a href={`/api/reportes/${r.id}/archivo`} className="inline-flex items-center gap-1.5 font-semibold whitespace-nowrap text-portal-activo">
                      <Download size={18} aria-hidden />Descargar
                    </a>
                  </td>
                </tr>
              );
            })}
          </tbody>
        </table>
        {total > 1 && (
          <div className="flex justify-end border-t border-portal-borde px-5 py-3.5">
            <Paginacion actual={actual} total={total} onCambio={setPag} />
          </div>
        )}
      </section>
    </>
  );
}
