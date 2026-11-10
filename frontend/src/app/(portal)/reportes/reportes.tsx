"use client";

import { useEffect, useState } from "react";
import { Briefcase, ClipboardList, Download, User, Users, X, type LucideIcon } from "lucide-react";
import { EncabezadoPagina } from "@/components/portal";
import { Alerta, FiltroMultiple, Insignia, useDesplegable, type Tono } from "@/components/modulo";
import { sesionVencida } from "@/lib/acceso";

// Generar reporte (Figma R1 por generación, R2 situación laboral, R3 avance de titulación).
// El tipo «Egresados» no tiene maqueta: sigue el estilo de R2 con tabla.

type Tipo = "generacion" | "laboral" | "egresados" | "avance";
type Opcion = { valor: string; conteo: number };
type Datos = {
  indicadores: { etiqueta: string; valor: string; nota: string | null }[];
  tabla: { columnas: string[]; filas: string[][]; total: string[] | null };
  grafica: {
    porGeneracion?: { generacion: string; activos: number; inactivos: number; egresados: number; baja: number; enTiempo: number; enCurso: boolean }[];
    rangos?: { etiqueta: string; cantidad: number }[];
    situaciones?: { etiqueta: string; cantidad: number }[];
    alumnos?: { alumno: string; modalidad: string; avance: number; esperado: number | null; evaluaciones: number; rezago: string; extra: number; tiempo: string }[];
    modalidades?: Opcion[];
    estados?: Opcion[];
  };
  nota: string | null;
};

const tipos: { valor: Tipo; titulo: string; descripcion: string; icono: LucideIcon }[] = [
  { valor: "generacion", titulo: "Por generación", descripcion: "Estado y titulación en tiempo", icono: Users },
  { valor: "laboral", titulo: "Situación laboral", descripcion: "Empleo de los egresados", icono: Briefcase },
  { valor: "egresados", titulo: "Egresados", descripcion: "Estado y localización", icono: User },
  { valor: "avance", titulo: "Avance de titulación", descripcion: "Progreso y rezago", icono: ClipboardList },
];

// Paleta categórica validada (Figma R1), en orden fijo.
const series = [
  { clave: "activos", etiqueta: "Activos", color: "#2a78d6" },
  { clave: "inactivos", etiqueta: "Inactivos", color: "#eb6834" },
  { clave: "egresados", etiqueta: "Egresados", color: "#1baf7a" },
  { clave: "baja", etiqueta: "Baja", color: "#eda100" },
] as const;
const tonoTiempo: Tono[] = ["gris", "ambar", "naranja", "rojo"];

export function GenerarReporte() {
  const [tipo, setTipo] = useState<Tipo>("generacion");
  const [generaciones, setGeneraciones] = useState<{ id: number; nombre: string }[]>([]);
  const [elegidas, setElegidas] = useState<number[]>([]);
  const [noLocalizables, setNoLocalizables] = useState(true);
  const [modalidades, setModalidades] = useState<string[]>([]);
  const [estados, setEstados] = useState<string[]>([]);
  const [datos, setDatos] = useState<Datos | null>(null);
  const [error, setError] = useState("");
  const [exportando, setExportando] = useState(false);

  useEffect(() => {
    fetch("/api/reportes/generaciones").then(async (res) => {
      if (res.status === 401) return sesionVencida();
      if (!res.ok) return;
      const lista = await res.json();
      setGeneraciones(lista);
      setElegidas(lista.map((g: { id: number }) => g.id));
    });
  }, []);

  const filtros = { tipo, generaciones: elegidas, incluirNoLocalizables: noLocalizables, modalidades, estados };
  const clave = JSON.stringify(filtros);

  useEffect(() => {
    if (!generaciones.length) return;
    let vigente = true;
    fetch("/api/reportes/vista", { method: "POST", headers: { "Content-Type": "application/json" }, body: clave }).then(async (res) => {
      if (res.status === 401) return sesionVencida();
      if (vigente && res.ok) setDatos(await res.json());
    });
    return () => { vigente = false; };
  }, [clave, generaciones.length]);

  async function exportar(formato: "pdf" | "excel" | "imagen", grafica?: number) {
    setError("");
    setExportando(true);
    const res = await fetch("/api/reportes/exportar", {
      method: "POST", headers: { "Content-Type": "application/json" }, body: JSON.stringify({ filtros, formato, grafica }),
    });
    setExportando(false);
    if (res.status === 401) return sesionVencida();
    if (!res.ok) return setError("No se pudo generar el archivo.");
    const nombre = /filename\*?=(?:UTF-8'')?"?([^";]+)/.exec(res.headers.get("content-disposition") ?? "")?.[1];
    const url = URL.createObjectURL(await res.blob());
    Object.assign(document.createElement("a"), { href: url, download: nombre ? decodeURIComponent(nombre) : `reporte.${{ pdf: "pdf", excel: "xlsx", imagen: "png" }[formato]}` }).click();
    URL.revokeObjectURL(url);
  }

  function cambiarTipo(t: Tipo) {
    setTipo(t);
    setDatos(null);
    setModalidades([]);
    setEstados([]);
  }

  const g = datos?.grafica;
  return (
    <>
      <EncabezadoPagina titulo="Generar reporte" descripcion="Indicadores para seguimiento y auditorías." />
      <div className="flex flex-col gap-5">
        <div className="grid grid-cols-4 gap-3.5" role="radiogroup" aria-label="Tipo de reporte">
          {tipos.map(({ valor, titulo, descripcion, icono: Icono }) => {
            const activo = tipo === valor;
            return (
              <button key={valor} role="radio" aria-checked={activo} onClick={() => cambiarTipo(valor)}
                className={`flex min-w-0 items-center gap-3 rounded-xl p-4 text-left ${activo ? "border-[1.5px] border-portal-activo bg-portal-activo-fondo" : "border border-portal-borde bg-blanco"}`}>
                <Icono size={24} className={activo ? "text-portal-activo" : "text-portal-icono"} aria-hidden />
                <span className="flex min-w-0 flex-col gap-0.5">
                  <span className={`text-sm font-semibold ${activo ? "text-portal-activo" : "text-portal-titulo"}`}>{titulo}</span>
                  <span className="truncate text-[13px] text-portal-subtexto">{descripcion}</span>
                </span>
              </button>
            );
          })}
        </div>

        <section className="flex min-w-0 flex-col gap-5 rounded-xl border border-portal-borde bg-blanco px-6 pt-5 pb-6">
          <div className="flex items-center gap-2.5">
            <div className="flex flex-1 flex-wrap items-center gap-2">
              <ChipsGeneraciones todas={generaciones} elegidas={elegidas} onCambio={setElegidas} />
              {(tipo === "laboral" || tipo === "egresados") && (
                <label className="ml-1 flex cursor-pointer items-center gap-2 text-[13px] text-portal-menu-texto">
                  <button type="button" role="switch" aria-checked={noLocalizables} onClick={() => setNoLocalizables(!noLocalizables)}
                    className={`relative h-5 w-8 rounded-full transition-colors ${noLocalizables ? "bg-portal-activo" : "bg-portal-borde-campo"}`}>
                    <span className={`absolute top-0.5 size-4 rounded-full bg-blanco shadow transition-all ${noLocalizables ? "left-[14px]" : "left-0.5"}`} />
                  </button>
                  Incluir no localizables
                </label>
              )}
              {tipo === "avance" && (
                <>
                  <FiltroMultiple etiqueta="Modalidad" titulo="Modalidad" opciones={g?.modalidades ?? []} seleccion={modalidades} onAplicar={setModalidades} />
                  <FiltroMultiple etiqueta="Estado" titulo="Estado académico" opciones={g?.estados ?? []} seleccion={estados} onAplicar={setEstados} />
                </>
              )}
            </div>
            {(["pdf", "excel"] as const).map((f) => (
              <button key={f} onClick={() => exportar(f)} disabled={exportando || !datos}
                className="flex items-center gap-2 rounded-lg border border-portal-borde-campo bg-blanco py-2.5 pr-[18px] pl-4 text-[15px] font-semibold text-portal-menu-texto disabled:opacity-50">
                <Download size={18} aria-hidden />
                {f === "pdf" ? "PDF" : "Excel"}
              </button>
            ))}
          </div>
          {error && <Alerta tipo="error">{error}</Alerta>}

          {datos && (
            <>
              <div className="flex gap-4">
                {datos.indicadores.map((i) => (
                  <div key={i.etiqueta} className="flex min-w-0 flex-1 flex-col gap-1 rounded-[10px] bg-portal-tabla-cabecera px-4 py-3.5">
                    <p className="text-[13px] text-portal-subtexto">{i.etiqueta}</p>
                    <p className="text-[26px] font-bold text-portal-titulo">{i.valor}</p>
                    {i.nota && <p className="text-[13px] text-portal-subtexto">{i.nota}</p>}
                  </div>
                ))}
              </div>

              {g?.porGeneracion && (
                <div className="flex gap-5">
                  <BarrasPorGeneracion filas={g.porGeneracion} onDescargar={() => exportar("imagen", 0)} />
                  <TitulacionEnTiempo filas={g.porGeneracion} onDescargar={() => exportar("imagen", 1)} />
                </div>
              )}
              {g?.rangos && <BarrasHorizontales titulo="Tiempo para emplearse después de egresar" barras={g.rangos} gris="Sin empleo" onDescargar={() => exportar("imagen", 0)} />}
              {g?.situaciones && <BarrasHorizontales titulo="Egresados por situación laboral" barras={g.situaciones} gris="Sin trabajo" onDescargar={() => exportar("imagen", 0)} />}

              {tipo === "avance" ? <TablaAvance alumnos={g?.alumnos ?? []} /> : tipo !== "laboral" && <Tabla tabla={datos.tabla} />}
              {datos.nota && <p className="text-xs text-portal-subtexto">{datos.nota}</p>}
              {tipo === "avance" && (
                <p className="flex gap-6 text-xs text-portal-subtexto">
                  <span className="border-l-2 border-portal-titulo pl-1.5">Línea: avance esperado según el semestre</span>
                  <span>Tiempo: +1 semestre amarillo · +2 naranja · +3 rojo</span>
                </p>
              )}
            </>
          )}
        </section>
      </div>
    </>
  );
}

function ChipsGeneraciones({ todas, elegidas, onCambio }: { todas: { id: number; nombre: string }[]; elegidas: number[]; onCambio: (ids: number[]) => void }) {
  const { caja, abierto, setAbierto, alternar, lado } = useDesplegable();
  const faltan = todas.filter((g) => !elegidas.includes(g.id));
  return (
    <>
      <span className="text-sm font-semibold text-portal-menu-texto">Generaciones</span>
      {elegidas.length === 0 && <span className="text-[13px] text-portal-subtexto">Todas</span>}
      {todas.filter((g) => elegidas.includes(g.id)).map((g) => (
        <span key={g.id} className="flex items-center gap-1 rounded-md bg-portal-neutro-fondo py-[5px] pr-1.5 pl-2.5 text-[13px] font-medium text-portal-menu-texto">
          {g.nombre}
          <button onClick={() => onCambio(elegidas.filter((id) => id !== g.id))} aria-label={`Quitar ${g.nombre}`}>
            <X size={14} aria-hidden />
          </button>
        </span>
      ))}
      {faltan.length > 0 && (
        <div ref={caja} className="relative">
          <button onClick={alternar} aria-expanded={abierto} className="text-sm font-semibold text-portal-activo">+ Agregar</button>
          {abierto && (
            <ul className={`absolute top-full ${lado} z-10 mt-2 w-[200px] rounded-[10px] border border-portal-borde bg-blanco p-1.5 shadow-[0_8px_24px_rgba(10,18,51,0.14)]`}>
              {faltan.map((g) => (
                <li key={g.id}>
                  <button onClick={() => { onCambio([...elegidas, g.id]); setAbierto(false); }}
                    className="w-full rounded-md px-2.5 py-2 text-left text-sm text-portal-menu-texto hover:bg-portal-tabla-cabecera">{g.nombre}</button>
                </li>
              ))}
            </ul>
          )}
        </div>
      )}
    </>
  );
}

// Descarga la gráfica como imagen; queda en el historial.
function DescargarGrafica({ titulo, onClick }: { titulo: string; onClick: () => void }) {
  return (
    <button onClick={onClick} aria-label={`Descargar «${titulo}» como imagen`} title="Descargar imagen"
      className="flex size-[30px] shrink-0 items-center justify-center rounded-md border border-portal-borde-campo text-portal-icono hover:text-portal-activo">
      <Download size={16} aria-hidden />
    </button>
  );
}

type Descarga = { onDescargar: () => void };

function BarrasPorGeneracion({ filas, onDescargar }: { filas: NonNullable<Datos["grafica"]["porGeneracion"]> } & Descarga) {
  const maximo = Math.max(1, ...filas.flatMap((f) => series.map((s) => f[s.clave])));
  return (
    <figure className="flex min-w-0 flex-1 flex-col gap-3.5 rounded-xl border border-portal-borde px-5 py-[18px]">
      <figcaption className="flex items-start justify-between gap-3 text-base font-semibold text-portal-titulo">
        Estado de los alumnos por generación
        <DescargarGrafica titulo="Estado de los alumnos por generación" onClick={onDescargar} />
      </figcaption>
      <ul className="flex gap-4">
        {series.map((s) => (
          <li key={s.clave} className="flex items-center gap-1.5 text-[13px] text-portal-menu-texto">
            <span className="size-3 rounded-[3px]" style={{ background: s.color }} aria-hidden />{s.etiqueta}
          </li>
        ))}
      </ul>
      <div className="flex items-end gap-[22px] pt-2">
        {filas.map((f) => (
          <div key={f.generacion} className="flex min-w-0 flex-1 flex-col items-center gap-2">
            <div className="flex h-[200px] items-end gap-0.5">
              {series.map((s) => {
                const v = f[s.clave];
                return (
                  <div key={s.clave} className="flex w-[22px] flex-col items-center gap-1" title={`${f.generacion} · ${s.etiqueta}: ${v}`}>
                    <span className="text-[11px] font-medium text-portal-subtexto">{v}</span>
                    {v > 0
                      ? <span className="w-[22px] rounded-t-[4px]" style={{ height: `${(v / maximo) * 180}px`, background: s.color }} />
                      : <span className="h-0.5 w-[22px] rounded-t-[1px] bg-portal-borde" />}
                  </div>
                );
              })}
            </div>
            <span className="h-px w-[110px] bg-portal-borde-campo" aria-hidden />
            <span className="text-[13px] text-portal-menu-texto">{f.generacion}</span>
          </div>
        ))}
      </div>
    </figure>
  );
}

function TitulacionEnTiempo({ filas, onDescargar }: { filas: NonNullable<Datos["grafica"]["porGeneracion"]> } & Descarga) {
  return (
    <figure className="flex min-w-0 flex-1 flex-col gap-3.5 self-start rounded-xl border border-portal-borde px-5 py-[18px]">
      <figcaption className="flex flex-col gap-3.5">
        <span className="flex items-start justify-between gap-3 text-base font-semibold text-portal-titulo">
          Titulación en tiempo
          <DescargarGrafica titulo="Titulación en tiempo" onClick={onDescargar} />
        </span>
        <span className="text-[13px] text-portal-subtexto">De los egresados de cada generación</span>
      </figcaption>
      {filas.map((f) => {
        const pct = f.egresados ? Math.round((f.enTiempo * 100) / f.egresados) : 0;
        return (
          <div key={f.generacion} className="flex flex-col gap-1.5">
            <p className="flex justify-between">
              <span className="text-sm text-portal-menu-texto">{f.generacion}</span>
              {f.enCurso || !f.egresados
                ? <span className="text-[13px] text-portal-placeholder">{f.enCurso ? "En curso" : "Sin egresados"}</span>
                : <span className="text-[13px] font-semibold text-portal-titulo">{pct} % · {f.enTiempo} de {f.egresados}</span>}
            </p>
            <span className="h-2.5 overflow-hidden rounded-[5px] bg-portal-neutro-fondo" role="progressbar" aria-valuenow={pct} aria-valuemin={0} aria-valuemax={100} aria-label={f.generacion}>
              <span className="block h-full bg-[#2a78d6]" style={{ width: `${pct}%` }} />
            </span>
          </div>
        );
      })}
    </figure>
  );
}

function BarrasHorizontales({ titulo, barras, gris, onDescargar }: { titulo: string; barras: { etiqueta: string; cantidad: number }[]; gris: string } & Descarga) {
  const maximo = Math.max(1, ...barras.map((b) => b.cantidad));
  return (
    <figure className="flex flex-col gap-3">
      <figcaption className="flex items-start justify-between gap-3 text-base font-semibold text-portal-titulo">
        {titulo}
        <DescargarGrafica titulo={titulo} onClick={onDescargar} />
      </figcaption>
      {barras.map((b) => (
        <div key={b.etiqueta} className="flex items-center gap-3">
          <span className="w-[122px] shrink-0 text-sm text-portal-menu-texto">{b.etiqueta}</span>
          <span className="flex flex-1 items-center gap-3">
            <span className={`h-5 rounded-[3px] ${b.etiqueta === gris ? "bg-[#8e93ae]" : "bg-[#2a78d6]"}`} style={{ width: `${(b.cantidad / maximo) * 75}%` }} />
            <span className="text-[13px] whitespace-nowrap text-portal-menu-texto">{b.cantidad} {b.cantidad === 1 ? "egresado" : "egresados"}</span>
          </span>
        </div>
      ))}
    </figure>
  );
}

function Tabla({ tabla }: { tabla: Datos["tabla"] }) {
  return (
    <div className="overflow-hidden rounded-[10px] border border-portal-borde">
      <table className="w-full text-left">
        <thead className="bg-portal-tabla-cabecera text-[13px] text-portal-subtexto">
          <tr>{tabla.columnas.map((c) => <th key={c} className="px-4 py-2.5 font-semibold">{c}</th>)}</tr>
        </thead>
        <tbody className="text-sm text-portal-menu-texto">
          {tabla.filas.length === 0 && <tr className="border-t border-portal-borde"><td colSpan={tabla.columnas.length} className="px-4 py-2.5 text-portal-placeholder">Ningún registro coincide con los filtros.</td></tr>}
          {tabla.filas.map((f, i) => (
            <tr key={i} className="border-t border-portal-borde">{f.map((v, j) => <td key={j} className="px-4 py-2.5">{v}</td>)}</tr>
          ))}
          {tabla.total && (
            <tr className="border-t border-portal-borde bg-portal-activo-fondo font-semibold text-portal-activo">
              {tabla.total.map((v, j) => <td key={j} className="px-4 py-2.5">{v}</td>)}
            </tr>
          )}
        </tbody>
      </table>
    </div>
  );
}

function TablaAvance({ alumnos }: { alumnos: NonNullable<Datos["grafica"]["alumnos"]> }) {
  return (
    <div className="overflow-hidden rounded-[10px] border border-portal-borde">
      <table className="w-full text-left">
        <thead className="bg-portal-tabla-cabecera text-[13px] text-portal-subtexto">
          <tr>{["Alumno", "Modalidad", "Avance", "Evaluaciones", "Rezago por avance", "Tiempo"].map((c) => <th key={c} className="px-4 py-2.5 font-semibold">{c}</th>)}</tr>
        </thead>
        <tbody className="text-sm text-portal-menu-texto">
          {alumnos.length === 0 && <tr className="border-t border-portal-borde"><td colSpan={6} className="px-4 py-2.5 text-portal-placeholder">Ningún registro coincide con los filtros.</td></tr>}
          {alumnos.map((a) => (
            <tr key={a.alumno} className="border-t border-portal-borde">
              <td className="px-4 py-2.5 font-semibold text-portal-titulo">{a.alumno}</td>
              <td className="px-4">{a.modalidad}</td>
              <td className="px-4">
                <span className="flex items-center gap-2">
                  <span className="relative h-1.5 w-[122px] rounded-[3px] bg-portal-neutro-fondo" role="progressbar" aria-valuenow={Math.round(a.avance)} aria-valuemin={0} aria-valuemax={100}
                    aria-label={a.esperado === null ? "Avance" : `Avance; esperado ${Math.round(a.esperado)} %`}>
                    <span className="block h-full rounded-[3px] bg-[#2a78d6]" style={{ width: `${Math.min(a.avance, 100)}%` }} />
                    {a.esperado !== null && <span className="absolute -top-1 h-3.5 w-0.5 bg-portal-titulo" style={{ left: `${a.esperado}%` }} aria-hidden />}
                  </span>
                  <span className="text-[13px]">{Math.round(a.avance)} %</span>
                </span>
              </td>
              <td className="px-4">{a.evaluaciones}</td>
              <td className="px-4">{a.rezago === "En rezago" ? <Insignia tono="rojo">En rezago</Insignia> : <span className="text-portal-placeholder">—</span>}</td>
              <td className="px-4">{a.extra > 0 ? <Insignia tono={tonoTiempo[Math.min(a.extra, 3)]}>{a.tiempo}</Insignia> : <span className="text-portal-subtexto">{a.tiempo}</span>}</td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  );
}
