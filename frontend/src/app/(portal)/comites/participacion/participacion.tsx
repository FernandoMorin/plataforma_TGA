"use client";

import Link from "next/link";
import { useEffect, useState } from "react";
import { Search } from "lucide-react";
import { EncabezadoPagina } from "@/components/portal";
import { Insignia } from "@/components/modulo";
import { sesionVencida } from "@/lib/acceso";
import { iniciales } from "../comites";

// Participación docente (Figma CM4): los comités de un profesor y su estado en cada uno.

type Profesor = { rpe: string; nombre: string };
type Datos = {
  rpe: string;
  nombre: string;
  correo: string | null;
  activos: number;
  director: number;
  concluidos: number;
  participaciones: { idComite: number; comite: string; alumno: string; trabajo: string | null; rol: string; participacion: "Activa" | "Concluida" | "Reemplazada" }[];
};

const tonoParticipacion = { Activa: "verde", Concluida: "gris", Reemplazada: "ambar" } as const;

export function Participacion() {
  const [profesores, setProfesores] = useState<Profesor[]>([]);
  const [texto, setTexto] = useState("");
  const [datos, setDatos] = useState<Datos | null>(null);
  const elegido = profesores.find((p) => p.nombre === texto);

  useEffect(() => {
    fetch("/api/comites/participacion").then(async (res) => {
      if (res.status === 401) sesionVencida();
      else if (res.ok) setProfesores(await res.json());
    });
  }, []);
  useEffect(() => {
    if (!elegido) return;
    fetch(`/api/comites/participacion/${encodeURIComponent(elegido.rpe)}`).then(async (res) => res.ok && setDatos(await res.json()));
  }, [elegido]);

  const mostrado = elegido && datos?.rpe === elegido.rpe ? datos : null;
  return (
    <>
      <EncabezadoPagina titulo="Participación docente" descripcion="Comités en los que participa cada profesor." />
      <label className="flex w-[420px] items-center gap-2 rounded-lg border border-portal-borde-campo bg-blanco px-3 py-2.5">
        <Search size={18} className="text-portal-placeholder" aria-hidden />
        <input type="text" list="profesores" value={texto} onChange={(e) => setTexto(e.target.value)}
          placeholder="Buscar profesor" aria-label="Buscar profesor"
          className="flex-1 text-[15px] text-portal-titulo outline-none placeholder:text-portal-placeholder" />
        <datalist id="profesores">{profesores.map((p) => <option key={p.rpe} value={p.nombre} />)}</datalist>
      </label>

      {mostrado && (
        <>
          <section className="flex items-center gap-4 rounded-xl border border-portal-borde bg-blanco px-5 py-[18px]">
            <span aria-hidden className="flex size-[52px] items-center justify-center rounded-full bg-portal-activo-fondo text-[17px] font-bold text-portal-activo">{iniciales(mostrado.nombre)}</span>
            <div className="flex flex-1 flex-col gap-1">
              <h2 className="text-lg font-semibold text-portal-titulo">{mostrado.nombre}</h2>
              <p className="text-sm text-portal-subtexto">{mostrado.rpe.startsWith("EXT-") ? "Clave" : "RPE"} {mostrado.rpe}{mostrado.correo && ` · ${mostrado.correo}`}</p>
            </div>
            <dl className="flex gap-6">
              {[["Comités activos", mostrado.activos], ["Como director", mostrado.director], ["Concluidos", mostrado.concluidos]].map(([etiqueta, valor]) => (
                <div key={etiqueta} className="flex flex-col-reverse items-center gap-0.5 pl-6">
                  <dt className="text-[13px] text-portal-subtexto">{etiqueta}</dt>
                  <dd className="text-[22px] font-bold text-portal-titulo">{valor}</dd>
                </div>
              ))}
            </dl>
          </section>

          <section className="overflow-hidden rounded-xl border border-portal-borde bg-blanco">
            <table className="w-full table-fixed text-left">
              <colgroup><col className="w-[170px]" /><col className="w-[250px]" /><col /><col className="w-[140px]" /><col className="w-[150px]" /></colgroup>
              <thead className="bg-portal-tabla-cabecera text-[13px] text-portal-subtexto">
                <tr>
                  <th className="py-2.5 pl-5 font-semibold">Comité</th>
                  <th className="font-semibold">Alumno</th>
                  <th className="font-semibold">Trabajo</th>
                  <th className="font-semibold">Rol</th>
                  <th className="font-semibold">Participación</th>
                </tr>
              </thead>
              <tbody className="text-sm text-portal-menu-texto">
                {mostrado.participaciones.length === 0 && (
                  <tr className="border-t border-portal-borde"><td colSpan={5} className="py-4 pl-5 text-portal-placeholder">No participa en ningún comité.</td></tr>
                )}
                {mostrado.participaciones.map((p, i) => (
                  <tr key={i} className="border-t border-portal-borde">
                    <td className="py-3 pl-5"><Link href={`/comites/${p.idComite}`} className="font-semibold text-portal-titulo hover:underline">{p.comite}</Link></td>
                    <td>{p.alumno}</td>
                    <td className="truncate pr-4" title={p.trabajo ?? undefined}>{p.trabajo}</td>
                    <td><Insignia tono={p.rol === "Director" ? "azul" : "gris"}>{p.rol}</Insignia></td>
                    <td><Insignia tono={tonoParticipacion[p.participacion]}>{p.participacion}</Insignia></td>
                  </tr>
                ))}
              </tbody>
            </table>
          </section>
        </>
      )}
    </>
  );
}
