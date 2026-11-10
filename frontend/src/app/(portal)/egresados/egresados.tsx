"use client";

import Link from "next/link";
import { useEffect, useMemo, useState } from "react";
import { Eye, Search } from "lucide-react";
import { EncabezadoPagina } from "@/components/portal";
import { FiltroMultiple, Insignia, Paginacion, Pestanas, type Tono } from "@/components/modulo";
import { sesionVencida } from "@/lib/acceso";
import { fecha } from "../alumnos/[clave]/pestanas";

// Consulta de egresados (Figma E1).

type Egresado = {
  clave: number;
  identificador: string | null;
  generacion: string | null;
  nombre: string;
  situacion: string;
  empleoActual: string | null;
  areas: string[];
  relacion: "Sí" | "No" | null;
  actualizado: string | null;
  sinActualizar: boolean;
  estado: "Localizable" | "No localizable";
  activo: boolean;
};

export const tonoSituacion: Record<string, Tono> = { "Con trabajo": "verde", Becario: "azul", Otro: "gris", "Sin trabajo": "gris" };
const POR_PAGINA = 10;
const pestanas = ["Todos", "Localizables", "No localizables", "Sin actualizar", "Dados de baja"] as const;
type Pestana = (typeof pestanas)[number];
// Como en Figma, Todos = localizables + no localizables + dados de baja; los dados de baja no cuentan en las demás.
const en: Record<Pestana, (e: Egresado) => boolean> = {
  Todos: () => true,
  Localizables: (e) => e.activo && e.estado === "Localizable",
  "No localizables": (e) => e.activo && e.estado === "No localizable",
  "Sin actualizar": (e) => e.activo && e.sinActualizar,
  "Dados de baja": (e) => !e.activo,
};
const normalizar = (s: string) => s.normalize("NFD").replace(/\p{Diacritic}/gu, "").toLowerCase();

export function ConsultaEgresados() {
  const [egresados, setEgresados] = useState<Egresado[] | null>(null);
  const [pestana, setPestana] = useState<Pestana>("Todos");
  const [busqueda, setBusqueda] = useState("");
  const [generaciones, setGeneraciones] = useState<string[]>([]);
  const [situaciones, setSituaciones] = useState<string[]>([]);
  const [areas, setAreas] = useState<string[]>([]);
  const [relaciones, setRelaciones] = useState<string[]>([]);
  const [pag, setPag] = useState(1);

  useEffect(() => {
    fetch("/api/egresados").then(async (res) => {
      if (res.status === 401) sesionVencida();
      else if (res.ok) setEgresados(await res.json());
    });
  }, []);

  const { conteos, visibles, opciones } = useMemo(() => {
    const todos = egresados ?? [];
    const texto = normalizar(busqueda.trim());
    const filtrados = todos.filter((e) => (!texto || normalizar(`${e.nombre} ${e.identificador ?? ""}`).includes(texto))
      && (!generaciones.length || generaciones.includes(e.generacion ?? ""))
      && (!situaciones.length || situaciones.includes(e.situacion))
      && (!areas.length || e.areas.some((a) => areas.includes(a)))
      && (!relaciones.length || relaciones.includes(e.relacion ?? "")));
    const contar = (valores: string[], tiene: (e: Egresado, v: string) => boolean) =>
      valores.map((valor) => ({ valor, conteo: todos.filter((e) => tiene(e, valor)).length }));
    const distintos = (lista: string[]) => [...new Set(lista.filter(Boolean))].sort();
    return {
      conteos: pestanas.map((p) => ({ valor: p, texto: p, conteo: filtrados.filter(en[p]).length })),
      visibles: filtrados.filter(en[pestana]),
      opciones: {
        generaciones: contar(distintos(todos.map((e) => e.generacion ?? "")), (e, v) => e.generacion === v),
        situaciones: contar(["Con trabajo", "Becario", "Otro", "Sin trabajo"], (e, v) => e.situacion === v),
        areas: contar(distintos(todos.flatMap((e) => e.areas)), (e, v) => e.areas.includes(v)),
        relaciones: contar(["Sí", "No"], (e, v) => e.relacion === v),
      },
    };
  }, [egresados, pestana, busqueda, generaciones, situaciones, areas, relaciones]);

  const total = Math.max(1, Math.ceil(visibles.length / POR_PAGINA));
  const actual = Math.min(pag, total);
  const enPagina = visibles.slice((actual - 1) * POR_PAGINA, actual * POR_PAGINA);
  const filtro = (set: (v: string[]) => void) => (v: string[]) => { set(v); setPag(1); };

  return (
    <>
      <EncabezadoPagina titulo="Consulta de egresados" descripcion="Egresados de la maestría y su situación actual." />
      <section className="rounded-xl border border-portal-borde bg-blanco">
        <Pestanas pestanas={conteos} actual={pestana} onCambio={(p) => { setPestana(p); setPag(1); }} />
        <div className="flex flex-wrap items-center gap-2.5 px-5 py-4">
          <label className="flex w-[320px] items-center gap-2 rounded-lg border border-portal-borde-campo bg-blanco px-3 py-2.5">
            <Search size={18} className="text-portal-placeholder" aria-hidden />
            <input type="text" value={busqueda} onChange={(e) => { setBusqueda(e.target.value); setPag(1); }}
              placeholder="Buscar por nombre o identificador" aria-label="Buscar por nombre o identificador"
              className="flex-1 text-[15px] text-portal-titulo outline-none placeholder:text-portal-placeholder" />
          </label>
          <FiltroMultiple etiqueta="Generación" titulo="Generación" opciones={opciones.generaciones} seleccion={generaciones} onAplicar={filtro(setGeneraciones)} />
          <FiltroMultiple etiqueta="Situación laboral" titulo="Situación laboral" opciones={opciones.situaciones} seleccion={situaciones} onAplicar={filtro(setSituaciones)} />
          <FiltroMultiple etiqueta="Área profesional" titulo="Área profesional" opciones={opciones.areas} seleccion={areas} onAplicar={filtro(setAreas)} />
          <FiltroMultiple etiqueta="Relación con la maestría" titulo="Relación con la maestría" opciones={opciones.relaciones} seleccion={relaciones} onAplicar={filtro(setRelaciones)} />
        </div>
        <table className="w-full table-fixed border-t border-portal-borde text-left">
          <colgroup><col className="w-[240px]" /><col className="w-[110px]" /><col className="w-[150px]" /><col /><col className="w-[120px]" /><col className="w-[130px]" /><col className="w-[60px]" /></colgroup>
          <thead className="bg-portal-tabla-cabecera text-[13px] text-portal-subtexto">
            <tr className="border-b border-portal-borde">
              <th className="py-2.5 pl-5 font-semibold">Egresado</th>
              <th className="font-semibold">Generación</th>
              <th className="font-semibold">Situación laboral</th>
              <th className="font-semibold">Empleo actual</th>
              <th className="font-semibold">Actualizado</th>
              <th className="font-semibold">Estado</th>
              <th><span className="sr-only">Ver ficha</span></th>
            </tr>
          </thead>
          <tbody className="text-sm text-portal-menu-texto">
            {egresados && visibles.length === 0 && (
              <tr className="border-b border-portal-borde"><td colSpan={7} className="py-4 pl-5 text-portal-placeholder">Ningún egresado coincide con los filtros.</td></tr>
            )}
            {enPagina.map((e) => (
              <tr key={e.clave} className="border-b border-portal-borde">
                <td className="py-3 pl-5">
                  <p className="font-semibold text-portal-titulo">{e.nombre}</p>
                  <p className="mt-[3px] text-[13px] text-portal-subtexto">{e.identificador}</p>
                </td>
                <td>{e.generacion}</td>
                <td><Insignia tono={tonoSituacion[e.situacion] ?? "gris"}>{e.situacion}</Insignia></td>
                <td className="truncate pr-4">{e.empleoActual || <span className="text-portal-placeholder">—</span>}</td>
                <td className={e.sinActualizar ? "font-semibold text-portal-advertencia-texto" : ""}>{fecha(e.actualizado)}</td>
                <td>{e.activo ? <Insignia tono={e.estado === "Localizable" ? "verde" : "rojo"}>{e.estado}</Insignia> : <Insignia tono="gris">Dado de baja</Insignia>}</td>
                <td className="pr-5 text-right">
                  <Link href={`/egresados/${e.clave}`} aria-label={`Ver ficha de ${e.nombre}`} className="inline-flex size-[34px] items-center justify-center rounded-lg border border-portal-borde-campo text-portal-activo">
                    <Eye size={18} aria-hidden />
                  </Link>
                </td>
              </tr>
            ))}
          </tbody>
        </table>
        <div className="flex items-center justify-between px-5 py-3.5">
          <p className="text-sm text-portal-subtexto">
            {visibles.length === 0 ? "Mostrando 0 egresados"
              : `Mostrando ${(actual - 1) * POR_PAGINA + 1}–${(actual - 1) * POR_PAGINA + enPagina.length} de ${visibles.length} egresados`}
          </p>
          {total > 1 && <Paginacion actual={actual} total={total} onCambio={setPag} />}
        </div>
      </section>
    </>
  );
}
