"use client";

import Link from "next/link";
import { useEffect, useMemo, useState } from "react";
import { Eye, Search } from "lucide-react";
import { EncabezadoPagina } from "@/components/portal";
import { BarraAvance, FiltroMultiple, Insignia, Pestanas, type Tono } from "@/components/modulo";
import { sesionVencida } from "@/lib/acceso";

// Titulación del coordinador (Figma CT1): todos los trabajos, con pestañas por etapa y filtros.

type Trabajo = {
  id: number;
  clave: number;
  alumno: string;
  titulo: string | null;
  modalidad: string | null;
  generacion: string | null;
  director: string;
  comite: "Completo" | "Incompleto" | "Sin armar" | null;
  avance: number | null;
  informe: string | null;
  enCurso: string | null;
  grupo: string;
};

const pestanas = ["Todos", "Solicitudes", "Sin comité", "En progreso", "En evaluación", "Concluidos"] as const;
type Pestana = (typeof pestanas)[number];

const tonoInforme = (t: Trabajo): Tono =>
  t.informe === "Concluido" ? "verde" : t.informe === "Cancelado" ? "rojo" : t.informe === "Solicitud pendiente" || t.enCurso === "En consolidación" ? "ambar" : "azul";
const etiquetaInforme = (t: Trabajo) => (t.enCurso === "Abierta" ? "Abierto" : t.enCurso ?? "Sin informe en curso");
const normalizar = (s: string) => s.normalize("NFD").replace(/\p{Diacritic}/gu, "").toLowerCase();

export function ConsultaTitulacion() {
  const [trabajos, setTrabajos] = useState<Trabajo[] | null>(null);
  const [pestana, setPestana] = useState<Pestana>("Todos");
  const [busqueda, setBusqueda] = useState("");
  const [modalidades, setModalidades] = useState<string[]>([]);
  const [generaciones, setGeneraciones] = useState<string[]>([]);
  const [directores, setDirectores] = useState<string[]>([]);
  const [informes, setInformes] = useState<string[]>([]);

  useEffect(() => {
    fetch("/api/titulacion").then(async (res) => {
      if (res.status === 401) sesionVencida();
      else if (res.ok) setTrabajos(await res.json());
    });
  }, []);

  const { conteos, visibles, opciones } = useMemo(() => {
    const todos = trabajos ?? [];
    const texto = normalizar(busqueda.trim());
    const filtros: [string[], (t: Trabajo) => string][] = [
      [modalidades, (t) => t.modalidad ?? ""], [generaciones, (t) => t.generacion ?? ""],
      [directores, (t) => t.director], [informes, etiquetaInforme],
    ];
    const filtrados = todos.filter((t) => (!texto || normalizar(`${t.alumno} ${t.clave} ${t.titulo ?? ""}`).includes(texto))
      && filtros.every(([sel, de]) => !sel.length || sel.includes(de(t))));
    const enPestana = (p: Pestana) => (t: Trabajo) => p === "Todos" || t.grupo === p;
    const distintos = (de: (t: Trabajo) => string) =>
      [...new Set(todos.map(de).filter(Boolean))].sort().map((valor) => ({ valor, conteo: todos.filter((t) => de(t) === valor).length }));
    return {
      conteos: pestanas.map((p) => ({ valor: p, texto: p, conteo: filtrados.filter(enPestana(p)).length })),
      visibles: filtrados.filter(enPestana(pestana)),
      opciones: {
        modalidades: distintos((t) => t.modalidad ?? ""), generaciones: distintos((t) => t.generacion ?? ""),
        directores: distintos((t) => t.director), informes: distintos(etiquetaInforme),
      },
    };
  }, [trabajos, pestana, busqueda, modalidades, generaciones, directores, informes]);

  return (
    <>
      <EncabezadoPagina titulo="Titulación" descripcion="Trabajos de titulación de los alumnos." />
      <section className="rounded-xl border border-portal-borde bg-blanco">
        <Pestanas pestanas={conteos} actual={pestana} onCambio={setPestana} />
        <div className="flex flex-wrap items-center gap-2.5 px-5 py-4">
          <label className="flex w-[300px] items-center gap-2 rounded-lg border border-portal-borde-campo bg-blanco px-3 py-2.5">
            <Search size={18} className="text-portal-placeholder" aria-hidden />
            <input type="text" value={busqueda} onChange={(e) => setBusqueda(e.target.value)}
              placeholder="Buscar por alumno o título" aria-label="Buscar por alumno o título"
              className="flex-1 text-[15px] text-portal-titulo outline-none placeholder:text-portal-placeholder" />
          </label>
          <FiltroMultiple etiqueta="Modalidad" titulo="Modalidad" opciones={opciones.modalidades} seleccion={modalidades} onAplicar={setModalidades} />
          <FiltroMultiple etiqueta="Generación" titulo="Generación" opciones={opciones.generaciones} seleccion={generaciones} onAplicar={setGeneraciones} />
          <FiltroMultiple etiqueta="Director" titulo="Director" opciones={opciones.directores} seleccion={directores} onAplicar={setDirectores} />
          <FiltroMultiple etiqueta="Informe" titulo="Informe en curso" opciones={opciones.informes} seleccion={informes} onAplicar={setInformes} />
        </div>
        <table className="w-full table-fixed border-t border-portal-borde text-left">
          <colgroup><col className="w-[235px]" /><col className="w-[150px]" /><col className="w-[220px]" /><col className="w-[120px]" /><col className="w-[135px]" /><col /><col className="w-[70px]" /></colgroup>
          <thead className="bg-portal-tabla-cabecera text-[13px] text-portal-subtexto">
            <tr className="border-b border-portal-borde">
              <th className="py-2.5 pl-5 font-semibold">Alumno</th>
              <th className="font-semibold">Modalidad</th>
              <th className="font-semibold">Director</th>
              <th className="font-semibold">Comité</th>
              <th className="font-semibold">Avance</th>
              <th className="font-semibold">Informe actual</th>
              <th><span className="sr-only">Ver ficha</span></th>
            </tr>
          </thead>
          <tbody className="text-sm text-portal-menu-texto">
            {trabajos && visibles.length === 0 && (
              <tr className="border-b border-portal-borde"><td colSpan={7} className="py-4 pl-5 text-portal-placeholder">Ningún trabajo coincide con los filtros.</td></tr>
            )}
            {visibles.map((t) => (
              <tr key={t.id} className="border-b border-portal-borde">
                <td className="py-3 pl-5">
                  <p className="font-semibold text-portal-titulo">{t.alumno}</p>
                  <p className="mt-[3px] text-[13px] text-portal-subtexto">{t.clave}</p>
                </td>
                <td>{t.modalidad}</td>
                <td>{t.director}</td>
                <td>{t.comite ? <Insignia tono={t.comite === "Completo" ? "gris" : "rojo"}>{t.comite}</Insignia> : <span className="text-portal-placeholder">—</span>}</td>
                <td>{t.avance === null ? <span className="text-portal-placeholder">—</span> : <BarraAvance porcentaje={t.avance} />}</td>
                <td>{t.informe ? <Insignia tono={tonoInforme(t)}>{t.informe}</Insignia> : <span className="text-portal-placeholder">—</span>}</td>
                <td className="pr-5 text-right">
                  <Link href={`/alumnos/${t.clave}`} aria-label={`Ver ficha de ${t.alumno}`} className="inline-flex size-[34px] items-center justify-center rounded-lg border border-portal-borde-campo text-portal-activo">
                    <Eye size={18} aria-hidden />
                  </Link>
                </td>
              </tr>
            ))}
          </tbody>
        </table>
        <p className="px-5 py-3.5 text-sm text-portal-subtexto">Mostrando {visibles.length} {visibles.length === 1 ? "trabajo" : "trabajos"}</p>
      </section>
    </>
  );
}
