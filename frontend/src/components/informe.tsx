"use client";

import type { ReactNode } from "react";
import { Download } from "lucide-react";
import { desdeUtc, fechaLocal } from "@/lib/fechas";
import { Insignia } from "@/components/modulo";

// Tarjeta de un informe (Figma AT5). La usan el alumno (Mis evaluaciones) y su comité (detalle del alumno).

export type Informe = {
  id: number;
  numero: number;
  periodo: string | null;
  estado: string;
  cerrada: boolean;
  fecha: string | null;
  avance: number | null;
  promedio: number | null;
  documento: number | null;
  exposicion: number | null;
  dominio: number | null;
  gradoAvance: number | null;
  comentarios: string | null;
  compromisos: { descripcion: string; fecha: string | null; cumplido: boolean }[] | null;
  acta: number | null;
};

const criterios = [
  { clave: "documento", texto: "Documento de avance" },
  { clave: "exposicion", texto: "Exposición" },
  { clave: "dominio", texto: "Dominio del tema" },
  { clave: "gradoAvance", texto: "Grado de avance en el periodo" },
] as const;

const fecha = (iso: string | null) => (iso ? iso.slice(0, 10).split("-").reverse().join("/") : "—");

export type Avance = { id: number; nombre: string | null; tipo: string; informe: number; fecha: string | null };

// Documentos que subió el alumno para un informe (Avances y detalle del alumno para su comité).
export function ListaAvances({ avances }: { avances: Avance[] }) {
  return (
    <ul>
      {avances.map((d) => (
        <li key={d.id} className="flex items-center gap-3 border-t border-portal-borde py-2.5">
          <a href={`/api/documentos/${d.id}`} className="flex-1 truncate text-sm font-semibold text-portal-activo">{d.nombre}</a>
          <Insignia tono="gris">{d.tipo}</Insignia>
          <span className="text-[13px] text-portal-subtexto">{d.fecha ? fechaLocal(desdeUtc(d.fecha)) : "—"}</span>
        </li>
      ))}
    </ul>
  );
}

// «insignia» y «abierto» reemplazan lo que se muestra mientras el informe no está cerrado; «children» va al final.
export function TarjetaInforme({ informe: i, insignia, abierto, children }: { informe: Informe; insignia?: ReactNode; abierto?: ReactNode; children?: ReactNode }) {
  return (
    <section className="flex flex-col gap-4 rounded-xl border border-portal-borde bg-blanco px-6 py-[22px]">
      <div className="flex items-center justify-between">
        <h2 className="text-[17px] font-semibold text-portal-titulo">Informe {i.numero} · {i.periodo}</h2>
        {insignia ?? (i.cerrada ? <Insignia tono="verde">Cerrada</Insignia> : <Insignia tono="ambar">En proceso</Insignia>)}
      </div>
      {i.cerrada ? <Detalle i={i} /> : abierto ?? <p className="text-sm text-portal-subtexto">Podrás consultarla cuando el comité la cierre.</p>}
      {children}
    </section>
  );
}

function Detalle({ i }: { i: Informe }) {
  return (
    <>
      <dl className="flex gap-4">
        {[["Avance", i.avance === null ? "—" : `${Math.round(i.avance)} %`], ["Promedio", i.promedio ?? "—"], ["Fecha", fecha(i.fecha)]].map(([etiqueta, valor]) => (
          <div key={etiqueta} className="flex flex-1 flex-col gap-1 rounded-[10px] bg-portal-tabla-cabecera px-3.5 py-3">
            <dt className="text-[13px] text-portal-subtexto">{etiqueta}</dt>
            <dd className="text-[22px] font-bold text-portal-titulo">{valor}</dd>
          </div>
        ))}
      </dl>

      <div className="flex flex-col gap-2">
        <h3 className="text-sm font-semibold text-portal-titulo">Calificación por criterio</h3>
        {criterios.map((c) => (
          <div key={c.clave} className="flex items-center gap-3 text-sm">
            <span className="w-[240px] shrink-0 text-portal-menu-texto">{c.texto}</span>
            <span className="h-2 flex-1 overflow-hidden rounded-[4px] bg-portal-neutro-fondo" aria-hidden>
              <span className="block h-full bg-portal-activo" style={{ width: `${i[c.clave] ?? 0}%` }} />
            </span>
            <span className="w-7 text-right font-semibold text-portal-titulo">{i[c.clave]}</span>
          </div>
        ))}
      </div>

      <div className="flex flex-col gap-1.5 text-sm">
        <h3 className="font-semibold text-portal-titulo">Comentarios y sugerencias del comité</h3>
        <p className="text-portal-menu-texto">{i.comentarios}</p>
      </div>

      {!!i.compromisos?.length && (
        <div className="flex flex-col gap-1.5">
          <h3 className="text-sm font-semibold text-portal-titulo">Compromisos</h3>
          <ul>
            {i.compromisos.map((c) => (
              <li key={c.descripcion} className="flex items-center gap-3 border-t border-portal-borde py-2">
                <span className="flex-1 text-sm text-portal-menu-texto">{c.descripcion}</span>
                <span className="text-[13px] text-portal-subtexto">Para el {fecha(c.fecha)}</span>
                <Insignia tono={c.cumplido ? "verde" : "gris"}>{c.cumplido ? "Cumplido" : "Pendiente"}</Insignia>
              </li>
            ))}
          </ul>
        </div>
      )}

      {i.acta && (
        <div className="flex justify-end">
          <a href={`/api/documentos/${i.acta}`} className="flex items-center gap-2 rounded-lg border border-portal-borde-campo bg-blanco py-2.5 pr-[18px] pl-4 text-[15px] font-semibold text-portal-menu-texto">
            <Download size={18} aria-hidden />
            Descargar acta
          </a>
        </div>
      )}
    </>
  );
}
