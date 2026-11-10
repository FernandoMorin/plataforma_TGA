"use client";

import type { ReactNode } from "react";
import { Insignia } from "@/components/modulo";

// Piezas que comparten la ficha del egresado (coordinador, E2-E3) y el portal del egresado (G2, G4).

export type Empleo = {
  id: number;
  tipo: "Empleo" | "Beca" | "Otro";
  puesto: string | null;
  empresa: string | null;
  area: string | null;
  inicio: string | null;
  fin: string | null;
  relacion: boolean | null;
  sueldo?: number | null;
  vigente: boolean;
};
export type Comentario = { fecha: string | null; comentario: string | null; sugerencia: string | null };

const meses = ["Ene", "Feb", "Mar", "Abr", "May", "Jun", "Jul", "Ago", "Sep", "Oct", "Nov", "Dic"];
const mes = (iso: string | null) => (iso ? `${meses[Number(iso.slice(5, 7)) - 1]} ${iso.slice(0, 4)}` : "—");
export const fechaCorta = (iso: string | null) => (iso ? iso.slice(0, 10).split("-").reverse().join("/") : "—");

// Tarjeta de empleo (Figma G2): el vigente va resaltado con la insignia «Actual».
export function TarjetaEmpleo({ empleo: e, acciones }: { empleo: Empleo; acciones?: ReactNode }) {
  return (
    <section className={`flex flex-col gap-4 rounded-xl border px-6 py-[22px] ${e.vigente ? "border-portal-info-borde bg-portal-activo-fondo" : "border-portal-borde bg-blanco"}`}>
      <div className="flex items-center justify-between gap-3">
        <h2 className="text-[17px] font-semibold text-portal-titulo">{e.puesto}</h2>
        <div className="flex items-center gap-3">
          {e.tipo !== "Empleo" && <Insignia tono="gris">{e.tipo}</Insignia>}
          {e.vigente && <Insignia tono="verde">Actual</Insignia>}
          {acciones}
        </div>
      </div>
      <p className="text-sm font-medium text-portal-menu-texto">{e.empresa}</p>
      <dl className="flex gap-8">
        {[["Área", e.area || "—"], ["Periodo", `${mes(e.inicio)} – ${e.vigente && !e.fin ? "actual" : mes(e.fin)}`], ["Relación con la maestría", e.relacion === null ? "—" : e.relacion ? "Sí" : "No"]].map(([t, v]) => (
          <div key={t} className="flex flex-col gap-0.5">
            <dt className="text-[13px] text-portal-subtexto">{t}</dt>
            <dd className="text-sm font-medium text-portal-titulo">{v}</dd>
          </div>
        ))}
      </dl>
    </section>
  );
}

// Lista de comentarios enviados (Figma G4, tarjeta «Enviadas»).
export function ListaComentarios({ comentarios }: { comentarios: Comentario[] }) {
  return (
    <ul>
      {comentarios.map((c, i) => (
        <li key={i} className="flex flex-col gap-1 border-t border-portal-borde py-2.5">
          <span className="text-[13px] text-portal-subtexto">{fechaCorta(c.fecha)}</span>
          {c.comentario && <p className="text-sm text-portal-menu-texto">{c.comentario}</p>}
          {c.sugerencia && <p className="text-sm text-portal-subtexto">Sugerencia: {c.sugerencia}</p>}
        </li>
      ))}
    </ul>
  );
}
