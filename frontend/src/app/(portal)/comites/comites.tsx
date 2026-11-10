"use client";

import Link from "next/link";
import { useEffect, useMemo, useState } from "react";
import { Eye, Search } from "lucide-react";
import { EncabezadoPagina } from "@/components/portal";
import { FiltroMultiple, Insignia, Paginacion, Pestanas } from "@/components/modulo";
import { sesionVencida } from "@/lib/acceso";
import { fecha } from "../alumnos/[clave]/pestanas";

// Consulta de comités (Figma CM1).

type Comite = {
  id: number;
  clave: string;
  claveAlumno: number;
  alumno: string;
  modalidad: string | null;
  generacion: string | null;
  director: string | null;
  integrantes: { nombre: string; director: boolean }[];
  estado: string;
  grupo: "Completos" | "Incompletos" | "Concluidos";
  desde: string | null;
};

const POR_PAGINA = 10;
const pestanas = ["Todos", "Completos", "Incompletos", "Concluidos"] as const;
type Pestana = (typeof pestanas)[number];

export const iniciales = (nombre: string) => nombre.split(" ").slice(0, 2).map((p) => p[0]).join("").toUpperCase();
const normalizar = (s: string) => s.normalize("NFD").replace(/\p{Diacritic}/gu, "").toLowerCase();

export function ConsultaComites() {
  const [comites, setComites] = useState<Comite[] | null>(null);
  const [pestana, setPestana] = useState<Pestana>("Todos");
  const [busqueda, setBusqueda] = useState("");
  const [generaciones, setGeneraciones] = useState<string[]>([]);
  const [modalidades, setModalidades] = useState<string[]>([]);
  const [pag, setPag] = useState(1);

  useEffect(() => {
    fetch("/api/comites").then(async (res) => {
      if (res.status === 401) sesionVencida();
      else if (res.ok) setComites(await res.json());
    });
  }, []);

  const { conteos, visibles, opciones } = useMemo(() => {
    const todos = comites ?? [];
    const texto = normalizar(busqueda.trim());
    const filtrados = todos.filter((c) =>
      (!texto || normalizar(`${c.clave} ${c.alumno} ${c.integrantes.map((i) => i.nombre).join(" ")}`).includes(texto))
      && (!generaciones.length || generaciones.includes(c.generacion ?? ""))
      && (!modalidades.length || modalidades.includes(c.modalidad ?? "")));
    const en = (p: Pestana) => (c: Comite) => p === "Todos" || c.grupo === p;
    const distintos = (de: (c: Comite) => string) =>
      [...new Set(todos.map(de).filter(Boolean))].sort().map((valor) => ({ valor, conteo: todos.filter((c) => de(c) === valor).length }));
    return {
      conteos: pestanas.map((p) => ({ valor: p, texto: p, conteo: filtrados.filter(en(p)).length })),
      visibles: filtrados.filter(en(pestana)),
      opciones: { generaciones: distintos((c) => c.generacion ?? ""), modalidades: distintos((c) => c.modalidad ?? "") },
    };
  }, [comites, pestana, busqueda, generaciones, modalidades]);

  const total = Math.max(1, Math.ceil(visibles.length / POR_PAGINA));
  const actual = Math.min(pag, total);
  const enPagina = visibles.slice((actual - 1) * POR_PAGINA, actual * POR_PAGINA);

  return (
    <>
      <EncabezadoPagina titulo="Comités tutoriales" descripcion="Comités tutoriales de los alumnos." />
      <section className="rounded-xl border border-portal-borde bg-blanco">
        <Pestanas pestanas={conteos} actual={pestana} onCambio={(p) => { setPestana(p); setPag(1); }} />
        <div className="flex flex-wrap items-center gap-2.5 px-5 py-4">
          <label className="flex w-[330px] items-center gap-2 rounded-lg border border-portal-borde-campo bg-blanco px-3 py-2.5">
            <Search size={18} className="text-portal-placeholder" aria-hidden />
            <input type="text" value={busqueda} onChange={(e) => { setBusqueda(e.target.value); setPag(1); }}
              placeholder="Buscar por clave, alumno o profesor" aria-label="Buscar por clave, alumno o profesor"
              className="flex-1 text-[15px] text-portal-titulo outline-none placeholder:text-portal-placeholder" />
          </label>
          <FiltroMultiple etiqueta="Generación" titulo="Generación" opciones={opciones.generaciones} seleccion={generaciones} onAplicar={(v) => { setGeneraciones(v); setPag(1); }} />
          <FiltroMultiple etiqueta="Modalidad" titulo="Modalidad" opciones={opciones.modalidades} seleccion={modalidades} onAplicar={(v) => { setModalidades(v); setPag(1); }} />
        </div>
        <table className="w-full table-fixed border-t border-portal-borde text-left">
          <colgroup><col className="w-[160px]" /><col className="w-[250px]" /><col className="w-[230px]" /><col className="w-[150px]" /><col className="w-[160px]" /><col /><col className="w-[70px]" /></colgroup>
          <thead className="bg-portal-tabla-cabecera text-[13px] text-portal-subtexto">
            <tr className="border-b border-portal-borde">
              <th className="py-2.5 pl-5 font-semibold">Comité</th>
              <th className="font-semibold">Alumno</th>
              <th className="font-semibold">Director</th>
              <th className="font-semibold">Integrantes</th>
              <th className="font-semibold">Estado</th>
              <th className="font-semibold">Desde</th>
              <th><span className="sr-only">Ver detalle</span></th>
            </tr>
          </thead>
          <tbody className="text-sm text-portal-menu-texto">
            {comites && visibles.length === 0 && (
              <tr className="border-b border-portal-borde"><td colSpan={7} className="py-4 pl-5 text-portal-placeholder">Ningún comité coincide con los filtros.</td></tr>
            )}
            {enPagina.map((c) => (
              <tr key={c.id} className="border-b border-portal-borde">
                <td className="py-3 pl-5 font-semibold text-portal-titulo">{c.clave}</td>
                <td>
                  <p className="font-semibold text-portal-titulo">{c.alumno}</p>
                  <p className="mt-[3px] text-[13px] text-portal-subtexto">{c.modalidad}</p>
                </td>
                <td>{c.director ?? <span className="text-portal-placeholder">—</span>}</td>
                <td>
                  <span className="flex" aria-label={c.integrantes.map((i) => i.nombre).join(", ")}>
                    {c.integrantes.map((i) => (
                      <span key={i.nombre} title={i.nombre} aria-hidden
                        className={`-mr-2 flex size-[30px] items-center justify-center rounded-full border-2 border-blanco text-[11px] font-semibold last:mr-0 ${i.director ? "bg-portal-activo text-blanco" : "bg-portal-neutro-fondo text-portal-subtexto"}`}>
                        {iniciales(i.nombre)}
                      </span>
                    ))}
                  </span>
                </td>
                <td><Insignia tono={c.grupo === "Completos" ? "verde" : c.grupo === "Concluidos" ? "gris" : "ambar"}>{c.estado}</Insignia></td>
                <td>{fecha(c.desde)}</td>
                <td className="pr-5 text-right">
                  <Link href={`/comites/${c.id}`} aria-label={`Ver comité ${c.clave}`} className="inline-flex size-[34px] items-center justify-center rounded-lg border border-portal-borde-campo text-portal-activo">
                    <Eye size={18} aria-hidden />
                  </Link>
                </td>
              </tr>
            ))}
          </tbody>
        </table>
        <div className="flex items-center justify-between px-5 py-3.5">
          <p className="text-sm text-portal-subtexto">
            {visibles.length === 0 ? "Mostrando 0 comités"
              : `Mostrando ${(actual - 1) * POR_PAGINA + 1}–${(actual - 1) * POR_PAGINA + enPagina.length} de ${visibles.length} comités`}
          </p>
          {total > 1 && <Paginacion actual={actual} total={total} onCambio={setPag} />}
        </div>
      </section>
    </>
  );
}
