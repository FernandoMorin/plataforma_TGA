"use client";

import { sesionVencida } from "@/lib/acceso";

import Link from "next/link";
import { Fragment, useEffect, useMemo, useState } from "react";
import { Eye, Search, UserPlus } from "lucide-react";
import { EncabezadoPagina } from "@/components/portal";
import { BarraAvance, FiltroMultiple, FiltroRango, Insignia, Paginacion, Pestanas, type Tono } from "@/components/modulo";
import { pagina } from "@/lib/menu";

// Consulta de alumnos (Figma A1 y A2). GET /api/alumnos devuelve a todos; aquí se filtra, se cuenta y se pagina.

type Alumno = {
  clave: number;
  nombre: string;
  correo: string | null;
  generacion: string | null;
  semestre: number | null;
  estadoAcademico: string;
  modalidad: string;
  toefl: string;
  titulacion: string;
  avance: number | null;
  rezago: string;
};

const POR_PAGINA = 8;

export const tonoEstado: Record<string, Tono> = { Activo: "verde", Titulado: "azul", "Baja temporal": "ambar", "Baja definitiva": "rojo" };
export const tonoToefl: Record<string, Tono> = { Aprobado: "verde", Presentado: "ambar", Pendiente: "gris" };
export const tonoTitulacion: Record<string, Tono> = {
  "Solicitud pendiente": "gris", "Sin iniciar": "gris", "En progreso": "ambar", "En evaluación": "azul", Aceptada: "verde", Cancelada: "rojo",
};

const pestanas = [
  { valor: "Todos", texto: "Todos" },
  { valor: "Activo", texto: "Activos" },
  { valor: "Baja temporal", texto: "Baja temporal" },
  { valor: "Titulado", texto: "Titulados" },
  { valor: "Baja definitiva", texto: "Baja definitiva" },
] as const;
type Pestana = (typeof pestanas)[number]["valor"];

type Filtro = { clave: string; etiqueta: string; titulo: string; valor: (a: Alumno) => string | undefined; fijas?: string[] };
const filtros: Filtro[] = [
  { clave: "generacion", etiqueta: "Generación", titulo: "Generación", valor: (a) => a.generacion ?? undefined },
  { clave: "semestre", etiqueta: "Semestre", titulo: "Semestre", valor: (a) => a.semestre?.toString() },
  { clave: "modalidad", etiqueta: "Modalidad", titulo: "Modalidad de titulación", valor: (a) => a.modalidad, fijas: ["Sin asignar", "Tesis", "Artículo científico"] },
  { clave: "toefl", etiqueta: "TOEFL", titulo: "Estado del requisito TOEFL", valor: (a) => a.toefl, fijas: ["Pendiente", "Presentado", "Aprobado"] },
  {
    clave: "titulacion", etiqueta: "Titulación", titulo: "Estado de titulación", valor: (a) => a.titulacion,
    fijas: ["Sin trabajo registrado", "Solicitud pendiente", "Sin iniciar", "En progreso", "En evaluación", "Aceptada", "Cancelada"],
  },
  { clave: "rezago", etiqueta: "Rezago", titulo: "Rezago por avance", valor: (a) => a.rezago, fijas: ["En rezago", "Al corriente", "No aplica"] },
];

// Búsqueda sin distinguir mayúsculas ni acentos.
const normalizar = (s: string) => s.normalize("NFD").replace(/\p{Diacritic}/gu, "").toLowerCase();

export function ConsultaAlumnos() {
  const [alumnos, setAlumnos] = useState<Alumno[]>([]);
  const [pestana, setPestana] = useState<Pestana>("Todos");
  const [busqueda, setBusqueda] = useState("");
  const [seleccion, setSeleccion] = useState<Record<string, string[]>>({});
  const [avance, setAvance] = useState<[number, number] | null>(null);
  const [pag, setPag] = useState(1);

  useEffect(() => {
    fetch("/api/alumnos").then(async (res) => {
      if (res.status === 401) sesionVencida();
      else if (res.ok) setAlumnos(await res.json());
    });
  }, []);

  const { conteoPestanas, visibles, opciones } = useMemo(() => {
    const texto = normalizar(busqueda.trim());
    const busca = (a: Alumno) => !texto || normalizar(`${a.clave} ${a.nombre} ${a.correo ?? ""}`).includes(texto);
    const enPestana = (a: Alumno) => pestana === "Todos" || a.estadoAcademico === pestana;
    // Cumple la búsqueda y todos los filtros, menos el indicado (para contar las opciones de ese filtro).
    // Con rango de avance solo quedan los alumnos con avance registrado (trabajo aceptado) dentro del rango.
    const enRango = (a: Alumno) => !avance || (a.avance !== null && a.avance >= avance[0] && a.avance <= avance[1]);
    const cumple = (a: Alumno, excepto?: string) =>
      busca(a) && enRango(a) &&
      filtros.every((f) => f.clave === excepto || !seleccion[f.clave]?.length || seleccion[f.clave].includes(f.valor(a) ?? ""));

    const filtrados = alumnos.filter((a) => cumple(a));
    const opciones = Object.fromEntries(
      filtros.map((f) => {
        const base = alumnos.filter((a) => enPestana(a) && cumple(a, f.clave));
        const valores = f.fijas ?? [...new Set(alumnos.map(f.valor).filter((v): v is string => !!v))].sort((x, y) => x.localeCompare(y, "es", { numeric: true }));
        return [f.clave, valores.map((valor) => ({ valor, conteo: base.filter((a) => f.valor(a) === valor).length }))];
      }),
    );
    return {
      conteoPestanas: pestanas.map((p) => ({ ...p, conteo: filtrados.filter((a) => p.valor === "Todos" || a.estadoAcademico === p.valor).length })),
      visibles: filtrados.filter(enPestana),
      opciones,
    };
  }, [alumnos, pestana, busqueda, seleccion, avance]);

  const totalPaginas = Math.max(1, Math.ceil(visibles.length / POR_PAGINA));
  const actual = Math.min(pag, totalPaginas);
  const enPagina = visibles.slice((actual - 1) * POR_PAGINA, actual * POR_PAGINA);
  const hayFiltros = !!avance || Object.values(seleccion).some((v) => v.length);
  const p = pagina("/alumnos")!;

  return (
    <>
      <EncabezadoPagina titulo={p.titulo} descripcion={p.descripcion}>
        <Link href="/alumnos/importar" className="flex items-center gap-2 rounded-lg bg-portal-activo py-2.5 pr-[18px] pl-4 text-[15px] font-semibold text-blanco">
          <UserPlus size={18} aria-hidden />
          Importar alumnos
        </Link>
      </EncabezadoPagina>

      <section className="rounded-xl border border-portal-borde bg-blanco">
        <Pestanas pestanas={conteoPestanas} actual={pestana} onCambio={(v) => { setPestana(v); setPag(1); }} />

        <div className="flex flex-wrap items-center gap-2.5 px-5 py-4">
          <label className="flex w-[300px] items-center gap-2 rounded-lg border border-portal-borde-campo bg-blanco px-3 py-2.5">
            <Search size={18} className="text-portal-placeholder" aria-hidden />
            <input
              type="text"
              value={busqueda}
              onChange={(e) => { setBusqueda(e.target.value); setPag(1); }}
              placeholder="Buscar por clave, nombre o correo"
              aria-label="Buscar por clave, nombre o correo"
              className="flex-1 text-[15px] text-portal-titulo outline-none placeholder:text-portal-placeholder"
            />
          </label>
          {filtros.map((f) => (
            <Fragment key={f.clave}>
              {/* «Avance» va antes de «Rezago», en el orden de Figma. */}
              {f.clave === "rezago" && (
                <FiltroRango etiqueta="Avance" titulo="Avance de titulación" unidad="%" min={0} max={100} paso={5} rango={avance}
                  onAplicar={(r) => { setAvance(r); setPag(1); }} />
              )}
              <FiltroMultiple
                etiqueta={f.etiqueta}
                titulo={f.titulo}
                opciones={opciones[f.clave]}
                seleccion={seleccion[f.clave] ?? []}
                onAplicar={(valores) => { setSeleccion({ ...seleccion, [f.clave]: valores }); setPag(1); }}
              />
            </Fragment>
          ))}
          {hayFiltros && (
            <button onClick={() => { setSeleccion({}); setAvance(null); setPag(1); }} className="text-sm font-semibold text-portal-activo">
              Limpiar filtros
            </button>
          )}
        </div>

        <table className="w-full table-fixed border-t border-portal-borde text-left">
          <colgroup>
            <col className="w-[310px]" /><col className="w-[100px]" /><col className="w-[50px]" /><col className="w-[140px]" />
            <col className="w-[140px]" /><col className="w-[110px]" /><col className="w-[186px]" /><col />
          </colgroup>
          <thead className="bg-portal-tabla-cabecera text-[13px] font-semibold text-portal-subtexto">
            <tr className="border-b border-portal-borde">
              <th className="py-2.5 pl-5 font-semibold">Alumno</th>
              <th className="font-semibold">Generación</th>
              <th className="font-semibold">Sem.</th>
              <th className="font-semibold">Estado académico</th>
              <th className="font-semibold">Modalidad</th>
              <th className="font-semibold">TOEFL</th>
              <th className="font-semibold">Titulación</th>
              <th><span className="sr-only">Acciones</span></th>
            </tr>
          </thead>
          <tbody className="text-sm text-portal-menu-texto">
            {enPagina.map((a) => (
              <tr key={a.clave} className="border-b border-portal-borde">
                <td className="py-3 pl-5">
                  <p className="font-semibold text-portal-titulo">{a.nombre}</p>
                  <p className="mt-[3px] text-[13px] text-portal-subtexto">{a.clave}{a.correo && ` · ${a.correo}`}</p>
                </td>
                <td>{a.generacion}</td>
                <td>{a.semestre}</td>
                <td><Insignia tono={tonoEstado[a.estadoAcademico] ?? "gris"}>{a.estadoAcademico}</Insignia></td>
                <td>{a.modalidad}</td>
                <td><Insignia tono={tonoToefl[a.toefl] ?? "gris"}>{a.toefl}</Insignia></td>
                <td>
                  {a.titulacion === "Sin trabajo registrado" ? (
                    <span className="text-[13px] text-portal-placeholder">{a.titulacion}</span>
                  ) : (
                    <span className="flex flex-col items-start gap-1.5">
                      <Insignia tono={tonoTitulacion[a.titulacion] ?? "gris"}>{a.titulacion}</Insignia>
                      {a.avance !== null && <BarraAvance porcentaje={a.avance} />}
                    </span>
                  )}
                </td>
                <td className="pr-5 text-right">
                  <Link href={`/alumnos/${a.clave}`} aria-label={`Ver ficha de ${a.nombre}`} className="inline-flex size-[34px] items-center justify-center rounded-lg border border-portal-borde-campo text-portal-activo">
                    <Eye size={18} aria-hidden />
                  </Link>
                </td>
              </tr>
            ))}
          </tbody>
        </table>

        <div className="flex items-center justify-between px-5 py-3.5">
          <p className="text-sm text-portal-subtexto">
            {visibles.length === 0
              ? "Mostrando 0 alumnos"
              : `Mostrando ${(actual - 1) * POR_PAGINA + 1}–${(actual - 1) * POR_PAGINA + enPagina.length} de ${visibles.length} alumnos`}
          </p>
          {totalPaginas > 1 && <Paginacion actual={actual} total={totalPaginas} onCambio={setPag} />}
        </div>
      </section>
    </>
  );
}
